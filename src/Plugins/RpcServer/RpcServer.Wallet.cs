// Copyright (C) 2015-2024 The Neo Project.
//
// RpcServer.Wallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.RpcServer.Model;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Neo.Plugins.RpcServer
{
    partial class RpcServer
    {
        private class DummyWallet : Wallet
        {
            public DummyWallet(ProtocolSettings settings) : base(null, settings) { }
            public override string Name => "";
            public override Version Version => new();

            public override bool ChangePassword(string oldPassword, string newPassword) => false;
            public override bool Contains(UInt160 scriptHash) => false;
            public override WalletAccount CreateAccount(byte[] privateKey) => null;
            public override WalletAccount CreateAccount(Contract contract, KeyPair key = null) => null;
            public override WalletAccount CreateAccount(UInt160 scriptHash) => null;
            public override void Delete() { }
            public override bool DeleteAccount(UInt160 scriptHash) => false;
            public override WalletAccount GetAccount(UInt160 scriptHash) => null;
            public override IEnumerable<WalletAccount> GetAccounts() => Array.Empty<WalletAccount>();
            public override bool VerifyPassword(string password) => false;
            public override void Save() { }
        }

        protected internal Wallet wallet;

        /// <summary>
        /// Checks if a wallet is open and throws an error if not.
        /// </summary>
        private void CheckWallet()
        {
            wallet.NotNull_Or(RpcError.NoOpenedWallet);
        }

        /// <summary>
        /// Closes the currently opened wallet.
        /// </summary>
        /// <param name="_params">An empty array.</param>
        /// <returns>Returns true if the wallet was successfully closed.</returns>
        [RpcMethod]
        protected internal virtual JToken CloseWallet(JArray _params)
        {
            wallet = null;
            return true;
        }

        /// <summary>
        /// Exports the private key of a specified address.
        /// </summary>
        /// <param name="_params">An array containing the address as a string.</param>
        /// <returns>The exported private key as a string.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open or the address is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken DumpPrivKey(JArray _params)
        {
            CheckWallet();
            UInt160 scriptHash = AddressToScriptHash(_params[0].AsString(), system.Settings.AddressVersion);
            WalletAccount account = wallet.GetAccount(scriptHash);
            return account.GetKey().Export();
        }

        /// <summary>
        /// Creates a new address in the wallet.
        /// </summary>
        /// <param name="_params">An empty array.</param>
        /// <returns>The newly created address as a string.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open.</exception>
        [RpcMethod]
        protected internal virtual JToken GetNewAddress(JArray _params)
        {
            CheckWallet();
            WalletAccount account = wallet.CreateAccount();
            if (wallet is NEP6Wallet nep6)
                nep6.Save();
            return account.Address;
        }

        /// <summary>
        /// Gets the balance of a specified asset in the wallet.
        /// </summary>
        /// <param name="_params">An array containing the asset ID as a string.</param>
        /// <returns>A JSON object containing the balance of the specified asset.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open or the asset ID is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken GetWalletBalance(JArray _params)
        {
            CheckWallet();
            UInt160 asset_id = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid asset id: {_params[0]}"));
            JObject json = new();
            json["balance"] = wallet.GetAvailable(system.StoreView, asset_id).Value.ToString();
            return json;
        }

        /// <summary>
        /// Gets the amount of unclaimed GAS in the wallet.
        /// </summary>
        /// <param name="_params">An empty array.</param>
        /// <returns>The amount of unclaimed GAS as a string.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open.</exception>
        [RpcMethod]
        protected internal virtual JToken GetWalletUnclaimedGas(JArray _params)
        {
            CheckWallet();
            // Datoshi is the smallest unit of GAS, 1 GAS = 10^8 Datoshi
            BigInteger datoshi = BigInteger.Zero;
            using (var snapshot = system.GetSnapshotCache())
            {
                uint height = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
                foreach (UInt160 account in wallet.GetAccounts().Select(p => p.ScriptHash))
                    datoshi += NativeContract.NEO.UnclaimedGas(snapshot, account, height);
            }
            return datoshi.ToString();
        }

        /// <summary>
        /// Imports a private key into the wallet.
        /// </summary>
        /// <param name="_params">An array containing the private key as a string.</param>
        /// <returns>A JSON object containing information about the imported account.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open or the private key is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken ImportPrivKey(JArray _params)
        {
            CheckWallet();
            string privkey = _params[0].AsString();
            WalletAccount account = wallet.Import(privkey);
            if (wallet is NEP6Wallet nep6wallet)
                nep6wallet.Save();
            return new JObject
            {
                ["address"] = account.Address,
                ["haskey"] = account.HasKey,
                ["label"] = account.Label,
                ["watchonly"] = account.WatchOnly
            };
        }

        /// <summary>
        /// Calculates the network fee for a given transaction.
        /// </summary>
        /// <param name="_params">An array containing the Base64-encoded serialized transaction.</param>
        /// <returns>A JSON object containing the calculated network fee.</returns>
        /// <exception cref="RpcException">Thrown when the input parameters are invalid or the transaction is malformed.</exception>
        [RpcMethod]
        protected internal virtual JToken CalculateNetworkFee(JArray _params)
        {
            if (_params.Count == 0)
            {
                throw new RpcException(RpcError.InvalidParams.WithData("Params array is empty, need a raw transaction."));
            }
            var tx = Result.Ok_Or(() => Convert.FromBase64String(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid tx: {_params[0]}")); ;

            JObject account = new();
            var networkfee = Wallets.Helper.CalculateNetworkFee(
                tx.AsSerializable<Transaction>(), system.StoreView, system.Settings,
                wallet is not null ? a => wallet.GetAccount(a).Contract.Script : _ => null);
            account["networkfee"] = networkfee.ToString();
            return account;
        }

        /// <summary>
        /// Lists all addresses in the wallet.
        /// </summary>
        /// <param name="_params">An empty array.</param>
        /// <returns>An array of JSON objects, each containing information about an address in the wallet.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open.</exception>
        [RpcMethod]
        protected internal virtual JToken ListAddress(JArray _params)
        {
            CheckWallet();
            return wallet.GetAccounts().Select(p =>
            {
                JObject account = new();
                account["address"] = p.Address;
                account["haskey"] = p.HasKey;
                account["label"] = p.Label;
                account["watchonly"] = p.WatchOnly;
                return account;
            }).ToArray();
        }

        /// <summary>
        /// Opens a wallet file.
        /// </summary>
        /// <param name="_params">An array containing the wallet path and password.</param>
        /// <returns>Returns true if the wallet was successfully opened.</returns>
        /// <exception cref="RpcException">Thrown when the wallet file is not found, the wallet is not supported, or the password is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken OpenWallet(JArray _params)
        {
            string path = _params[0].AsString();
            string password = _params[1].AsString();
            File.Exists(path).True_Or(RpcError.WalletNotFound);
            try
            {
                wallet = Wallet.Open(path, password, system.Settings).NotNull_Or(RpcError.WalletNotSupported);
            }
            catch (NullReferenceException)
            {
                throw new RpcException(RpcError.WalletNotSupported);
            }
            catch (InvalidOperationException)
            {
                throw new RpcException(RpcError.WalletNotSupported.WithData("Invalid password."));
            }

            return true;
        }

        /// <summary>
        /// Processes the result of an invocation with wallet for signing.
        /// </summary>
        /// <param name="result">The result object to process.</param>
        /// <param name="signers">Optional signers for the transaction.</param>
        private void ProcessInvokeWithWallet(JObject result, Signer[] signers = null)
        {
            if (wallet == null || signers == null || signers.Length == 0) return;

            UInt160 sender = signers[0].Account;
            Transaction tx;
            try
            {
                tx = wallet.MakeTransaction(system.StoreView, Convert.FromBase64String(result["script"].AsString()), sender, signers, maxGas: settings.MaxGasInvoke);
            }
            catch (Exception e)
            {
                result["exception"] = GetExceptionMessage(e);
                return;
            }
            ContractParametersContext context = new(system.StoreView, tx, settings.Network);
            wallet.Sign(context);
            if (context.Completed)
            {
                tx.Witnesses = context.GetWitnesses();
                result["tx"] = Convert.ToBase64String(tx.ToArray());
            }
            else
            {
                result["pendingsignature"] = context.ToJson();
            }
        }

        /// <summary>
        /// Transfers an asset from a specific address to another address.
        /// </summary>
        /// <param name="_params">An array containing asset ID, from address, to address, amount, and optional signers.</param>
        /// <returns>The transaction details if successful, or the contract parameters if signatures are incomplete.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open, parameters are invalid, or there are insufficient funds.</exception>
        [RpcMethod]
        protected internal virtual JToken SendFrom(JArray _params)
        {
            CheckWallet();
            UInt160 assetId = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid asset id: {_params[0]}"));
            UInt160 from = AddressToScriptHash(_params[1].AsString(), system.Settings.AddressVersion);
            UInt160 to = AddressToScriptHash(_params[2].AsString(), system.Settings.AddressVersion);
            using var snapshot = system.GetSnapshotCache();
            AssetDescriptor descriptor = new(snapshot, system.Settings, assetId);
            BigDecimal amount = new(BigInteger.Parse(_params[3].AsString()), descriptor.Decimals);
            (amount.Sign > 0).True_Or(RpcErrorFactory.InvalidParams("Amount can't be negative."));
            Signer[] signers = _params.Count >= 5 ? ((JArray)_params[4]).Select(p => new Signer() { Account = AddressToScriptHash(p.AsString(), system.Settings.AddressVersion), Scopes = WitnessScope.CalledByEntry }).ToArray() : null;

            Transaction tx = Result.Ok_Or(() => wallet.MakeTransaction(snapshot, new[]
            {
                new TransferOutput
                {
                    AssetId = assetId,
                    Value = amount,
                    ScriptHash = to
                }
            }, from, signers), RpcError.InvalidRequest.WithData("Can not process this request.")).NotNull_Or(RpcError.InsufficientFunds);

            ContractParametersContext transContext = new(snapshot, tx, settings.Network);
            wallet.Sign(transContext);
            if (!transContext.Completed)
                return transContext.ToJson();
            tx.Witnesses = transContext.GetWitnesses();
            if (tx.Size > 1024)
            {
                long calFee = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot) + 100000;
                if (tx.NetworkFee < calFee)
                    tx.NetworkFee = calFee;
            }
            (tx.NetworkFee <= settings.MaxFee).True_Or(RpcError.WalletFeeLimit);
            return SignAndRelay(snapshot, tx);
        }

        /// <summary>
        /// Transfers assets to multiple addresses.
        /// </summary>
        /// <param name="_params">
        /// An array containing the following elements:
        /// [0] (optional): The address to send from as a string. If omitted, the assets will be sent from any address in the wallet.
        /// [1]: An array of transfer objects, each containing:
        ///     - "asset": The asset ID (UInt160) as a string.
        ///     - "value": The amount to transfer as a string.
        ///     - "address": The recipient address as a string.
        /// [2] (optional): An array of signers, each containing:
        ///     - The address of the signer as a string.
        /// </param>
        /// <returns>
        /// If the transaction is successfully created and all signatures are present:
        ///     Returns a JSON object representing the transaction.
        /// If not all signatures are present:
        ///     Returns a JSON object representing the contract parameters that need to be signed.
        /// </returns>
        /// <exception cref="RpcException">
        /// Thrown when:
        /// - No wallet is open.
        /// - The 'to' parameter is invalid or empty.
        /// - Any of the asset IDs are invalid.
        /// - Any of the amounts are negative or invalid.
        /// - Any of the addresses are invalid.
        /// - There are insufficient funds for the transfer.
        /// - The network fee exceeds the maximum allowed fee.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken SendMany(JArray _params)
        {
            CheckWallet();
            int to_start = 0;
            UInt160 from = null;
            if (_params[0] is JString)
            {
                from = AddressToScriptHash(_params[0].AsString(), system.Settings.AddressVersion);
                to_start = 1;
            }
            JArray to = Result.Ok_Or(() => (JArray)_params[to_start], RpcError.InvalidParams.WithData($"Invalid 'to' parameter: {_params[to_start]}"));
            (to.Count != 0).True_Or(RpcErrorFactory.InvalidParams("Argument 'to' can't be empty."));
            Signer[] signers = _params.Count >= to_start + 2 ? ((JArray)_params[to_start + 1]).Select(p => new Signer() { Account = AddressToScriptHash(p.AsString(), system.Settings.AddressVersion), Scopes = WitnessScope.CalledByEntry }).ToArray() : null;

            TransferOutput[] outputs = new TransferOutput[to.Count];
            using var snapshot = system.GetSnapshotCache();
            for (int i = 0; i < to.Count; i++)
            {
                UInt160 asset_id = UInt160.Parse(to[i]["asset"].AsString());
                AssetDescriptor descriptor = new(snapshot, system.Settings, asset_id);
                outputs[i] = new TransferOutput
                {
                    AssetId = asset_id,
                    Value = new BigDecimal(BigInteger.Parse(to[i]["value"].AsString()), descriptor.Decimals),
                    ScriptHash = AddressToScriptHash(to[i]["address"].AsString(), system.Settings.AddressVersion)
                };
                (outputs[i].Value.Sign > 0).True_Or(RpcErrorFactory.InvalidParams($"Amount of '{asset_id}' can't be negative."));
            }
            Transaction tx = wallet.MakeTransaction(snapshot, outputs, from, signers).NotNull_Or(RpcError.InsufficientFunds);

            ContractParametersContext transContext = new(snapshot, tx, settings.Network);
            wallet.Sign(transContext);
            if (!transContext.Completed)
                return transContext.ToJson();
            tx.Witnesses = transContext.GetWitnesses();
            if (tx.Size > 1024)
            {
                long calFee = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot) + 100000;
                if (tx.NetworkFee < calFee)
                    tx.NetworkFee = calFee;
            }
            (tx.NetworkFee <= settings.MaxFee).True_Or(RpcError.WalletFeeLimit);
            return SignAndRelay(snapshot, tx);
        }

        /// <summary>
        /// Transfers an asset to a specific address.
        /// </summary>
        /// <param name="_params">An array containing asset ID, to address, and amount.</param>
        /// <returns>The transaction details if successful, or the contract parameters if signatures are incomplete.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open, parameters are invalid, or there are insufficient funds.</exception>
        [RpcMethod]
        protected internal virtual JToken SendToAddress(JArray _params)
        {
            CheckWallet();
            UInt160 assetId = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid asset hash: {_params[0]}"));
            UInt160 to = AddressToScriptHash(_params[1].AsString(), system.Settings.AddressVersion);
            using var snapshot = system.GetSnapshotCache();
            AssetDescriptor descriptor = new(snapshot, system.Settings, assetId);
            BigDecimal amount = new(BigInteger.Parse(_params[2].AsString()), descriptor.Decimals);
            (amount.Sign > 0).True_Or(RpcError.InvalidParams);
            Transaction tx = wallet.MakeTransaction(snapshot, new[]
            {
                new TransferOutput
                {
                    AssetId = assetId,
                    Value = amount,
                    ScriptHash = to
                }
            }).NotNull_Or(RpcError.InsufficientFunds);

            ContractParametersContext transContext = new(snapshot, tx, settings.Network);
            wallet.Sign(transContext);
            if (!transContext.Completed)
                return transContext.ToJson();
            tx.Witnesses = transContext.GetWitnesses();
            if (tx.Size > 1024)
            {
                long calFee = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot) + 100000;
                if (tx.NetworkFee < calFee)
                    tx.NetworkFee = calFee;
            }
            (tx.NetworkFee <= settings.MaxFee).True_Or(RpcError.WalletFeeLimit);
            return SignAndRelay(snapshot, tx);
        }

        /// <summary>
        /// Cancels an unconfirmed transaction.
        /// </summary>
        /// <param name="_params">An array containing the transaction ID to cancel, signers, and optional extra fee.</param>
        /// <returns>The details of the cancellation transaction.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open, the transaction is already confirmed, or there are insufficient funds for the cancellation fee.</exception>
        [RpcMethod]
        protected internal virtual JToken CancelTransaction(JArray _params)
        {
            CheckWallet();
            var txid = Result.Ok_Or(() => UInt256.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid txid: {_params[0]}"));
            NativeContract.Ledger.GetTransactionState(system.StoreView, txid).Null_Or(RpcErrorFactory.AlreadyExists("This tx is already confirmed, can't be cancelled."));

            var conflict = new TransactionAttribute[] { new Conflicts() { Hash = txid } };
            Signer[] signers = _params.Count >= 2 ? ((JArray)_params[1]).Select(j => new Signer() { Account = AddressToScriptHash(j.AsString(), system.Settings.AddressVersion), Scopes = WitnessScope.None }).ToArray() : Array.Empty<Signer>();
            signers.Any().True_Or(RpcErrorFactory.BadRequest("No signer."));
            Transaction tx = new Transaction
            {
                Signers = signers,
                Attributes = conflict,
                Witnesses = Array.Empty<Witness>(),
            };

            tx = Result.Ok_Or(() => wallet.MakeTransaction(system.StoreView, new[] { (byte)OpCode.RET }, signers[0].Account, signers, conflict), RpcError.InsufficientFunds, true);

            if (system.MemPool.TryGetValue(txid, out Transaction conflictTx))
            {
                tx.NetworkFee = Math.Max(tx.NetworkFee, conflictTx.NetworkFee) + 1;
            }
            else if (_params.Count >= 3)
            {
                var extraFee = _params[2].AsString();
                AssetDescriptor descriptor = new(system.StoreView, system.Settings, NativeContract.GAS.Hash);
                (BigDecimal.TryParse(extraFee, descriptor.Decimals, out BigDecimal decimalExtraFee) && decimalExtraFee.Sign > 0).True_Or(RpcErrorFactory.InvalidParams("Incorrect amount format."));

                tx.NetworkFee += (long)decimalExtraFee.Value;
            };
            return SignAndRelay(system.StoreView, tx);
        }

        /// <summary>
        /// Invokes the verify method of a contract.
        /// </summary>
        /// <param name="_params">An array containing the script hash, optional arguments, and optional signers and witnesses.</param>
        /// <returns>A JSON object containing the result of the verification.</returns>
        /// <exception cref="RpcException">Thrown when the script hash is invalid, the contract is not found, or the verification fails.</exception>
        [RpcMethod]
        protected internal virtual JToken InvokeContractVerify(JArray _params)
        {
            UInt160 script_hash = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid script hash: {_params[0]}"));
            ContractParameter[] args = _params.Count >= 2 ? ((JArray)_params[1]).Select(p => ContractParameter.FromJson((JObject)p)).ToArray() : Array.Empty<ContractParameter>();
            Signer[] signers = _params.Count >= 3 ? SignerOrWitness.ParseArray((JArray)_params[2], system.Settings).Where(u => u.IsSigner).Select(u => u.AsSigner()).ToArray() : null;
            Witness[] witnesses = _params.Count >= 3 ? SignerOrWitness.ParseArray((JArray)_params[2], system.Settings).Where(u => !u.IsSigner).Select(u => u.AsWitness()).ToArray() : null;
            return GetVerificationResult(script_hash, args, signers, witnesses);
        }

        /// <summary>
        /// Gets the result of the contract verification.
        /// </summary>
        /// <param name="scriptHash">The script hash of the contract.</param>
        /// <param name="args">The contract parameters.</param>
        /// <param name="signers">Optional signers for the verification.</param>
        /// <param name="witnesses">Optional witnesses for the verification.</param>
        /// <returns>A JSON object containing the verification result.</returns>
        private JObject GetVerificationResult(UInt160 scriptHash, ContractParameter[] args, Signer[] signers = null, Witness[] witnesses = null)
        {
            using var snapshot = system.GetSnapshotCache();
            var contract = NativeContract.ContractManagement.GetContract(snapshot, scriptHash).NotNull_Or(RpcError.UnknownContract);
            var md = contract.Manifest.Abi.GetMethod(ContractBasicMethod.Verify, args.Count()).NotNull_Or(RpcErrorFactory.InvalidContractVerification(contract.Hash, args.Count()));
            (md.ReturnType == ContractParameterType.Boolean).True_Or(RpcErrorFactory.InvalidContractVerification("The verify method doesn't return boolean value."));
            Transaction tx = new()
            {
                Signers = signers ?? new Signer[] { new() { Account = scriptHash } },
                Attributes = Array.Empty<TransactionAttribute>(),
                Witnesses = witnesses,
                Script = new[] { (byte)OpCode.RET }
            };
            using ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot.CloneCache(), settings: system.Settings);
            engine.LoadContract(contract, md, CallFlags.ReadOnly);

            var invocationScript = Array.Empty<byte>();
            if (args.Length > 0)
            {
                using ScriptBuilder sb = new();
                for (int i = args.Length - 1; i >= 0; i--)
                    sb.EmitPush(args[i]);

                invocationScript = sb.ToArray();
                tx.Witnesses ??= new Witness[] { new() { InvocationScript = invocationScript } };
                engine.LoadScript(new Script(invocationScript), configureState: p => p.CallFlags = CallFlags.None);
            }
            JObject json = new();
            json["script"] = Convert.ToBase64String(invocationScript);
            json["state"] = engine.Execute();
            // Gas consumed in the unit of datoshi, 1 GAS = 1e8 datoshi
            json["gasconsumed"] = engine.FeeConsumed.ToString();
            json["exception"] = GetExceptionMessage(engine.FaultException);
            try
            {
                json["stack"] = new JArray(engine.ResultStack.Select(p => p.ToJson(settings.MaxStackSize)));
            }
            catch (Exception ex)
            {
                json["exception"] = ex.Message;
            }
            return json;
        }

        /// <summary>
        /// Signs and relays a transaction.
        /// </summary>
        /// <param name="snapshot">The data snapshot.</param>
        /// <param name="tx">The transaction to sign and relay.</param>
        /// <returns>A JSON object containing the transaction details.</returns>
        private JObject SignAndRelay(DataCache snapshot, Transaction tx)
        {
            ContractParametersContext context = new(snapshot, tx, settings.Network);
            wallet.Sign(context);
            if (context.Completed)
            {
                tx.Witnesses = context.GetWitnesses();
                system.Blockchain.Tell(tx);
                return Utility.TransactionToJson(tx, system.Settings);
            }
            else
            {
                return context.ToJson();
            }
        }

        /// <summary>
        /// Converts an address to a script hash.
        /// </summary>
        /// <param name="address">The address to convert.</param>
        /// <param name="version">The address version.</param>
        /// <returns>The script hash corresponding to the address.</returns>
        internal static UInt160 AddressToScriptHash(string address, byte version)
        {
            if (UInt160.TryParse(address, out var scriptHash))
            {
                return scriptHash;
            }

            return address.ToScriptHash(version);
        }
    }
}
