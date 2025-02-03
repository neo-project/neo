// Copyright (C) 2015-2025 The Neo Project.
//
// StateStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.StateService.Network;
using Neo.Plugins.StateService.Verification;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Neo.Plugins.StateService.Storage
{
    class StateStore : UntypedActor
    {
        private readonly StatePlugin system;
        private readonly IStore store;
        private const int MaxCacheCount = 100;
        private readonly Dictionary<uint, StateRoot> cache = new Dictionary<uint, StateRoot>();
        private StateSnapshot currentSnapshot;
        private StateSnapshot _state_snapshot;
        public UInt256 CurrentLocalRootHash => currentSnapshot.CurrentLocalRootHash();
        public uint? LocalRootIndex => currentSnapshot.CurrentLocalRootIndex();
        public uint? ValidatedRootIndex => currentSnapshot.CurrentValidatedRootIndex();

        private static StateStore singleton;
        public static StateStore Singleton
        {
            get
            {
                while (singleton is null) Thread.Sleep(10);
                return singleton;
            }
        }

        public StateStore(StatePlugin system, string path)
        {
            if (singleton != null) throw new InvalidOperationException(nameof(StateStore));
            this.system = system;
            store = StatePlugin._system.LoadStore(path);
            singleton = this;
            StatePlugin._system.ActorSystem.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
            UpdateCurrentSnapshot();
        }

        public void Dispose()
        {
            store.Dispose();
        }

        public StateSnapshot GetSnapshot()
        {
            return new StateSnapshot(store);
        }

        public ISnapshot GetStoreSnapshot()
        {
            return store.GetSnapshot();
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StateRoot state_root:
                    OnNewStateRoot(state_root);
                    break;
                case Blockchain.RelayResult rr:
                    if (rr.Result == VerifyResult.Succeed && rr.Inventory is ExtensiblePayload payload && payload.Category == StatePlugin.StatePayloadCategory)
                        OnStatePayload(payload);
                    break;
                default:
                    break;
            }
        }

        private void OnStatePayload(ExtensiblePayload payload)
        {
            if (payload.Data.Length == 0) return;
            if ((MessageType)payload.Data.Span[0] != MessageType.StateRoot) return;
            StateRoot message;
            try
            {
                message = payload.Data[1..].AsSerializable<StateRoot>();
            }
            catch (FormatException)
            {
                return;
            }
            OnNewStateRoot(message);
        }

        private bool OnNewStateRoot(StateRoot state_root)
        {
            if (state_root?.Witness is null) return false;
            if (ValidatedRootIndex != null && state_root.Index <= ValidatedRootIndex) return false;
            if (LocalRootIndex is null) throw new InvalidOperationException(nameof(StateStore) + " could not get local root index");
            if (LocalRootIndex < state_root.Index && state_root.Index < LocalRootIndex + MaxCacheCount)
            {
                cache.Add(state_root.Index, state_root);
                return true;
            }
            using var state_snapshot = Singleton.GetSnapshot();
            StateRoot local_root = state_snapshot.GetStateRoot(state_root.Index);
            if (local_root is null || local_root.Witness != null) return false;
            if (!state_root.Verify(StatePlugin._system.Settings, StatePlugin._system.StoreView)) return false;
            if (local_root.RootHash != state_root.RootHash) return false;
            state_snapshot.AddValidatedStateRoot(state_root);
            state_snapshot.Commit();
            UpdateCurrentSnapshot();
            system.Verifier?.Tell(new VerificationService.ValidatedRootPersisted { Index = state_root.Index });
            return true;
        }

        public void UpdateLocalStateRootSnapshot(uint height, List<StorageCache.CacheEntry> change_set)
        {
            _state_snapshot = Singleton.GetSnapshot();
            foreach (var item in change_set)
            {
                switch (item.State)
                {
                    case TrackState.Added:
                        _state_snapshot.Trie.Put(item.Key.ToArray(), item.Value.ToArray());
                        break;
                    case TrackState.Changed:
                        _state_snapshot.Trie.Put(item.Key.ToArray(), item.Value.ToArray());
                        break;
                    case TrackState.Deleted:
                        _state_snapshot.Trie.Delete(item.Key.ToArray());
                        break;
                }
            }
            UInt256 root_hash = _state_snapshot.Trie.Root.Hash;
            StateRoot state_root = new StateRoot
            {
                Version = StateRoot.CurrentVersion,
                Index = height,
                RootHash = root_hash,
                Witness = null,
            };
            _state_snapshot.AddLocalStateRoot(state_root);
        }

        public void UpdateLocalStateRoot(uint height)
        {
            _state_snapshot?.Commit();
            _state_snapshot = null;
            UpdateCurrentSnapshot();
            system.Verifier?.Tell(new VerificationService.BlockPersisted { Index = height });
            CheckValidatedStateRoot(height);
        }

        private void CheckValidatedStateRoot(uint index)
        {
            if (cache.TryGetValue(index, out StateRoot state_root))
            {
                cache.Remove(index);
                Self.Tell(state_root);
            }
        }

        private void UpdateCurrentSnapshot()
        {
            Interlocked.Exchange(ref currentSnapshot, GetSnapshot())?.Dispose();
        }

        protected override void PostStop()
        {
            base.PostStop();
        }

        public static Props Props(StatePlugin system, string path)
        {
            return Akka.Actor.Props.Create(() => new StateStore(system, path));
        }
    }
}
