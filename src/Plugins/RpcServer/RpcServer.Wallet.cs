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
using Address = Neo.Plugins.RpcServer.Model.Address;
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
            public override WalletAccount? CreateAccount(byte[] privateKey) => null;
            public override WalletAccount? CreateAccount(Contract contract, KeyPair? key = null) => null;
            public override WalletAccount? CreateAccount(UInt160 scriptHash) => null;
            public override void Delete() { }
            public override bool DeleteAccount(UInt160 scriptHash) => false;
            public override WalletAccount? GetAccount(UInt160 scriptHash) => null;
            public override IEnumerable<WalletAccount> GetAccounts() => [];
            public override bool VerifyPassword(string password) => false;
            public override void Save() { }
        }

        protected internal Wallet? wallet;

        /// <summary>
        /// Checks if a wallet is open and throws an error if not.
        /// </summary>
        private Wallet CheckWallet()
        {
            return wallet.NotNull_Or(RpcError.NoOpenedWallet);
        }

        /// <summary>
        /// Closes the currently opened wallet.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "closewallet", "params": []}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": true}</code>
        /// </summary>
        /// <returns>Returns true if the wallet was successfully closed.</returns>
        [RpcMethod]
        protected internal virtual JToken CloseWallet()
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
        /// <param name="address">The address(UInt160 or Base58Check address) to export the private key for.</param>
        /// <returns>The exported private key as a string.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open or the address is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken DumpPrivKey(Address address)
        {
            return CheckWallet().GetAccount(address.ScriptHash)
                .NotNull_Or(RpcError.UnknownAccount.WithData($"{address.ScriptHash}"))
                .GetKey()
                .Export();
        }

        /// <summary>
        /// Creates a new address in the wallet.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getnewaddress", "params": []}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": "The newly created Base58Check address"}</code>
        /// </summary>
        /// <returns>The newly created address as a string.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open.</exception>
        [RpcMethod]
        protected internal virtual JToken GetNewAddress()
        {
            var wallet = CheckWallet();
            var account = wallet.CreateAccount();
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
        /// <param name="assetId">An 1-element(UInt160) array containing the asset ID as a string.</param>
        /// <returns>A JSON object containing the balance of the specified asset.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open or the asset ID is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken GetWalletBalance(UInt160 assetId)
        {
            var balance = CheckWallet().GetAvailable(system.StoreView, assetId).Value;
            return new JObject { ["balance"] = balance.ToString() };
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
        /// <returns>The amount of unclaimed GAS(an integer number in string).</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open.</exception>
        [RpcMethod]
        protected internal virtual JToken GetWalletUnclaimedGas()
        {
            var wallet = CheckWallet();
            // Datoshi is the smallest unit of GAS, 1 GAS = 10^8 Datoshi
            var datoshi = BigInteger.Zero;
            using (var snapshot = system.GetSnapshotCache())
            {
                uint height = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
                foreach (var account in wallet.GetAccounts().Select(p => p.ScriptHash))
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
        /// <param name="privkey">The WIF-encoded private key to import.</param>
        /// <returns>A JSON object containing information about the imported account.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open or the private key is invalid.</exception>
        [RpcMethod]
        protected internal virtual JToken ImportPrivKey(string privkey)
        {
            var wallet = CheckWallet();
            var account = wallet.Import(privkey);
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
        /// <param name="tx">The raw transaction to calculate the network fee for.</param>
        /// <returns>A JSON object containing the calculated network fee.</returns>
        /// <exception cref="RpcException">Thrown when the input parameters are invalid or the transaction is malformed.</exception>
        [RpcMethod]
        protected internal virtual JToken CalculateNetworkFee(byte[] tx)
        {
            var transaction = Result.Ok_Or(() => tx.AsSerializable<Transaction>(), RpcErrorFactory.InvalidParams("Invalid tx."));
            var networkfee = Helper.CalculateNetworkFee(transaction, system.StoreView, system.Settings, wallet);
            return new JObject { ["networkfee"] = networkfee.ToString() };
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
        /// <returns>An array of JSON objects, each containing information about an address in the wallet.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open.</exception>
        [RpcMethod]
        protected internal virtual JToken ListAddress()
        {
            return CheckWallet().GetAccounts().Select(p =>
            {
                return new JObject
                {
                    ["address"] = p.Address,
                    ["haskey"] = p.HasKey,
                    ["label"] = p.Label,
                    ["watchonly"] = p.WatchOnly
                };
            }).ToArray();
        }

        /// <summary>
        /// Opens a wallet file.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "openwallet", "params": ["path", "password"]}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": true}</code>
        /// </summary>
        /// <param name="path">The path to the wallet file.</param>
        /// <param name="password">The password to open the wallet.</param>
        /// <returns>Returns true if the wallet was successfully opened.</returns>
        /// <exception cref="RpcException">
        /// Thrown when the wallet file is not found, the wallet is not supported, or the password is invalid.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken OpenWallet(string path, string password)
        {
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
        /// <param name="script">The script to process.</param>
        /// <param name="signers">Optional signers for the transaction.</param>
        private void ProcessInvokeWithWallet(JObject result, byte[] script, Signer[]? signers = null)
        {
            if (wallet == null || signers == null || signers.Length == 0) return;

            var sender = signers[0].Account;
            Transaction tx;
            try
            {
                tx = wallet.MakeTransaction(system.StoreView, script, sender, signers, maxGas: settings.MaxGasInvoke);
            }
            catch (Exception e)
            {
                result["exception"] = GetExceptionMessage(e);
                return;
            }

            var context = new ContractParametersContext(system.StoreView, tx, settings.Network);
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
        /// <param name="assetId">The asset ID as a string.</param>
        /// <param name="from">The from address as a string.</param>
        /// <param name="to">The to address as a string.</param>
        /// <param name="amount">The amount as a string.</param>
        /// <param name="signers">An array of signers, each containing: The address of the signer as a string.</param>
        /// <returns>The transaction details if successful, or the contract parameters if signatures are incomplete.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open, parameters are invalid, or there are insufficient funds.</exception>
        [RpcMethod]
        protected internal virtual JToken SendFrom(UInt160 assetId, Address from, Address to, string amount, Address[]? signers = null)
        {
            var wallet = CheckWallet();

            using var snapshot = system.GetSnapshotCache();
            var descriptor = new AssetDescriptor(snapshot, system.Settings, assetId);

            var amountDecimal = new BigDecimal(BigInteger.Parse(amount), descriptor.Decimals);
            (amountDecimal.Sign > 0).True_Or(RpcErrorFactory.InvalidParams("Amount can't be negative."));

            var callSigners = signers?.ToSigners(WitnessScope.CalledByEntry);
            var transfer = new TransferOutput { AssetId = assetId, Value = amountDecimal, ScriptHash = to.ScriptHash };
            var tx = Result.Ok_Or(() => wallet.MakeTransaction(snapshot, [transfer], from.ScriptHash, callSigners),
                RpcError.InvalidRequest.WithData("Can not process this request.")).NotNull_Or(RpcError.InsufficientFunds);

            var transContext = new ContractParametersContext(snapshot, tx, settings.Network);
            wallet.Sign(transContext);

            if (!transContext.Completed) return transContext.ToJson();

            tx.Witnesses = transContext.GetWitnesses();
            EnsureNetworkFee(snapshot, tx);

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
            var wallet = CheckWallet();

            int toStart = 0;
            var addressVersion = system.Settings.AddressVersion;
            UInt160? from = null;
            if (_params[0] is JString)
            {
                from = _params[0]!.AsString().AddressToScriptHash(addressVersion);
                toStart = 1;
            }

            JArray to = Result.Ok_Or(() => (JArray)_params[toStart]!, RpcError.InvalidParams.WithData($"Invalid 'to' parameter: {_params[toStart]}"));
            (to.Count != 0).True_Or(RpcErrorFactory.InvalidParams("Argument 'to' can't be empty."));

            var signers = _params.Count >= toStart + 2 && _params[toStart + 1] is not null
                ? _params[toStart + 1]!.ToAddresses(addressVersion).ToSigners(WitnessScope.CalledByEntry)
                : null;

            var outputs = new TransferOutput[to.Count];
            using var snapshot = system.GetSnapshotCache();
            for (int i = 0; i < to.Count; i++)
            {
                var item = to[i].NotNull_Or(RpcErrorFactory.InvalidParams($"Invalid 'to' parameter at {i}."));
                var asset = item["asset"].NotNull_Or(RpcErrorFactory.InvalidParams($"no 'asset' parameter at 'to[{i}]'."));
                var value = item["value"].NotNull_Or(RpcErrorFactory.InvalidParams($"no 'value' parameter at 'to[{i}]'."));
                var address = item["address"].NotNull_Or(RpcErrorFactory.InvalidParams($"no 'address' parameter at 'to[{i}]'."));

                var assetId = UInt160.Parse(asset.AsString());
                var descriptor = new AssetDescriptor(snapshot, system.Settings, assetId);
                outputs[i] = new TransferOutput
                {
                    AssetId = assetId,
                    Value = new BigDecimal(BigInteger.Parse(value.AsString()), descriptor.Decimals),
                    ScriptHash = address.AsString().AddressToScriptHash(system.Settings.AddressVersion)
                };
                (outputs[i].Value.Sign > 0).True_Or(RpcErrorFactory.InvalidParams($"Amount of '{assetId}' can't be negative."));
            }

            var tx = wallet.MakeTransaction(snapshot, outputs, from, signers).NotNull_Or(RpcError.InsufficientFunds);
            var transContext = new ContractParametersContext(snapshot, tx, settings.Network);
            wallet.Sign(transContext);

            if (!transContext.Completed) return transContext.ToJson();

            tx.Witnesses = transContext.GetWitnesses();
            EnsureNetworkFee(snapshot, tx);

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
        /// <param name="assetId">The asset ID as a string.</param>
        /// <param name="to">The to address as a string.</param>
        /// <param name="amount">The amount as a string.</param>
        /// <returns>The transaction details if successful, or the contract parameters if signatures are incomplete.</returns>
        /// <exception cref="RpcException">Thrown when no wallet is open, parameters are invalid, or there are insufficient funds.</exception>
        [RpcMethod]
        protected internal virtual JToken SendToAddress(UInt160 assetId, Address to, string amount)
        {
            var wallet = CheckWallet();

            using var snapshot = system.GetSnapshotCache();
            var descriptor = new AssetDescriptor(snapshot, system.Settings, assetId);
            var amountDecimal = new BigDecimal(BigInteger.Parse(amount), descriptor.Decimals);
            (amountDecimal.Sign > 0).True_Or(RpcErrorFactory.InvalidParams("Amount can't be negative."));

            var tx = wallet.MakeTransaction(snapshot, [new() { AssetId = assetId, Value = amountDecimal, ScriptHash = to.ScriptHash }])
                .NotNull_Or(RpcError.InsufficientFunds);

            var transContext = new ContractParametersContext(snapshot, tx, settings.Network);
            wallet.Sign(transContext);
            if (!transContext.Completed)
                return transContext.ToJson();

            tx.Witnesses = transContext.GetWitnesses();
            EnsureNetworkFee(snapshot, tx);

            (tx.NetworkFee <= settings.MaxFee).True_Or(RpcError.WalletFeeLimit);
            return SignAndRelay(snapshot, tx);
        }

        private void EnsureNetworkFee(StoreCache snapshot, Transaction tx)
        {
            if (tx.Size > 1024)
            {
                var calFee = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);

                if (system.Settings.IsHardforkEnabledInNextBlock(Hardfork.HF_Faun, snapshot))
                {
                    calFee = calFee.DivideCeiling(ApplicationEngine.FeeFactor);
                }

                calFee += 100000;

                if (tx.NetworkFee < calFee)
                    tx.NetworkFee = (long)calFee;
            }
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
        /// <param name="txid">The transaction ID to cancel as a string.</param>
        /// <param name="signers">The signers as an array of strings.</param>
        /// <param name="extraFee">The extra fee as a string.</param>
        /// <returns>The details of the cancellation transaction.</returns>
        /// <exception cref="RpcException">
        /// Thrown when no wallet is open, the transaction is already confirmed, or there are insufficient funds for the cancellation fee.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken CancelTransaction(UInt256 txid, Address[] signers, string? extraFee = null)
        {
            var wallet = CheckWallet();
            NativeContract.Ledger.GetTransactionState(system.StoreView, txid)
                .Null_Or(RpcErrorFactory.AlreadyExists("This tx is already confirmed, can't be cancelled."));

            if (signers is null || signers.Length == 0) throw new RpcException(RpcErrorFactory.BadRequest("No signer."));

            var conflict = new TransactionAttribute[] { new Conflicts() { Hash = txid } };
            var noneSigners = signers.ToSigners(WitnessScope.None)!;
            var tx = new Transaction
            {
                Signers = noneSigners,
                Attributes = conflict,
                Witnesses = [],
            };

            tx = Result.Ok_Or(
                () => wallet.MakeTransaction(system.StoreView, new[] { (byte)OpCode.RET }, noneSigners[0].Account, noneSigners, conflict),
                RpcError.InsufficientFunds, true);
            if (system.MemPool.TryGetValue(txid, out var conflictTx))
            {
                tx.NetworkFee = Math.Max(tx.NetworkFee, conflictTx.NetworkFee) + 1;
            }
            else if (extraFee is not null)
            {
                var descriptor = new AssetDescriptor(system.StoreView, system.Settings, NativeContract.GAS.Hash);
                (BigDecimal.TryParse(extraFee, descriptor.Decimals, out var decimalExtraFee) && decimalExtraFee.Sign > 0)
                    .True_Or(RpcErrorFactory.InvalidParams("Incorrect amount format."));

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
        /// <param name="scriptHash">The script hash as a string.</param>
        /// <param name="args">The arguments as an array of strings.</param>
        /// <param name="signersAndWitnesses">The JSON array of signers and witnesses<see cref="ParameterConverter.ToSignersAndWitnesses"/>. Optional.</param>
        /// <returns>A JSON object containing the result of the verification.</returns>
        /// <exception cref="RpcException">
        /// Thrown when the script hash is invalid, the contract is not found, or the verification fails.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken InvokeContractVerify(UInt160 scriptHash,
            ContractParameter[]? args = null, SignersAndWitnesses signersAndWitnesses = default)
        {
            args ??= [];
            var (signers, witnesses) = signersAndWitnesses;
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
        private JObject GetVerificationResult(UInt160 scriptHash, ContractParameter[] args, Signer[]? signers = null, Witness[]? witnesses = null)
        {
            using var snapshot = system.GetSnapshotCache();
            var contract = NativeContract.ContractManagement.GetContract(snapshot, scriptHash)
                .NotNull_Or(RpcError.UnknownContract);

            var md = contract.Manifest.Abi.GetMethod(ContractBasicMethod.Verify, args.Length)
                .NotNull_Or(RpcErrorFactory.InvalidContractVerification(contract.Hash, args.Length));

            (md.ReturnType == ContractParameterType.Boolean)
                .True_Or(RpcErrorFactory.InvalidContractVerification("The verify method doesn't return boolean value."));

            var tx = new Transaction
            {
                Signers = signers ?? [new() { Account = scriptHash }],
                Attributes = [],
                Witnesses = witnesses,
                Script = new[] { (byte)OpCode.RET }
            };

            using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot.CloneCache(), settings: system.Settings);
            engine.LoadContract(contract, md, CallFlags.ReadOnly);

            var invocationScript = Array.Empty<byte>();
            if (args.Length > 0)
            {
                using ScriptBuilder sb = new();
                for (int i = args.Length - 1; i >= 0; i--)
                    sb.EmitPush(args[i]);

                invocationScript = sb.ToArray();
                tx.Witnesses ??= [new() { InvocationScript = invocationScript }];
                engine.LoadScript(new Script(invocationScript), configureState: p => p.CallFlags = CallFlags.None);
            }

            var json = new JObject()
            {
                ["script"] = Convert.ToBase64String(invocationScript),
                ["state"] = engine.Execute(),
                // Gas consumed in the unit of datoshi, 1 GAS = 1e8 datoshi
                ["gasconsumed"] = engine.FeeConsumed.ToString(),
                ["exception"] = GetExceptionMessage(engine.FaultException)
            };

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
            var wallet = CheckWallet();
            var context = new ContractParametersContext(snapshot, tx, settings.Network);
            wallet.Sign(context);
            if (context.Completed)
            {
                tx.Witnesses = context.GetWitnesses();
                system.Blockchain.Tell(tx);
                return tx.ToJson(system.Settings);
            }
            else
            {
                return context.ToJson();
            }
        }
    }
}
