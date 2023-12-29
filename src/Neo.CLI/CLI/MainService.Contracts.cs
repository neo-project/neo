// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
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
                tx = CurrentWallet.MakeTransaction(NeoSystem.StoreView, script);
            }
            catch (InvalidOperationException e)
            {
                ConsoleHelper.Error(GetExceptionMessage(e));
                return;
            }
            UInt160 hash = SmartContract.Helper.GetContractHash(tx.Sender, nef.CheckSum, manifest.Name);

            ConsoleHelper.Info("Contract hash: ", $"{hash}");
            ConsoleHelper.Info("Gas consumed: ", $"{new BigDecimal((BigInteger)tx.SystemFee, NativeContract.GAS.Decimals)}");
            ConsoleHelper.Info("Network fee: ", $"{new BigDecimal((BigInteger)tx.NetworkFee, NativeContract.GAS.Decimals)}");
            ConsoleHelper.Info("Total fee: ", $"{new BigDecimal((BigInteger)(tx.SystemFee + tx.NetworkFee), NativeContract.GAS.Decimals)} GAS");
            if (!ReadUserInput("Relay tx? (no|yes)").IsYes()) // Add this in case just want to get hash but not relay
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
                tx = CurrentWallet.MakeTransaction(NeoSystem.StoreView, script, sender, signers);
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
                ConsoleHelper.Info("Gas consumed: ", $"{new BigDecimal((BigInteger)tx.SystemFee, NativeContract.GAS.Decimals)}");
                ConsoleHelper.Info("Network fee: ", $"{new BigDecimal((BigInteger)tx.NetworkFee, NativeContract.GAS.Decimals)}");
                ConsoleHelper.Info("Total fee: ", $"{new BigDecimal((BigInteger)(tx.SystemFee + tx.NetworkFee), NativeContract.GAS.Decimals)} GAS");
                if (!ReadUserInput("Relay tx? (no|yes)").IsYes()) // Add this in case just want to get hash but not relay
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
        /// <param name="maxGas">Max fee for running the script</param>
        [ConsoleCommand("invoke", Category = "Contract Commands")]
        private void OnInvokeCommand(UInt160 scriptHash, string operation, JArray? contractParameters = null, UInt160? sender = null, UInt160[]? signerAccounts = null, decimal maxGas = 20)
        {
            var gas = new BigDecimal(maxGas, NativeContract.GAS.Decimals);
            Signer[] signers = Array.Empty<Signer>();
            if (!NoWallet() && sender != null)
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

            Transaction tx = new Transaction
            {
                Signers = signers,
                Attributes = Array.Empty<TransactionAttribute>(),
                Witnesses = Array.Empty<Witness>(),
            };

            if (!OnInvokeWithResult(scriptHash, operation, out _, tx, contractParameters, gas: (long)gas.Value)) return;

            if (NoWallet()) return;
            try
            {
                tx = CurrentWallet.MakeTransaction(NeoSystem.StoreView, tx.Script, sender, signers, maxGas: (long)gas.Value);
            }
            catch (InvalidOperationException e)
            {
                ConsoleHelper.Error(GetExceptionMessage(e));
                return;
            }
            ConsoleHelper.Info("Network fee: ",
                $"{new BigDecimal((BigInteger)tx.NetworkFee, NativeContract.GAS.Decimals)}\t",
                "Total fee: ",
                $"{new BigDecimal((BigInteger)(tx.SystemFee + tx.NetworkFee), NativeContract.GAS.Decimals)} GAS");
            if (!ReadUserInput("Relay tx? (no|yes)").IsYes())
            {
                return;
            }
            SignAndSendTx(NeoSystem.StoreView, tx);
        }
    }
}
