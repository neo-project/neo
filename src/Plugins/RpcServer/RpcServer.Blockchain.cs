// Copyright (C) 2015-2025 The Neo Project.
//
// RpcServer.Blockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.RpcServer.Model;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.Plugins.RpcServer
{
    partial class RpcServer
    {
        /// <summary>
        /// Gets the hash of the best (most recent) block.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getbestblockhash"}</code>
        /// <para>Response format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1, "result": "The block hash(UInt256)"}
        /// </code>
        /// </summary>
        /// <returns>The hash of the best block as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetBestBlockHash()
        {
            return NativeContract.Ledger.CurrentHash(system.StoreView).ToString();
        }

        /// <summary>
        /// Gets a block by its hash or index.
        /// <para>Request format:</para>
        /// <code>
        /// // Request with block hash(for example: 0x6c0b6c03fbc7d7d797ddd6483fe59a64f77c47475c1da600b71b189f6f4f234a)
        /// {"jsonrpc": "2.0", "id": 1, "method": "getblock", "params": ["The block hash(UInt256)"]}
        /// </code>
        /// <code>
        /// // Request with block index
        /// {"jsonrpc": "2.0", "id": 1, "method": "getblock", "params": [100]}
        /// </code>
        /// <code>
        /// // Request with block hash and verbose is true
        /// {"jsonrpc": "2.0", "id": 1, "method": "getblock", "params": ["The block hash(UInt256)", true]}
        /// </code>
        /// <para>Response format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1, "result": "A base64-encoded string of the block"}
        /// </code>
        /// <para>If verbose is true, the response format is:</para>
        /// <code>{
        ///   "jsonrpc":"2.0",
        ///   "id":1,
        ///   "result":{
        ///     "hash":"The block hash(UInt256)",
        ///     "size":697, // The size of the block
        ///     "version":0, // The version of the block
        ///     "previousblockhash":"The previous block hash(UInt256)",
        ///     "merkleroot":"The merkle root(UInt256)",
        ///     "time":1627896461306, // The timestamp of the block
        ///     "nonce":"09D4422954577BCE", // The nonce of the block
        ///     "index":100, // The index of the block
        ///     "primary":2, // The primary of the block
        ///     "nextconsensus":"The Base58Check-encoded next consensus address",
        ///     "witnesses":[{"invocation":"A base64-encoded string","verification":"A base64-encoded string"}],
        ///     "tx":[], // The transactions of the block
        ///     "confirmations": 200, // The confirmations of the block, if verbose is true
        ///     "nextblockhash":"The next block hash(UInt256)" // The next block hash, if verbose is true
        ///   }
        /// }</code>
        /// </summary>
        /// <param name="blockHashOrIndex">The block hash or index.</param>
        /// <param name="verbose">Optional, the default value is false.</param>
        /// <returns>The block data as a <see cref="JToken"/>. If the second item of _params is true, then
        /// block data is json format, otherwise, the return type is Base64-encoded byte array.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetBlock(BlockHashOrIndex blockHashOrIndex, bool verbose = false)
        {
            RpcException.ThrowIfNull(blockHashOrIndex, nameof(blockHashOrIndex), RpcError.InvalidParams);

            using var snapshot = system.GetSnapshotCache();
            var block = blockHashOrIndex.IsIndex
                ? NativeContract.Ledger.GetBlock(snapshot, blockHashOrIndex.AsIndex())
                : NativeContract.Ledger.GetBlock(snapshot, blockHashOrIndex.AsHash());
            block.NotNull_Or(RpcError.UnknownBlock);
            if (verbose)
            {
                JObject json = Utility.BlockToJson(block, system.Settings);
                json["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
                UInt256 hash = NativeContract.Ledger.GetBlockHash(snapshot, block.Index + 1);
                if (hash != null)
                    json["nextblockhash"] = hash.ToString();
                return json;
            }
            return Convert.ToBase64String(block.ToArray());
        }

        /// <summary>
        /// Gets the number of block headers in the blockchain.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getblockheadercount"}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": 100 /* The number of block headers in the blockchain */}</code>
        /// </summary>
        /// <returns>The count of block headers as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        internal virtual JToken GetBlockHeaderCount()
        {
            return (system.HeaderCache.Last?.Index ?? NativeContract.Ledger.CurrentIndex(system.StoreView)) + 1;
        }

        /// <summary>
        /// Gets the number of blocks in the blockchain.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getblockcount"}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": 100 /* The number of blocks in the blockchain */}</code>
        /// </summary>
        /// <returns>The count of blocks as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetBlockCount()
        {
            return NativeContract.Ledger.CurrentIndex(system.StoreView) + 1;
        }

        /// <summary>
        /// Gets the hash of the block at the specified height.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getblockhash", "params": [100] /* The block index */}</code>
        /// <para>Response format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1, "result": "The block hash(UInt256)"}
        /// </code>
        /// </summary>
        /// <param name="height">Block index (block height)</param>
        /// <returns>The hash of the block at the specified height as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetBlockHash(uint height)
        {
            var snapshot = system.StoreView;
            if (height <= NativeContract.Ledger.CurrentIndex(snapshot))
            {
                return NativeContract.Ledger.GetBlockHash(snapshot, height).ToString();
            }
            throw new RpcException(RpcError.UnknownHeight);
        }

        /// <summary>
        /// Gets a block header by its hash or index.
        /// <param name="blockHashOrIndex">The block script hash or index (i.e. block height=number of blocks - 1).</param>
        /// <param name="verbose">Optional, the default value is false.</param>
        /// <remarks>
        /// When verbose is false, serialized information of the block is returned in a hexadecimal string.
        /// If you need the detailed information, use the SDK for deserialization.
        /// When verbose is true or 1, detailed information of the block is returned in Json format.
        /// </remarks>
        /// <para>Request format:</para>
        /// <code>
        /// // Request with block hash(for example: 0x6c0b6c03fbc7d7d797ddd6483fe59a64f77c47475c1da600b71b189f6f4f234a)
        /// {"jsonrpc": "2.0", "id": 1, "method": "getblockheader", "params": ["The block hash(UInt256)"]}
        /// </code>
        /// <code>
        /// // Request with block index
        /// {"jsonrpc": "2.0", "id": 1, "method": "getblockheader", "params": [100]}
        /// </code>
        /// <code>
        /// // Request with block index and verbose is true
        /// {"jsonrpc": "2.0", "id": 1, "method": "getblockheader", "params": [100, true]}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": "A base64-encoded string of the block header"}</code>
        /// <para>If verbose is true, the response format is:</para>
        /// <code>{
        ///   "jsonrpc":"2.0",
        ///   "id":1,
        ///   "result": {
        ///     "hash": "The block hash(UInt256)",
        ///     "size": 696, // The size of the block header
        ///     "version": 0, // The version of the block header
        ///     "previousblockhash": "The previous block hash(UInt256)",
        ///     "merkleroot": "The merkle root(UInt256)",
        ///     "time": 1627896461306, // The timestamp of the block header
        ///     "nonce": "09D4422954577BCE", // The nonce of the block header
        ///     "index": 100, // The index of the block header
        ///     "primary": 2, // The primary of the block header
        ///     "nextconsensus": "The Base58Check-encoded next consensus address",
        ///     "witnesses": [{"invocation":"A base64-encoded string", "verification":"A base64-encoded string"}],
        ///     "confirmations": 200, // The confirmations of the block header, if verbose is true
        ///     "nextblockhash": "The next block hash(UInt256)" // The next block hash, if verbose is true
        ///   }
        /// }</code>
        /// </summary>
        /// <returns>
        /// The block header data as a <see cref="JToken"/>.
        /// In json format if the second item of _params is true, otherwise Base64-encoded byte array.
        /// </returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetBlockHeader(BlockHashOrIndex blockHashOrIndex, bool verbose = false)
        {
            RpcException.ThrowIfNull(blockHashOrIndex, nameof(blockHashOrIndex), RpcError.InvalidParams);

            var snapshot = system.StoreView;
            Header header;
            if (blockHashOrIndex.IsIndex)
            {
                header = NativeContract.Ledger.GetHeader(snapshot, blockHashOrIndex.AsIndex()).NotNull_Or(RpcError.UnknownBlock);
            }
            else
            {
                header = NativeContract.Ledger.GetHeader(snapshot, blockHashOrIndex.AsHash()).NotNull_Or(RpcError.UnknownBlock);
            }

            if (verbose)
            {
                var json = header.ToJson(system.Settings);
                json["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - header.Index + 1;

                var hash = NativeContract.Ledger.GetBlockHash(snapshot, header.Index + 1);
                if (hash != null) json["nextblockhash"] = hash.ToString();
                return json;
            }

            return Convert.ToBase64String(header.ToArray());
        }

        /// <summary>
        /// Gets the state of a contract by its ID or script hash or (only for native contracts) by case-insensitive name.
        /// <para>Request format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1, "method": "getcontractstate", "params": ["The contract id(int) or hash(UInt160)"]}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": "A json string of the contract state"}</code>
        /// </summary>
        /// <param name="contractNameOrHashOrId">Contract name or script hash or the native contract id.</param>
        /// <returns>The contract state in json format as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetContractState(ContractNameOrHashOrId contractNameOrHashOrId)
        {
            RpcException.ThrowIfNull(contractNameOrHashOrId, nameof(contractNameOrHashOrId), RpcError.InvalidParams);

            if (contractNameOrHashOrId.IsId)
            {
                var contractState = NativeContract.ContractManagement.GetContractById(system.StoreView, contractNameOrHashOrId.AsId());
                return contractState.NotNull_Or(RpcError.UnknownContract).ToJson();
            }

            var hash = contractNameOrHashOrId.IsName ? ToScriptHash(contractNameOrHashOrId.AsName()) : contractNameOrHashOrId.AsHash();
            var contract = NativeContract.ContractManagement.GetContract(system.StoreView, hash);
            return contract.NotNull_Or(RpcError.UnknownContract).ToJson();
        }

        private static UInt160 ToScriptHash(string keyword)
        {
            foreach (var native in NativeContract.Contracts)
            {
                if (keyword.Equals(native.Name, StringComparison.InvariantCultureIgnoreCase) || keyword == native.Id.ToString())
                    return native.Hash;
            }

            return UInt160.Parse(keyword);
        }

        /// <summary>
        /// Gets the current memory pool transactions.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getrawmempool", "params": [true/*shouldGetUnverified, optional*/]}</code>
        /// <para>Response format:</para>
        /// If shouldGetUnverified is true, the response format is:
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "height": 100,
        ///     "verified": ["The tx hash"], // The verified transactions
        ///     "unverified": ["The tx hash"] // The unverified transactions
        ///   }
        /// }</code>
        /// If shouldGetUnverified is false, the response format is:
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": ["The tx hash"] // The verified transactions
        /// }</code>
        /// </summary>
        /// <param name="shouldGetUnverified">Optional, the default value is false.</param>
        /// <returns>The memory pool transactions in json format as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetRawMemPool(bool shouldGetUnverified = false)
        {
            if (!shouldGetUnverified)
                return new JArray(system.MemPool.GetVerifiedTransactions().Select(p => (JToken)p.Hash.ToString()));

            JObject json = new();
            json["height"] = NativeContract.Ledger.CurrentIndex(system.StoreView);
            system.MemPool.GetVerifiedAndUnverifiedTransactions(
                out IEnumerable<Transaction> verifiedTransactions,
                out IEnumerable<Transaction> unverifiedTransactions);
            json["verified"] = new JArray(verifiedTransactions.Select(p => (JToken)p.Hash.ToString()));
            json["unverified"] = new JArray(unverifiedTransactions.Select(p => (JToken)p.Hash.ToString()));
            return json;
        }

        /// <summary>
        /// Gets a transaction by its hash.
        /// <para>Request format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1, "method": "getrawtransaction", "params": ["The tx hash", true/*verbose, optional*/]}
        /// </code>
        /// <para>Response format:</para>
        /// If verbose is false, the response format is:
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": "The Base64-encoded tx data"
        /// }</code>
        /// If verbose is true, the response format is:
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "hash": "The tx hash(UInt256)",
        ///     "size": 272, // The size of the tx
        ///     "version": 0, // The version of the tx
        ///     "nonce": 1553700339, // The nonce of the tx
        ///     "sender": "The Base58Check-encoded sender address", // The sender address of the tx
        ///     "sysfee": "100000000", // The system fee of the tx
        ///     "netfee": "1272390", // The network fee of the tx
        ///     "validuntilblock": 2105487, // The valid until block of the tx
        ///     "attributes": [], // The attributes of the tx
        ///     "signers": [], // The signers of the tx
        ///     "script": "A Base64-encoded string", // The script of the tx
        ///     "witnesses": [{"invocation": "A base64-encoded string", "verification": "A base64-encoded string"}] // The witnesses of the tx
        ///     "confirmations": 100, // The confirmations of the tx
        ///     "blockhash": "The block hash", // The block hash
        ///     "blocktime": 1627896461306 // The block time
        ///   }
        /// }</code>
        /// </summary>
        /// <param name="hash">The transaction hash.</param>
        /// <param name="verbose">Optional, the default value is false.</param>
        /// <returns>The transaction data as a <see cref="JToken"/>. In json format if verbose is true, otherwise base64string. </returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetRawTransaction(UInt256 hash, bool verbose = false)
        {
            RpcException.ThrowIfNull(hash, nameof(hash), RpcError.InvalidParams);

            if (system.MemPool.TryGetValue(hash, out var tx) && !verbose)
                return Convert.ToBase64String(tx.ToArray());
            var snapshot = system.StoreView;
            var state = NativeContract.Ledger.GetTransactionState(snapshot, hash);
            tx ??= state?.Transaction;
            tx.NotNull_Or(RpcError.UnknownTransaction);
            if (!verbose) return Convert.ToBase64String(tx.ToArray());
            var json = Utility.TransactionToJson(tx, system.Settings);
            if (state is not null)
            {
                var block = NativeContract.Ledger.GetTrimmedBlock(snapshot, NativeContract.Ledger.GetBlockHash(snapshot, state.BlockIndex));
                json["blockhash"] = block.Hash.ToString();
                json["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
                json["blocktime"] = block.Header.Timestamp;
            }
            return json;
        }

        /// <summary>
        /// Gets the storage item by contract ID or script hash and key.
        /// <para>Request format:</para>
        /// <code>
        /// {
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "getstorage",
        ///   "params": ["The contract id(int) or hash(UInt160)", "The Base64-encoded key"]
        /// }
        /// </code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": "The Base64-encoded storage value"}</code>
        /// </summary>
        /// <param name="contractNameOrHashOrId">The contract ID or script hash.</param>
        /// <param name="base64Key">The Base64-encoded storage key.</param>
        /// <returns>The storage item as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetStorage(ContractNameOrHashOrId contractNameOrHashOrId, string base64Key)
        {
            RpcException.ThrowIfNull(contractNameOrHashOrId, nameof(contractNameOrHashOrId), RpcError.InvalidParams);
            RpcException.ThrowIfNull(base64Key, nameof(base64Key), RpcError.InvalidParams);

            using var snapshot = system.GetSnapshotCache();
            int id;
            if (contractNameOrHashOrId.IsHash)
            {
                var hash = contractNameOrHashOrId.AsHash();
                var contract = NativeContract.ContractManagement.GetContract(snapshot, hash).NotNull_Or(RpcError.UnknownContract);
                id = contract.Id;
            }
            else
            {
                id = contractNameOrHashOrId.AsId();
            }

            var key = Convert.FromBase64String(base64Key);
            var item = snapshot.TryGet(new StorageKey
            {
                Id = id,
                Key = key
            }).NotNull_Or(RpcError.UnknownStorageItem);
            return Convert.ToBase64String(item.Value.Span);
        }

        /// <summary>
        /// Finds storage items by contract ID or script hash and prefix.
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "findstorage",
        ///   "params": ["The contract id(int) or hash(UInt160)", "The base64-encoded key prefix", 0/*The start index, optional*/]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "truncated": true,
        ///     "next": 100,
        ///     "results": [
        ///       {"key": "The Base64-encoded storage key", "value": "The Base64-encoded storage value"},
        ///       {"key": "The Base64-encoded storage key", "value": "The Base64-encoded storage value"},
        ///       // ...
        ///     ]
        ///   }
        /// }</code>
        /// </summary>
        /// <param name="contractNameOrHashOrId">The contract ID (int) or script hash (UInt160).</param>
        /// <param name="base64KeyPrefix">The Base64-encoded storage key prefix.</param>
        /// <param name="start">The start index.</param>
        /// <returns>The found storage items <see cref="StorageItem"/> as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken FindStorage(ContractNameOrHashOrId contractNameOrHashOrId, string base64KeyPrefix, int start = 0)
        {
            RpcException.ThrowIfNull(contractNameOrHashOrId, nameof(contractNameOrHashOrId), RpcError.InvalidParams);
            RpcException.ThrowIfNull(base64KeyPrefix, nameof(base64KeyPrefix), RpcError.InvalidParams);

            using var snapshot = system.GetSnapshotCache();
            int id;
            if (contractNameOrHashOrId.IsHash)
            {
                var contract = NativeContract.ContractManagement.GetContract(snapshot, contractNameOrHashOrId.AsHash()).NotNull_Or(RpcError.UnknownContract);
                id = contract.Id;
            }
            else
            {
                id = contractNameOrHashOrId.AsId();
            }

            var prefix = Result.Ok_Or(
                () => Convert.FromBase64String(base64KeyPrefix),
                RpcError.InvalidParams.WithData($"Invalid Base64 string: {base64KeyPrefix}"));

            var json = new JObject();
            var items = new JArray();
            int pageSize = settings.FindStoragePageSize;
            int i = 0;
            using (var iter = NativeContract.ContractManagement.FindContractStorage(snapshot, id, prefix).Skip(count: start).GetEnumerator())
            {
                var hasMore = false;
                while (iter.MoveNext())
                {
                    if (i == pageSize)
                    {
                        hasMore = true;
                        break;
                    }

                    var item = new JObject
                    {
                        ["key"] = Convert.ToBase64String(iter.Current.Key.Key.Span),
                        ["value"] = Convert.ToBase64String(iter.Current.Value.Value.Span)
                    };
                    items.Add(item);
                    i++;
                }
                json["truncated"] = hasMore;
            }

            json["next"] = start + i;
            json["results"] = items;
            return json;
        }

        /// <summary>
        /// Gets the height of a transaction by its hash.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "gettransactionheight", "params": ["The tx hash(UInt256)"]}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": 100}</code>
        /// </summary>
        /// <param name="hash">The transaction hash.</param>
        /// <returns>The height of the transaction as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetTransactionHeight(UInt256 hash)
        {
            RpcException.ThrowIfNull(hash, nameof(hash), RpcError.InvalidParams);

            uint? height = NativeContract.Ledger.GetTransactionState(system.StoreView, hash)?.BlockIndex;
            if (height.HasValue) return height.Value;
            throw new RpcException(RpcError.UnknownTransaction);
        }

        /// <summary>
        /// Gets the next block validators.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getnextblockvalidators"}</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": [
        ///     {"publickey": "The public key", "votes": 100 /* The votes of the validator */}
        ///     // ...
        ///   ]
        /// }</code>
        /// </summary>
        /// <returns>The next block validators as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetNextBlockValidators()
        {
            using var snapshot = system.GetSnapshotCache();
            var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, system.Settings.ValidatorsCount);
            return validators.Select(p =>
            {
                JObject validator = new();
                validator["publickey"] = p.ToString();
                validator["votes"] = (int)NativeContract.NEO.GetCandidateVote(snapshot, p);
                return validator;
            }).ToArray();
        }

        /// <summary>
        /// Gets the list of candidates for the next block validators.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getcandidates"}</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": [
        ///     {"publickey": "The public key", "votes": "An integer number in string", "active": true /* Is active or not */}
        ///     // ...
        ///   ]
        /// }</code>
        /// </summary>
        /// <returns>The candidates public key list as a JToken.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetCandidates()
        {
            using var snapshot = system.GetSnapshotCache();
            byte[] script;
            using (ScriptBuilder sb = new())
            {
                script = sb.EmitDynamicCall(NativeContract.NEO.Hash, "getCandidates", null).ToArray();
            }
            StackItem[] resultstack;
            try
            {
                using var engine = ApplicationEngine.Run(script, snapshot, settings: system.Settings, gas: settings.MaxGasInvoke);
                resultstack = engine.ResultStack.ToArray();
            }
            catch
            {
                throw new RpcException(RpcError.InternalServerError.WithData("Can't get candidates."));
            }

            JObject json = new();
            try
            {
                if (resultstack.Length > 0)
                {
                    JArray jArray = new();
                    var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, system.Settings.ValidatorsCount)
                        ?? throw new RpcException(RpcError.InternalServerError.WithData("Can't get next block validators."));

                    foreach (var item in resultstack)
                    {
                        var value = (Array)item;
                        foreach (Struct ele in value)
                        {
                            var publickey = ele[0].GetSpan().ToHexString();
                            json["publickey"] = publickey;
                            json["votes"] = ele[1].GetInteger().ToString();
                            json["active"] = validators.ToByteArray().ToHexString().Contains(publickey);
                            jArray.Add(json);
                            json = new();
                        }
                        return jArray;
                    }
                }
            }
            catch
            {
                throw new RpcException(RpcError.InternalServerError.WithData("Can't get next block validators"));
            }

            return json;
        }

        /// <summary>
        /// Gets the list of committee members.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getcommittee"}</code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": ["The public key"]}</code>
        /// </summary>
        /// <returns>The committee members publickeys as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetCommittee()
        {
            return new JArray(NativeContract.NEO.GetCommittee(system.StoreView).Select(p => (JToken)p.ToString()));
        }

        /// <summary>
        /// Gets the list of native contracts.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "getnativecontracts"}</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": [{
        ///      "id": -1, // The contract id
        ///      "updatecounter": 0, // The update counter
        ///      "hash": "The contract hash(UInt160)", // The contract hash
        ///      "nef":  {
        ///        "magic": 0x3346454E, // The magic number, always 0x3346454E at present.
        ///        "compiler": "The compiler name",
        ///        "source": "The url of the source file",
        ///        "tokens": [
        ///          {
        ///            "hash": "The token hash(UInt160)",
        ///            "method": "The token method name",
        ///            "paramcount": 0, // The number of parameters
        ///            "hasreturnvalue": false, // Whether the method has a return value
        ///            "callflags": 0 // see CallFlags
        ///          } // A token in the contract
        ///          // ...
        ///        ],
        ///        "script": "The Base64-encoded script", // The Base64-encoded script
        ///        "checksum": 0x12345678 // The checksum
        ///      },
        ///      "manifest": {
        ///        "name": "The contract name",
        ///        "groups": [
        ///          {"pubkey": "The public key", "signature": "The signature"} // A group in the manifest
        ///        ],
        ///        "features": {}, // The features that the contract supports
        ///        "supportedstandards": ["The standard name"], // The standards that the contract supports
        ///        "abi": {
        ///          "methods": [
        ///            {
        ///              "name": "The method name",
        ///              "parameters": [
        ///                {"name": "The parameter name", "type": "The parameter type"} // A parameter in the method
        ///                // ...
        ///              ],
        ///              "returntype": "The return type",
        ///              "offset": 0, // The offset in script of the method
        ///              "safe": false // Whether the method is safe
        ///            } // A method in the abi
        ///            // ...
        ///          ],
        ///          "events": [
        ///            {
        ///              "name": "The event name",
        ///              "parameters": [
        ///                {"name": "The parameter name", "type": "The parameter type"} // A parameter in the event
        ///                // ...
        ///              ]
        ///            } // An event in the abi
        ///            // ...
        ///          ]
        ///        }, // The abi of the contract
        ///        "permissions": [
        ///          {
        ///            "contract": "The contract hash(UInt160), group(ECPoint), or '*'", // '*' means all contracts
        ///            "methods": ["The method name or '*'"] // '*' means all methods
        ///          } // A permission in the contract
        ///          // ...
        ///        ], // The permissions of the contract
        ///        "trusts": [
        ///          {
        ///            "contract": "The contract hash(UInt160), group(ECPoint), or '*'", // '*' means all contracts
        ///            "methods": ["The method name or '*'"] // '*' means all methods
        ///          } // A trust in the contract
        ///          // ...
        ///        ], // The trusts of the contract
        ///        "extra": {} // A json object, the extra content of the contract
        ///      } // The manifest of the contract
        ///    }]
        /// }</code>
        /// </summary>
        /// <returns>The native contract states <see cref="ContractState"/> as a <see cref="JToken"/>.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetNativeContracts()
        {
            var storeView = system.StoreView;
            var contractStates = NativeContract.Contracts
                .Select(p => NativeContract.ContractManagement.GetContract(storeView, p.Hash))
                .Where(p => p != null) // if not active
                .Select(p => p.ToJson());
            return new JArray(contractStates);
        }
    }
}
