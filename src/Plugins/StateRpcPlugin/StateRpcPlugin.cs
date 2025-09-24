// Copyright (C) 2015-2025 The Neo Project.
//
// StateRpcPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.Cryptography.MPTTrie;
using Neo.Extensions;
using Neo.Json;
using Neo.Plugins.RpcServer;
using Neo.Plugins.StateRootPlugin.Storage;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Plugins.StateRpcPlugin
{
    public class StateRpcPlugin : Plugin
    {
        public override string Name => "StateRpcPlugin";
        public override string Description => "Provides RPC methods for state queries";
        public override string ConfigFile => System.IO.Path.Combine(RootPath, "StateRpcPlugin.json");

        protected override UnhandledExceptionPolicy ExceptionPolicy => StateRpcSettings.Default.ExceptionPolicy;

        protected override void Configure()
        {
            StateRpcSettings.Load(GetConfiguration());
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (system.Settings.Network != StateRpcSettings.Default.Network) return;
            RpcServerPlugin.RegisterMethods(this, StateRpcSettings.Default.Network);
        }


        [ConsoleCommand("get proof", Category = "StateRpc", Description = "Get proof of key and contract hash")]
        private void OnGetProof(UInt256 rootHash, UInt160 scriptHash, string key)
        {
            

            try
            {
                ConsoleHelper.Info("Proof: ", GetProof(rootHash, scriptHash, Convert.FromBase64String(key)));
            }
            catch (RpcException e)
            {
                ConsoleHelper.Error(e.Message);
            }
        }

        [ConsoleCommand("verify proof", Category = "StateRpc", Description = "Verify proof, return value if successed")]
        private void OnVerifyProof(UInt256 rootHash, string proof)
        {
            try
            {
                ConsoleHelper.Info("Verify Result: ", VerifyProof(rootHash, Convert.FromBase64String(proof)));
            }
            catch (RpcException e)
            {
                ConsoleHelper.Error(e.Message);
            }
        }

        [RpcMethod]
        public JToken GetStateRoot(uint index)
        {
            using var snapshot = StateStore.Singleton.GetSnapshot();
            var stateRoot = snapshot.GetStateRoot(index).NotNull_Or(RpcError.UnknownStateRoot);
            return stateRoot.ToJson();
        }

        private string GetProof(Trie trie, int contractId, byte[] key)
        {
            var skey = new StorageKey()
            {
                Id = contractId,
                Key = key,
            };
            return GetProof(trie, skey);
        }

        private string GetProof(Trie trie, StorageKey skey)
        {
            trie.TryGetProof(skey.ToArray(), out var proof).True_Or(RpcError.UnknownStorageItem);
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Utility.StrictUTF8);

            writer.WriteVarBytes(skey.ToArray());
            writer.WriteVarInt(proof.Count);
            foreach (var item in proof)
            {
                writer.WriteVarBytes(item);
            }
            writer.Flush();

            return Convert.ToBase64String(ms.ToArray());
        }

        private string GetProof(UInt256 rootHash, UInt160 scriptHash, byte[] key)
        {
            CheckRootHash(rootHash);

            using var store = StateStore.Singleton.GetStoreSnapshot();
            var trie = new Trie(store, rootHash);
            var contract = GetHistoricalContractState(trie, scriptHash).NotNull_Or(RpcError.UnknownContract);
            return GetProof(trie, contract.Id, key);
        }

        [RpcMethod]
        public JToken GetProof(UInt256 rootHash, UInt160 scriptHash, string key)
        {
            var keyBytes = Result.Ok_Or(() => Convert.FromBase64String(key), RpcError.InvalidParams.WithData($"Invalid key: {key}"));
            return GetProof(rootHash, scriptHash, keyBytes);
        }

        private string VerifyProof(UInt256 rootHash, byte[] proof)
        {
            var proofs = new HashSet<byte[]>();

            using var ms = new MemoryStream(proof, false);
            using var reader = new BinaryReader(ms, Utility.StrictUTF8);

            var key = reader.ReadVarBytes(Node.MaxKeyLength);
            var count = reader.ReadVarInt(byte.MaxValue);
            for (ulong i = 0; i < count; i++)
            {
                proofs.Add(reader.ReadVarBytes());
            }

            var value = Trie.VerifyProof(rootHash, key, proofs).NotNull_Or(RpcError.InvalidProof);
            return Convert.ToBase64String(value);
        }

        [RpcMethod]
        public JToken VerifyProof(UInt256 rootHash, string proof)
        {
            var proofBytes = Result.Ok_Or(
                () => Convert.FromBase64String(proof), RpcError.InvalidParams.WithData($"Invalid proof: {proof}"));
            return VerifyProof(rootHash, proofBytes);
        }

        [RpcMethod]
        public JToken GetStateHeight()
        {
            return new JObject()
            {
                ["localrootindex"] = StateStore.Singleton.LocalRootIndex,
                ["validatedrootindex"] = StateStore.Singleton.ValidatedRootIndex,
            };
        }

        private ContractState GetHistoricalContractState(Trie trie, UInt160 scriptHash)
        {
            const byte prefix = 8;
            var skey = new KeyBuilder(NativeContract.ContractManagement.Id, prefix).Add(scriptHash);
            return trie.TryGetValue(skey.ToArray(), out var value)
                ? value.AsSerializable<StorageItem>().GetInteroperable<ContractState>()
                : null;
        }

        private StorageKey ParseStorageKey(byte[] data)
        {
            return new() { Id = BinaryPrimitives.ReadInt32LittleEndian(data), Key = data.AsMemory(sizeof(int)) };
        }

        private void CheckRootHash(UInt256 rootHash)
        {
            var fullState = StateRpcSettings.Default.FullState;
            var current = StateStore.Singleton.CurrentLocalRootHash;
            (!fullState && current != rootHash)
                .False_Or(RpcError.UnsupportedState.WithData($"fullState:{fullState},current:{current},rootHash:{rootHash}"));
        }

        [RpcMethod]
        public JToken FindStates(UInt256 rootHash, UInt160 scriptHash, byte[] prefix, byte[] key = null, int count = 0)
        {
            CheckRootHash(rootHash);

            key ??= [];
            count = count <= 0 ? StateRpcSettings.Default.MaxFindResultItems : count;
            count = Math.Min(count, StateRpcSettings.Default.MaxFindResultItems);

            using var store = StateStore.Singleton.GetStoreSnapshot();
            var trie = new Trie(store, rootHash);
            var contract = GetHistoricalContractState(trie, scriptHash).NotNull_Or(RpcError.UnknownContract);
            var pkey = new StorageKey() { Id = contract.Id, Key = prefix };
            var fkey = new StorageKey() { Id = pkey.Id, Key = key };

            var json = new JObject();
            var jarr = new JArray();
            int i = 0;
            foreach (var (ikey, ivalue) in trie.Find(pkey.ToArray(), 0 < key.Length ? fkey.ToArray() : null))
            {
                if (count < i) break;
                if (i < count)
                {
                    jarr.Add(new JObject()
                    {
                        ["key"] = Convert.ToBase64String(ParseStorageKey(ikey.ToArray()).Key.Span),
                        ["value"] = Convert.ToBase64String(ivalue.Span),
                    });
                }
                i++;
            }
            if (0 < jarr.Count)
            {
                json["firstProof"] = GetProof(trie, contract.Id, Convert.FromBase64String(jarr.First()["key"].AsString()));
            }
            if (1 < jarr.Count)
            {
                json["lastProof"] = GetProof(trie, contract.Id, Convert.FromBase64String(jarr.Last()["key"].AsString()));
            }
            json["truncated"] = count < i;
            json["results"] = jarr;
            return json;
        }

        [RpcMethod]
        public JToken GetState(UInt256 rootHash, UInt160 scriptHash, byte[] key)
        {
            CheckRootHash(rootHash);

            using var store = StateStore.Singleton.GetStoreSnapshot();
            var trie = new Trie(store, rootHash);
            var contract = GetHistoricalContractState(trie, scriptHash).NotNull_Or(RpcError.UnknownContract);
            var skey = new StorageKey()
            {
                Id = contract.Id,
                Key = key,
            };
            return Convert.ToBase64String(trie[skey.ToArray()]);
        }
    }
}