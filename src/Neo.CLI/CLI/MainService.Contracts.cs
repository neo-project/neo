// Copyright (C) 2015-2025 The Neo Project.
//
// MainService.Contracts.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.Cryptography.ECC;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.CLI
{
    partial class MainService
    {
        /// <summary>
        /// Process "deploy" command
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="manifestPath">Manifest path</param>
        /// <param name="data">Extra data for deploy</param>
        [ConsoleCommand("deploy", Category = "Contract Commands")]
        private void OnDeployCommand(string filePath, string? manifestPath = null, JObject? data = null)
        {
            if (NoWallet()) return;
            byte[] script = LoadDeploymentScript(filePath, manifestPath, data, out var nef, out var manifest);
            Transaction tx;
            try
            {
                tx = CurrentWallet!.MakeTransaction(NeoSystem.StoreView, script);
            }
            catch (InvalidOperationException e)
            {
                ConsoleHelper.Error(GetExceptionMessage(e));
                return;
            }
            UInt160 hash = SmartContract.Helper.GetContractHash(tx.Sender, nef.CheckSum, manifest.Name);

            ConsoleHelper.Info("Contract hash: ", $"{hash}");
            ConsoleHelper.Info("Gas consumed: ", $"{new BigDecimal((BigInteger)tx.SystemFee, NativeContract.GAS.Decimals)} GAS");
            ConsoleHelper.Info("Network fee: ", $"{new BigDecimal((BigInteger)tx.NetworkFee, NativeContract.GAS.Decimals)} GAS");
            ConsoleHelper.Info("Total fee: ", $"{new BigDecimal((BigInteger)(tx.SystemFee + tx.NetworkFee), NativeContract.GAS.Decimals)} GAS");
            if (!ConsoleHelper.ReadUserInput("Relay tx? (no|yes)").IsYes()) // Add this in case just want to get hash but not relay
            {
                return;
            }
            SignAndSendTx(NeoSystem.StoreView, tx);
        }

        /// <summary>
        /// Process "update" command
        /// </summary>
        /// <param name="scriptHash">Script hash</param>
        /// <param name="filePath">File path</param>
        /// <param name="manifestPath">Manifest path</param>
        /// <param name="sender">Sender</param>
        /// <param name="signerAccounts">Signer Accounts</param>
        /// <param name="data">Extra data for update</param>
        [ConsoleCommand("update", Category = "Contract Commands")]
        private void OnUpdateCommand(UInt160 scriptHash, string filePath, string manifestPath, UInt160 sender, UInt160[]? signerAccounts = null, JObject? data = null)
        {
            Signer[] signers = Array.Empty<Signer>();

            if (NoWallet()) return;
            if (sender != null)
            {
                if (signerAccounts == null)
                    signerAccounts = new[] { sender };
                else if (signerAccounts.Contains(sender) && signerAccounts[0] != sender)
                {
                    var signersList = signerAccounts.ToList();
                    signersList.Remove(sender);
                    signerAccounts = signersList.Prepend(sender).ToArray();
                }
                else if (!signerAccounts.Contains(sender))
                {
                    signerAccounts = signerAccounts.Prepend(sender).ToArray();
                }
                signers = signerAccounts.Select(p => new Signer() { Account = p, Scopes = WitnessScope.CalledByEntry }).ToArray();
            }

            Transaction tx;
            try
            {
                byte[] script = LoadUpdateScript(scriptHash, filePath, manifestPath, data, out var nef, out var manifest);
                tx = CurrentWallet!.MakeTransaction(NeoSystem.StoreView, script, sender, signers);
            }
            catch (InvalidOperationException e)
            {
                ConsoleHelper.Error(GetExceptionMessage(e));
                return;
            }
            ContractState contract = NativeContract.ContractManagement.GetContract(NeoSystem.StoreView, scriptHash);
            if (contract == null)
            {
                ConsoleHelper.Warning($"Can't upgrade, contract hash not exist: {scriptHash}");
            }
            else
            {
                ConsoleHelper.Info("Contract hash: ", $"{scriptHash}");
                ConsoleHelper.Info("Updated times: ", $"{contract.UpdateCounter}");
                ConsoleHelper.Info("Gas consumed: ", $"{new BigDecimal((BigInteger)tx.SystemFee, NativeContract.GAS.Decimals)} GAS");
                ConsoleHelper.Info("Network fee: ", $"{new BigDecimal((BigInteger)tx.NetworkFee, NativeContract.GAS.Decimals)} GAS");
                ConsoleHelper.Info("Total fee: ", $"{new BigDecimal((BigInteger)(tx.SystemFee + tx.NetworkFee), NativeContract.GAS.Decimals)} GAS");
                if (!ConsoleHelper.ReadUserInput("Relay tx? (no|yes)").IsYes()) // Add this in case just want to get hash but not relay
                {
                    return;
                }
                SignAndSendTx(NeoSystem.StoreView, tx);
            }
        }

        /// <summary>
        /// Process "invoke" command
        /// </summary>
        /// <param name="scriptHash">Script hash</param>
        /// <param name="operation">Operation</param>
        /// <param name="contractParameters">Contract parameters</param>
        /// <param name="sender">Transaction's sender</param>
        /// <param name="signerAccounts">Signer's accounts</param>
        /// <param name="maxGas">Max fee for running the script, in the unit of GAS</param>
        [ConsoleCommand("invoke", Category = "Contract Commands")]
        private void OnInvokeCommand(UInt160 scriptHash, string operation, JArray? contractParameters = null, UInt160? sender = null, UInt160[]? signerAccounts = null, decimal maxGas = 20)
        {
            // In the unit of datoshi, 1 datoshi = 1e-8 GAS
            var datoshi = new BigDecimal(maxGas, NativeContract.GAS.Decimals);
            Signer[] signers = Array.Empty<Signer>();
            if (!NoWallet())
            {
                if (sender == null)
                    sender = CurrentWallet!.GetDefaultAccount()?.ScriptHash;

                if (sender != null)
                {
                    if (signerAccounts == null)
                        signerAccounts = new UInt160[1] { sender };
                    else if (signerAccounts.Contains(sender) && signerAccounts[0] != sender)
                    {
                        var signersList = signerAccounts.ToList();
                        signersList.Remove(sender);
                        signerAccounts = signersList.Prepend(sender).ToArray();
                    }
                    else if (!signerAccounts.Contains(sender))
                    {
                        signerAccounts = signerAccounts.Prepend(sender).ToArray();
                    }
                    signers = signerAccounts.Select(p => new Signer() { Account = p, Scopes = WitnessScope.CalledByEntry }).ToArray();
                }
            }

            Transaction tx = new Transaction
            {
                Signers = signers,
                Attributes = Array.Empty<TransactionAttribute>(),
                Witnesses = Array.Empty<Witness>(),
            };

            if (!OnInvokeWithResult(scriptHash, operation, out _, tx, contractParameters, datoshi: (long)datoshi.Value)) return;

            if (NoWallet()) return;
            try
            {
                tx = CurrentWallet!.MakeTransaction(NeoSystem.StoreView, tx.Script, sender, signers, maxGas: (long)datoshi.Value);
            }
            catch (InvalidOperationException e)
            {
                ConsoleHelper.Error(GetExceptionMessage(e));
                return;
            }
            ConsoleHelper.Info("Network fee: ",
                $"{new BigDecimal((BigInteger)tx.NetworkFee, NativeContract.GAS.Decimals)} GAS\t",
                "Total fee: ",
                $"{new BigDecimal((BigInteger)(tx.SystemFee + tx.NetworkFee), NativeContract.GAS.Decimals)} GAS");
            if (!ConsoleHelper.ReadUserInput("Relay tx? (no|yes)").IsYes())
            {
                return;
            }
            SignAndSendTx(NeoSystem.StoreView, tx);
        }

        /// <summary>
        /// Process "invokeabi" command - invokes a contract method with parameters parsed according to the contract's ABI
        /// </summary>
        /// <param name="scriptHash">Script hash</param>
        /// <param name="operation">Operation</param>
        /// <param name="args">Arguments as an array of values that will be parsed according to the ABI</param>
        /// <param name="sender">Transaction's sender</param>
        /// <param name="signerAccounts">Signer's accounts</param>
        /// <param name="maxGas">Max fee for running the script, in the unit of GAS</param>
        [ConsoleCommand("invokeabi", Category = "Contract Commands")]
        private void OnInvokeAbiCommand(UInt160 scriptHash, string operation,
            JArray? args = null, UInt160? sender = null, UInt160[]? signerAccounts = null, decimal maxGas = 20)
        {
            // Get the contract from storage
            var contract = NativeContract.ContractManagement.GetContract(NeoSystem.StoreView, scriptHash);
            if (contract == null)
            {
                ConsoleHelper.Error("Contract does not exist.");
                return;
            }

            // Check if contract has valid ABI
            if (contract.Manifest?.Abi == null)
            {
                ConsoleHelper.Error("Contract ABI is not available.");
                return;
            }

            // Find the method in the ABI with matching parameter count
            var paramCount = args?.Count ?? 0;
            var method = contract.Manifest.Abi.GetMethod(operation, paramCount);
            if (method == null)
            {
                // Try to find any method with that name for a better error message
                var anyMethod = contract.Manifest.Abi.GetMethod(operation, -1);
                if (anyMethod != null)
                {
                    ConsoleHelper.Error($"Method '{operation}' exists but expects {anyMethod.Parameters.Length} parameters, not {paramCount}.");
                }
                else
                {
                    ConsoleHelper.Error($"Method '{operation}' does not exist in this contract.");
                }
                return;
            }

            // Validate parameter count
            if (args != null && args.Count != method.Parameters.Length)
            {
                ConsoleHelper.Error($"Method '{operation}' expects exactly {method.Parameters.Length} parameters but {args.Count} were provided.");
                return;
            }
            else if (args == null && method.Parameters.Length > 0)
            {
                ConsoleHelper.Error($"Method '{operation}' expects {method.Parameters.Length} parameters but none were provided.");
                return;
            }

            // Parse parameters according to the ABI
            JArray? contractParameters = null;
            if (args != null && args.Count > 0)
            {
                contractParameters = new JArray();
                for (int i = 0; i < args.Count; i++)
                {
                    var paramDef = method.Parameters[i];
                    var paramValue = args[i];

                    try
                    {
                        var contractParam = ParseParameterFromAbi(paramDef.Type, paramValue);
                        contractParameters.Add(contractParam.ToJson());
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelper.Error($"Failed to parse parameter '{paramDef.Name ?? $"at index {i}"}' (index {i}): {ex.Message}");
                        return;
                    }
                }
            }

            // Call the original invoke command with the parsed parameters
            OnInvokeCommand(scriptHash, operation, contractParameters, sender, signerAccounts, maxGas);
        }

        /// <summary>
        /// Parse a parameter value according to its ABI type
        /// </summary>
        private ContractParameter ParseParameterFromAbi(ContractParameterType type, JToken? value)
        {
            var param = new ContractParameter { Type = type };

            if (value == null || value == JToken.Null)
            {
                param.Value = null;
                return param;
            }

            switch (type)
            {
                case ContractParameterType.Boolean:
                    param.Value = value.AsBoolean();
                    break;
                case ContractParameterType.Integer:
                    try
                    {
                        param.Value = BigInteger.Parse(value.AsString());
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException($"Invalid integer format. Expected a numeric string, got: '{value.AsString()}'");
                    }
                    break;
                case ContractParameterType.ByteArray:
                    try
                    {
                        param.Value = Convert.FromBase64String(value.AsString());
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException($"Invalid ByteArray format. Expected a Base64 encoded string, got: '{value.AsString()}'");
                    }
                    break;
                case ContractParameterType.String:
                    param.Value = value.AsString();
                    break;
                case ContractParameterType.Hash160:
                    try
                    {
                        param.Value = UInt160.Parse(value.AsString());
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException($"Invalid Hash160 format. Expected format: '0x' followed by 40 hex characters (e.g., '0x1234...abcd'), got: '{value.AsString()}'");
                    }
                    break;
                case ContractParameterType.Hash256:
                    try
                    {
                        param.Value = UInt256.Parse(value.AsString());
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException($"Invalid Hash256 format. Expected format: '0x' followed by 64 hex characters, got: '{value.AsString()}'");
                    }
                    break;
                case ContractParameterType.PublicKey:
                    try
                    {
                        param.Value = ECPoint.Parse(value.AsString(), ECCurve.Secp256r1);
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException($"Invalid PublicKey format. Expected a hex string starting with '02' or '03' (33 bytes) or '04' (65 bytes), got: '{value.AsString()}'");
                    }
                    break;
                case ContractParameterType.Signature:
                    try
                    {
                        param.Value = Convert.FromBase64String(value.AsString());
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException($"Invalid Signature format. Expected a Base64 encoded string, got: '{value.AsString()}'");
                    }
                    break;
                case ContractParameterType.Array:
                    if (value is JArray array)
                    {
                        var items = new ContractParameter[array.Count];
                        for (int j = 0; j < array.Count; j++)
                        {
                            items[j] = ParseParameterFromAbi(ContractParameterType.Any, array[j]);
                        }
                        param.Value = items;
                    }
                    else
                    {
                        throw new ArgumentException($"Expected array value for Array parameter type, got: {value.GetType().Name}");
                    }
                    break;
                case ContractParameterType.Map:
                    if (value is JObject map)
                    {
                        var dict = new List<KeyValuePair<ContractParameter, ContractParameter>>();
                        foreach (var kvp in map.Properties)
                        {
                            var key = new ContractParameter { Type = ContractParameterType.String, Value = kvp.Key };
                            var val = ParseParameterFromAbi(ContractParameterType.Any, kvp.Value);
                            dict.Add(new KeyValuePair<ContractParameter, ContractParameter>(key, val));
                        }
                        param.Value = dict;
                    }
                    else
                    {
                        throw new ArgumentException("Expected object value for Map parameter type");
                    }
                    break;
                case ContractParameterType.InteropInterface:
                    throw new NotSupportedException("InteropInterface type cannot be parsed from JSON");
                case ContractParameterType.Any:
                    return InferParameterFromToken(value);
                default:
                    throw new ArgumentException($"Unsupported parameter type: {type}");
            }

            return param;
        }

        /// <summary>
        /// Infers the parameter type from a JToken and parses it accordingly
        /// </summary>
        private ContractParameter InferParameterFromToken(JToken value)
        {
            return value switch
            {
                JBoolean => ParseParameterFromAbi(ContractParameterType.Boolean, value),
                JNumber => ParseParameterFromAbi(ContractParameterType.Integer, value),
                JString => ParseParameterFromAbi(ContractParameterType.String, value),
                JArray => ParseParameterFromAbi(ContractParameterType.Array, value),
                JObject => ParseParameterFromAbi(ContractParameterType.Map, value),
                _ => throw new ArgumentException($"Cannot infer type for value: {value}")
            };
        }
    }
}
