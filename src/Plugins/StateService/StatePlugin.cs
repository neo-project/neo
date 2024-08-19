// Copyright (C) 2015-2024 The Neo Project.
//
// StatePlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.ConsoleService;
using Neo.Cryptography.MPTTrie;
using Neo.IEventHandlers;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.RpcServer;
using Neo.Plugins.StateService.Network;
using Neo.Plugins.StateService.Storage;
using Neo.Plugins.StateService.Verification;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins.StateService
{
    public class StatePlugin : Plugin, ICommittingHandler, ICommittedHandler, IWalletChangedHandler, IServiceAddedHandler
    {
        public const string StatePayloadCategory = "StateService";
        public override string Name => "StateService";
        public override string Description => "Enables MPT for the node";
        public override string ConfigFile => System.IO.Path.Combine(RootPath, "StateService.json");

        protected override UnhandledExceptionPolicy ExceptionPolicy => Settings.Default.ExceptionPolicy;

        internal IActorRef Store;
        internal IActorRef Verifier;

        internal static NeoSystem _system;
        private IWalletProvider walletProvider;

        public StatePlugin()
        {
            Blockchain.Committing += ((ICommittingHandler)this).Blockchain_Committing_Handler;
            Blockchain.Committed += ((ICommittedHandler)this).Blockchain_Committed_Handler;
        }

        protected override void Configure()
        {
            Settings.Load(GetConfiguration());
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (system.Settings.Network != Settings.Default.Network) return;
            _system = system;
            Store = _system.ActorSystem.ActorOf(StateStore.Props(this, string.Format(Settings.Default.Path, system.Settings.Network.ToString("X8"))));
            _system.ServiceAdded += ((IServiceAddedHandler)this).NeoSystem_ServiceAdded_Handler;
            RpcServerPlugin.RegisterMethods(this, Settings.Default.Network);
        }

        void IServiceAddedHandler.NeoSystem_ServiceAdded_Handler(object sender, object service)
        {
            if (service is IWalletProvider)
            {
                walletProvider = service as IWalletProvider;
                _system.ServiceAdded -= ((IServiceAddedHandler)this).NeoSystem_ServiceAdded_Handler;
                if (Settings.Default.AutoVerify)
                {
                    walletProvider.WalletChanged += ((IWalletChangedHandler)this).IWalletProvider_WalletChanged_Handler;
                }
            }
        }

        void IWalletChangedHandler.IWalletProvider_WalletChanged_Handler(object sender, Wallet wallet)
        {
            walletProvider.WalletChanged -= ((IWalletChangedHandler)this).IWalletProvider_WalletChanged_Handler;
            Start(wallet);
        }

        public override void Dispose()
        {
            base.Dispose();
            Blockchain.Committing -= ((ICommittingHandler)this).Blockchain_Committing_Handler;
            Blockchain.Committed -= ((ICommittedHandler)this).Blockchain_Committed_Handler;
            if (Store is not null) _system.EnsureStopped(Store);
            if (Verifier is not null) _system.EnsureStopped(Verifier);
        }

        void ICommittingHandler.Blockchain_Committing_Handler(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<ApplicationExecuted> applicationExecutedList)
        {
            if (system.Settings.Network != Settings.Default.Network) return;
            StateStore.Singleton.UpdateLocalStateRootSnapshot(block.Index, snapshot.GetChangeSet().Where(p => p.State != TrackState.None).Where(p => p.Key.Id != NativeContract.Ledger.Id).ToList());
        }

        void ICommittedHandler.Blockchain_Committed_Handler(NeoSystem system, Block block)
        {
            if (system.Settings.Network != Settings.Default.Network) return;
            StateStore.Singleton.UpdateLocalStateRoot(block.Index);
        }

        [ConsoleCommand("start states", Category = "StateService", Description = "Start as a state verifier if wallet is open")]
        private void OnStartVerifyingState()
        {
            if (_system is null || _system.Settings.Network != Settings.Default.Network) throw new InvalidOperationException("Network doesn't match");
            Start(walletProvider.GetWallet());
        }

        public void Start(Wallet wallet)
        {
            if (Verifier is not null)
            {
                ConsoleHelper.Warning("Already started!");
                return;
            }
            if (wallet is null)
            {
                ConsoleHelper.Warning("Please open wallet first!");
                return;
            }
            Verifier = _system.ActorSystem.ActorOf(VerificationService.Props(wallet));
        }

        [ConsoleCommand("state root", Category = "StateService", Description = "Get state root by index")]
        private void OnGetStateRoot(uint index)
        {
            if (_system is null || _system.Settings.Network != Settings.Default.Network) throw new InvalidOperationException("Network doesn't match");
            using var snapshot = StateStore.Singleton.GetSnapshot();
            StateRoot state_root = snapshot.GetStateRoot(index);
            if (state_root is null)
                ConsoleHelper.Warning("Unknown state root");
            else
                ConsoleHelper.Info(state_root.ToJson().ToString());
        }

        [ConsoleCommand("state height", Category = "StateService", Description = "Get current state root index")]
        private void OnGetStateHeight()
        {
            if (_system is null || _system.Settings.Network != Settings.Default.Network) throw new InvalidOperationException("Network doesn't match");
            ConsoleHelper.Info("LocalRootIndex: ",
                $"{StateStore.Singleton.LocalRootIndex}",
                " ValidatedRootIndex: ",
                $"{StateStore.Singleton.ValidatedRootIndex}");
        }

        [ConsoleCommand("get proof", Category = "StateService", Description = "Get proof of key and contract hash")]
        private void OnGetProof(UInt256 root_hash, UInt160 script_hash, string key)
        {
            if (_system is null || _system.Settings.Network != Settings.Default.Network) throw new InvalidOperationException("Network doesn't match");
            try
            {
                ConsoleHelper.Info("Proof: ", GetProof(root_hash, script_hash, Convert.FromBase64String(key)));
            }
            catch (RpcException e)
            {
                ConsoleHelper.Error(e.Message);
            }
        }

        [ConsoleCommand("verify proof", Category = "StateService", Description = "Verify proof, return value if successed")]
        private void OnVerifyProof(UInt256 root_hash, string proof)
        {
            try
            {
                ConsoleHelper.Info("Verify Result: ",
                    VerifyProof(root_hash, Convert.FromBase64String(proof)));
            }
            catch (RpcException e)
            {
                ConsoleHelper.Error(e.Message);
            }
        }

        [RpcMethod]
        public JToken GetStateRoot(JArray _params)
        {
            uint index = Result.Ok_Or(() => uint.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid state root index: {_params[0]}"));
            using var snapshot = StateStore.Singleton.GetSnapshot();
            StateRoot state_root = snapshot.GetStateRoot(index).NotNull_Or(RpcError.UnknownStateRoot);
            return state_root.ToJson();
        }

        private string GetProof(Trie trie, int contract_id, byte[] key)
        {
            StorageKey skey = new()
            {
                Id = contract_id,
                Key = key,
            };
            return GetProof(trie, skey);
        }

        private string GetProof(Trie trie, StorageKey skey)
        {
            trie.TryGetProof(skey.ToArray(), out var proof).True_Or(RpcError.UnknownStorageItem);
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms, Utility.StrictUTF8);

            writer.WriteVarBytes(skey.ToArray());
            writer.WriteVarInt(proof.Count);
            foreach (var item in proof)
            {
                writer.WriteVarBytes(item);
            }
            writer.Flush();

            return Convert.ToBase64String(ms.ToArray());
        }

        private string GetProof(UInt256 root_hash, UInt160 script_hash, byte[] key)
        {
            (!Settings.Default.FullState && StateStore.Singleton.CurrentLocalRootHash != root_hash).False_Or(RpcError.UnsupportedState);
            using var store = StateStore.Singleton.GetStoreSnapshot();
            var trie = new Trie(store, root_hash);
            var contract = GetHistoricalContractState(trie, script_hash).NotNull_Or(RpcError.UnknownContract);
            return GetProof(trie, contract.Id, key);
        }

        [RpcMethod]
        public JToken GetProof(JArray _params)
        {
            Result.True_Or(_params.Count == 3, RpcError.InvalidParams.WithData("Invalid params, need a root hash, a script hash, a key."));
            UInt256 root_hash = Result.Ok_Or(() => UInt256.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid root hash: {_params[0]}"));
            UInt160 script_hash = Result.Ok_Or(() => UInt160.Parse(_params[1].AsString()), RpcError.InvalidParams.WithData($"Invalid script hash: {_params[1]}"));
            byte[] key = Result.Ok_Or(() => Convert.FromBase64String(_params[2].AsString()), RpcError.InvalidParams.WithData($"Invalid key: {_params[2]}"));
            return GetProof(root_hash, script_hash, key);
        }

        private string VerifyProof(UInt256 root_hash, byte[] proof)
        {
            var proofs = new HashSet<byte[]>();

            using MemoryStream ms = new(proof, false);
            using BinaryReader reader = new(ms, Utility.StrictUTF8);

            var key = reader.ReadVarBytes(Node.MaxKeyLength);
            var count = reader.ReadVarInt();
            for (ulong i = 0; i < count; i++)
            {
                proofs.Add(reader.ReadVarBytes());
            }

            var value = Trie.VerifyProof(root_hash, key, proofs).NotNull_Or(RpcError.InvalidProof);
            return Convert.ToBase64String(value);
        }

        [RpcMethod]
        public JToken VerifyProof(JArray _params)
        {
            UInt256 root_hash = Result.Ok_Or(() => UInt256.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid root hash: {_params[0]}"));
            byte[] proof_bytes = Result.Ok_Or(() => Convert.FromBase64String(_params[1].AsString()), RpcError.InvalidParams.WithData($"Invalid proof: {_params[1]}"));
            return VerifyProof(root_hash, proof_bytes);
        }

        [RpcMethod]
        public JToken GetStateHeight(JArray _params)
        {
            var json = new JObject();
            json["localrootindex"] = StateStore.Singleton.LocalRootIndex;
            json["validatedrootindex"] = StateStore.Singleton.ValidatedRootIndex;
            return json;
        }

        private ContractState GetHistoricalContractState(Trie trie, UInt160 script_hash)
        {
            const byte prefix = 8;
            StorageKey skey = new KeyBuilder(NativeContract.ContractManagement.Id, prefix).Add(script_hash);
            return trie.TryGetValue(skey.ToArray(), out var value) ? value.AsSerializable<StorageItem>().GetInteroperable<ContractState>() : null;
        }

        private StorageKey ParseStorageKey(byte[] data)
        {
            return new()
            {
                Id = BinaryPrimitives.ReadInt32LittleEndian(data),
                Key = data.AsMemory(sizeof(int)),
            };
        }

        [RpcMethod]
        public JToken FindStates(JArray _params)
        {
            Result.True_Or(_params.Count >= 3 || _params.Count <= 5, RpcError.InvalidParams.WithData("Invalid params, need a root hash, a script hash, a prefix, a key(optional), a count(optional)."));
            var root_hash = Result.Ok_Or(() => UInt256.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid root hash: {_params[0]}"));
            (!Settings.Default.FullState && StateStore.Singleton.CurrentLocalRootHash != root_hash).False_Or(RpcError.UnsupportedState);
            var script_hash = Result.Ok_Or(() => UInt160.Parse(_params[1].AsString()), RpcError.InvalidParams.WithData($"Invalid script hash: {_params[1]}"));
            var prefix = Result.Ok_Or(() => Convert.FromBase64String(_params[2].AsString()), RpcError.InvalidParams.WithData($"Invalid prefix: {_params[2]}"));
            byte[] key = Array.Empty<byte>();
            if (3 < _params.Count)
                key = Result.Ok_Or(() => Convert.FromBase64String(_params[3].AsString()), RpcError.InvalidParams.WithData($"Invalid key: {_params[3]}"));
            int count = Settings.Default.MaxFindResultItems;
            if (4 < _params.Count)
                count = Result.Ok_Or(() => int.Parse(_params[4].AsString()), RpcError.InvalidParams.WithData($"Invalid count: {_params[4]}"));
            if (Settings.Default.MaxFindResultItems < count)
                count = Settings.Default.MaxFindResultItems;
            using var store = StateStore.Singleton.GetStoreSnapshot();
            var trie = new Trie(store, root_hash);
            var contract = GetHistoricalContractState(trie, script_hash).NotNull_Or(RpcError.UnknownContract);
            StorageKey pkey = new()
            {
                Id = contract.Id,
                Key = prefix,
            };
            StorageKey fkey = new()
            {
                Id = pkey.Id,
                Key = key,
            };
            JObject json = new();
            JArray jarr = new();
            int i = 0;
            foreach (var (ikey, ivalue) in trie.Find(pkey.ToArray(), 0 < key.Length ? fkey.ToArray() : null))
            {
                if (count < i) break;
                if (i < count)
                {
                    JObject j = new();
                    j["key"] = Convert.ToBase64String(ParseStorageKey(ikey.ToArray()).Key.Span);
                    j["value"] = Convert.ToBase64String(ivalue.Span);
                    jarr.Add(j);
                }
                i++;
            };
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
        public JToken GetState(JArray _params)
        {
            var root_hash = Result.Ok_Or(() => UInt256.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid root hash: {_params[0]}"));
            (!Settings.Default.FullState && StateStore.Singleton.CurrentLocalRootHash != root_hash).False_Or(RpcError.UnsupportedState);
            var script_hash = Result.Ok_Or(() => UInt160.Parse(_params[1].AsString()), RpcError.InvalidParams.WithData($"Invalid script hash: {_params[1]}"));
            var key = Result.Ok_Or(() => Convert.FromBase64String(_params[2].AsString()), RpcError.InvalidParams.WithData($"Invalid key: {_params[2]}"));
            using var store = StateStore.Singleton.GetStoreSnapshot();
            var trie = new Trie(store, root_hash);

            var contract = GetHistoricalContractState(trie, script_hash).NotNull_Or(RpcError.UnknownContract);
            StorageKey skey = new()
            {
                Id = contract.Id,
                Key = key,
            };
            return Convert.ToBase64String(trie[skey.ToArray()]);
        }
    }
}
