// Copyright (C) 2015-2025 The Neo Project.
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
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
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
using Helper = Neo.Wallets.Helper;

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
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "closewallet", "params": []}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": true}</code>
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
        /// <para>Request format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1, "method": "dumpprivkey", "params": ["An UInt160 or Base58Check address"]}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": "A WIF-encoded private key as a string"}</code>
        /// </summary>
        /// <param name="_params">An 1-element array containing the address(UInt160 or Base58Check address) as a string.</param>
        /// <returns>The exported private key as a string.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open or the address is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken DumpPrivKey(JArray _params)
        {
            RpcException.ThrowIfTooFew(_params, 1, RpcError.InvalidParams); // Address
            CheckWallet();

            var scriptHash = _params[0].AsString().AddressToScriptHash(system.Settings.AddressVersion);
            var account = wallet.GetAccount(scriptHash);
            return account.GetKey().Export();
        }

        /// <summary>
        /// Creates a new address in the wallet.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getnewaddress", "params": []}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": "The newly created Base58Check address"}</code>
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
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getwalletbalance", "params": ["An UInt160 address"]}</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {"balance": "0"} // An integer number in string, the balance of the specified asset in the wallet
        /// }</code>
        /// </summary>
        /// <param name="_params">An 1-element(UInt160) array containing the asset ID as a string.</param>
        /// <returns>A JSON object containing the balance of the specified asset.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open or the asset ID is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken GetWalletBalance(JArray _params)
        {
            RpcException.ThrowIfTooFew(_params, 1, RpcError.InvalidParams); // AssetId
            CheckWallet();

            UInt160 asset_id = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid asset id: {_params[0]}"));
            JObject json = new();
            json["balance"] = wallet.GetAvailable(system.StoreView, asset_id).Value.ToString();
            return json;
        }

        /// <summary>
        /// Gets the amount of unclaimed GAS in the wallet.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getwalletunclaimedgas", "params": []}</code>
        /// <para>Response format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1, "result": "The amount of unclaimed GAS(an integer number in string)"}
        /// </code>
        /// </summary>
        /// <param name="_params">An empty array.</param>
        /// <returns>The amount of unclaimed GAS(an integer number in string).</returns>
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
        /// <para>Request format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1, "method": "importprivkey", "params": ["A WIF-encoded private key"]}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {"address": "The Base58Check address", "haskey": true, "label": "The label", "watchonly": false}
        /// }</code>
        /// </summary>
        /// <param name="_params">An 1-element(WIF-encoded private key) array containing the private key as a string.</param>
        /// <returns>A JSON object containing information about the imported account.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open or the private key is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken ImportPrivKey(JArray _params)
        {
            RpcException.ThrowIfTooFew(_params, 1, RpcError.InvalidParams); // PrivateKey
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
        /// <para>Request format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1, "method": "calculatenetworkfee", "params": ["A Base64-encoded transaction"]}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": {"networkfee": "The network fee(an integer number in string)"}}</code>
        /// </summary>
        /// <param name="_params">An array containing the Base64-encoded transaction.</param>
        /// <returns>A JSON object containing the calculated network fee.</returns>
        /// <exception cref="RpcException">Thrown when the input parameters are invalid or the transaction is malformed.</exception>
        [RpcMethod]
        protected internal virtual JToken CalculateNetworkFee(JArray _params)
        {
            RpcException.ThrowIfTooFew(_params, 1, RpcError.InvalidParams); // Tx

            var tx = Result.Ok_Or(() => Convert.FromBase64String(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid tx: {_params[0]}"));

            JObject account = new();
            var networkfee = Helper.CalculateNetworkFee(tx.AsSerializable<Transaction>(), system.StoreView, system.Settings, wallet);
            account["networkfee"] = networkfee.ToString();
            return account;
        }

        /// <summary>
        /// Lists all addresses in the wallet.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "listaddress", "params": []}</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": [{"address": "address", "haskey": true, "label": "label", "watchonly": false} ]
        /// }</code>
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
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "openwallet", "params": ["path", "password"]}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": true}</code>
        /// </summary>
        /// <param name="_params">
        /// An array containing the following elements:
        /// [0]: The path to the wallet file as a string.
        /// [1]: The password to open the wallet as a string.
        /// </param>
        /// <returns>Returns true if the wallet was successfully opened.</returns>
        /// <exception cref="RpcException">
        /// Thrown when the wallet file is not found, the wallet is not supported, or the password is invalid.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken OpenWallet(JArray _params)
        {
            RpcException.ThrowIfTooFew(_params, 2, RpcError.InvalidParams); // Path, Password

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
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "sendfrom",
        ///   "params": [
        ///     "An UInt160 assetId",
        ///     "An UInt160 from address",
        ///     "An UInt160 to address",
        ///     "An amount as a string(An integer/decimal number in string)",
        ///     ["UInt160 or Base58Check address"] // signers is optional
        ///   ]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "hash": "The tx hash(UInt256)", // The hash of the transaction
        ///     "size": 272, // The size of the tx
        ///     "version": 0, // The version of the tx
        ///     "nonce": 1553700339, // The nonce of the tx
        ///     "sender": "The Base58Check address", // The sender of the tx
        ///     "sysfee": "100000000", // The system fee of the tx
        ///     "netfee": "1272390", // The network fee of the tx
        ///     "validuntilblock": 2105487, // The valid until block of the tx
        ///     "attributes": [], // The attributes of the tx
        ///     "signers": [{"account": "The UInt160 address", "scopes": "CalledByEntry"}], // The signers of the tx
        ///     "script": "A Base64-encoded script",
        ///     "witnesses": [{"invocation": "A Base64-encoded string", "verification": "A Base64-encoded string"}] // The witnesses of the tx
        ///   }
        /// }</code>
        /// </summary>
        /// <param name="_params">
        /// An array containing the following elements:
        /// [0]: The asset ID as a string.
        /// [1]: The from address as a string.
        /// [2]: The to address as a string.
        /// [3]: The amount as a string.
        /// [4] (optional): An array of signers, each containing:
        ///     - The address of the signer as a string.
        /// </param>
        /// <returns>The transaction details if successful, or the contract parameters if signatures are incomplete.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open, parameters are invalid, or there are insufficient funds.</exception>
        [RpcMethod]
        protected internal virtual JToken SendFrom(JArray _params)
        {
            RpcException.ThrowIfTooFew(_params, 4, RpcError.InvalidParams); // AssetId, From, To, Amount
            CheckWallet();

            var assetId = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid asset id: {_params[0]}"));
            var from = _params[1].AsString().AddressToScriptHash(system.Settings.AddressVersion);
            var to = _params[2].AsString().AddressToScriptHash(system.Settings.AddressVersion);

            using var snapshot = system.GetSnapshotCache();
            var descriptor = new AssetDescriptor(snapshot, system.Settings, assetId);
            var amount = new BigDecimal(BigInteger.Parse(_params[3].AsString()), descriptor.Decimals);
            (amount.Sign > 0).True_Or(RpcErrorFactory.InvalidParams("Amount can't be negative."));
            var signers = _params.Count >= 5
                ? ((JArray)_params[4]).Select(p => new Signer() { Account = p.AsString().AddressToScriptHash(system.Settings.AddressVersion), Scopes = WitnessScope.CalledByEntry }).ToArray()
                : null;

            var tx = Result.Ok_Or(() => wallet.MakeTransaction(snapshot,
            [
                new TransferOutput
                {
                    AssetId = assetId,
                    Value = amount,
                    ScriptHash = to
                }
            ], from, signers), RpcError.InvalidRequest.WithData("Can not process this request.")).NotNull_Or(RpcError.InsufficientFunds);

            var transContext = new ContractParametersContext(snapshot, tx, settings.Network);
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
        /// <para>Request format:</para>
        /// <code>{
        ///  "jsonrpc": "2.0",
        ///  "id": 1,
        ///  "method": "sendmany",
        ///  "params": [
        ///     "An UInt160 address",  // "from", optional
        ///     [{"asset": "An UInt160 assetId", "value": "An integer/decimal as a string", "address": "An UInt160 address"}],
        ///     ["UInt160 or Base58Check address"] // signers, optional
        ///   ]
        /// }</code>
        /// <para>Response format:</para>
        /// <code> {
        ///  "jsonrpc": "2.0", 
        ///  "id": 1, 
        ///   "result": {
        ///     "hash": "The tx hash(UInt256)", // The hash of the transaction
        ///     "size": 483, // The size of the tx
        ///     "version": 0, // The version of the tx
        ///     "nonce": 34429660, // The nonce of the tx
        ///     "sender": "The Base58Check address", // The sender of the tx
        ///     "sysfee": "100000000", // The system fee of the tx
        ///     "netfee": "2483780", // The network fee of the tx
        ///     "validuntilblock": 2105494, // The valid until block of the tx
        ///     "attributes": [], // The attributes of the tx
        ///     "signers": [{"account": "The UInt160 address", "scopes": "CalledByEntry"}], // The signers of the tx
        ///     "script": "A Base64-encoded script",
        ///     "witnesses": [{"invocation": "A Base64-encoded string", "verification": "A Base64-encoded string" }] // The witnesses of the tx
        ///   }
        /// }</code>
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
            RpcException.ThrowIfTooFew(_params, 1, RpcError.InvalidParams); // From
            CheckWallet();

            int to_start = 0;
            UInt160 from = null;
            if (_params[0] is JString)
            {
                from = _params[0].AsString().AddressToScriptHash(system.Settings.AddressVersion);
                to_start = 1;
            }

            JArray to = Result.Ok_Or(() => (JArray)_params[to_start], RpcError.InvalidParams.WithData($"Invalid 'to' parameter: {_params[to_start]}"));
            (to.Count != 0).True_Or(RpcErrorFactory.InvalidParams("Argument 'to' can't be empty."));

            var signers = _params.Count >= to_start + 2
                ? ((JArray)_params[to_start + 1]).Select(p => new Signer() { Account = p.AsString().AddressToScriptHash(system.Settings.AddressVersion), Scopes = WitnessScope.CalledByEntry }).ToArray()
                : null;

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
                    ScriptHash = to[i]["address"].AsString().AddressToScriptHash(system.Settings.AddressVersion)
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
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "sendtoaddress",
        ///   "params": ["An UInt160 assetId", "An UInt160 address(to)", "An amount as a string(An integer/decimal number)"]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "hash": "The tx hash(UInt256)", // The hash of the transaction
        ///     "size": 483, // The size of the tx
        ///     "version": 0, // The version of the tx
        ///     "nonce": 34429660, // The nonce of the tx
        ///     "sender": "The Base58Check address", // The sender of the tx
        ///     "sysfee": "100000000", // The system fee of the tx
        ///     "netfee": "2483780", // The network fee of the tx
        ///     "validuntilblock": 2105494, // The valid until block of the tx
        ///     "attributes": [], // The attributes of the tx
        ///     "signers": [{"account": "The UInt160 address", "scopes": "CalledByEntry"}], // The signers of the tx
        ///     "script": "A Base64-encoded script",
        ///     "witnesses": [{"invocation": "A Base64-encoded string", "verification": "A Base64-encoded string"}] // The witnesses of the tx
        ///   }
        /// }</code>
        /// </summary>
        /// <param name="_params">
        /// An array containing the following elements:
        /// [0]: The asset ID as a string.
        /// [1]: The to address as a string.
        /// [2]: The amount as a string.
        /// </param>
        /// <returns>The transaction details if successful, or the contract parameters if signatures are incomplete.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open, parameters are invalid, or there are insufficient funds.</exception>
        [RpcMethod]
        protected internal virtual JToken SendToAddress(JArray _params)
        {
            RpcException.ThrowIfTooFew(_params, 3, RpcError.InvalidParams); // AssetId, To, Amount
            CheckWallet();

            var assetId = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()),
                RpcError.InvalidParams.WithData($"Invalid asset hash: {_params[0]}"));
            var to = _params[1].AsString().AddressToScriptHash(system.Settings.AddressVersion);

            using var snapshot = system.GetSnapshotCache();
            var descriptor = new AssetDescriptor(snapshot, system.Settings, assetId);
            var amount = new BigDecimal(BigInteger.Parse(_params[2].AsString()), descriptor.Decimals);
            (amount.Sign > 0).True_Or(RpcError.InvalidParams);
            var tx = wallet.MakeTransaction(snapshot,
            [
                new TransferOutput
                {
                    AssetId = assetId,
                    Value = amount,
                    ScriptHash = to
                }
            ]).NotNull_Or(RpcError.InsufficientFunds);

            var transContext = new ContractParametersContext(snapshot, tx, settings.Network);
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
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "canceltransaction",
        ///   "params": [
        ///    "An tx hash(UInt256)",
        ///     ["UInt160 or Base58Check address"], // signers, optional
        ///     "An amount as a string(An integer/decimal number)" // extraFee, optional
        ///    ]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "hash": "The tx hash(UInt256)", // The hash of the transaction
        ///     "size": 483, // The size of the tx
        ///     "version": 0, // The version of the tx
        ///     "nonce": 34429660, // The nonce of the tx
        ///     "sender": "The Base58Check address", // The sender of the tx
        ///     "sysfee": "100000000", // A integer number in string
        ///     "netfee": "2483780", // A integer number in string
        ///     "validuntilblock": 2105494, // The valid until block of the tx
        ///     "attributes": [], // The attributes of the tx
        ///     "signers": [{"account": "The UInt160 address", "scopes": "CalledByEntry"}], // The signers of the tx
        ///     "script": "A Base64-encoded script",
        ///     "witnesses": [{"invocation": "A Base64-encoded string", "verification": "A Base64-encoded string"}] // The witnesses of the tx
        ///   }
        /// }</code>
        /// </summary>
        /// <param name="_params">
        /// An array containing the following elements:
        /// [0]: The transaction ID to cancel as a string.
        /// [1]: The signers as an array of strings.
        /// [2]: The extra fee as a string.
        /// </param>
        /// <returns>The details of the cancellation transaction.</returns>
        /// <exception cref="RpcException">
        /// Thrown when no wallet is open, the transaction is already confirmed, or there are insufficient funds for the cancellation fee.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken CancelTransaction(JArray _params)
        {
            RpcException.ThrowIfTooFew(_params, 1, RpcError.InvalidParams); // Txid
            CheckWallet();
            var txid = Result.Ok_Or(() => UInt256.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid txid: {_params[0]}"));
            NativeContract.Ledger.GetTransactionState(system.StoreView, txid).Null_Or(RpcErrorFactory.AlreadyExists("This tx is already confirmed, can't be cancelled."));

            var conflict = new TransactionAttribute[] { new Conflicts() { Hash = txid } };
            var signers = _params.Count >= 2
                ? ((JArray)_params[1]).Select(j => new Signer() { Account = j.AsString().AddressToScriptHash(system.Settings.AddressVersion), Scopes = WitnessScope.None }).ToArray()
                : [];
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
            }
            return SignAndRelay(system.StoreView, tx);
        }

        /// <summary>
        /// Invokes the verify method of a contract.
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "invokecontractverify",
        ///   "params": [
        ///     "The script hash(UInt160)",
        ///     [
        ///      { "type": "The type of the parameter", "value": "The value of the parameter" }
        ///      // ...
        ///     ], // The arguments as an array of ContractParameter JSON objects
        ///     [{
        ///       // The part of the Signer
        ///       "account": "An UInt160 or Base58Check address", // The account of the signer, required
        ///       "scopes": "WitnessScope", // WitnessScope, required
        ///       "allowedcontracts": ["UInt160 address"], // optional
        ///       "allowedgroups": ["PublicKey"], // ECPoint, i.e. ECC PublicKey, optional
        ///       "rules": [{"action": "WitnessRuleAction", "condition": {/*A json of WitnessCondition*/}}], // WitnessRule
        ///        // The part of the Witness, optional
        ///       "invocation": "A Base64-encoded string",
        ///       "verification": "A Base64-encoded string"
        ///     }], // A JSON array of signers and witnesses, optional
        ///   ]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "script": "A Base64-encoded string",
        ///     "state": "A string of VMState",
        ///     "gasconsumed": "An integer number in string",
        ///     "exception": "The exception message",
        ///     "stack": [{"type": "The stack item type", "value": "The stack item value"}]
        ///   }
        /// }</code>
        /// </summary>
        /// <param name="_params">
        /// An array containing the following elements:
        /// [0]: The script hash as a string.
        /// [1]: The arguments as an array of strings.
        /// [2]: The JSON array of signers and witnesses<see cref="ParameterConverter.ToSignersAndWitnesses"/>. Optional.
        /// </param>
        /// <returns>A JSON object containing the result of the verification.</returns>
        /// <exception cref="RpcException">
        /// Thrown when the script hash is invalid, the contract is not found, or the verification fails.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken InvokeContractVerify(JArray _params)
        {
            RpcException.ThrowIfTooFew(_params, 1, RpcError.InvalidParams); // ScriptHash

            var scriptHash = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()),
                RpcError.InvalidParams.WithData($"Invalid script hash: {_params[0]}"));

            var args = _params.Count >= 2
                ? ((JArray)_params[1]).Select(p => ContractParameter.FromJson((JObject)p)).ToArray()
                : [];

            var (signers, witnesses) = _params.Count >= 3
                ? ((JArray)_params[2]).ToSignersAndWitnesses(system.Settings.AddressVersion)
                : (null, null);
            return GetVerificationResult(scriptHash, args, signers, witnesses);
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
    }
}
