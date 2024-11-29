// Copyright (C) 2015-2024 The Neo Project.
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
using Neo.IO;
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
        private readonly StatePlugin _system;
        private readonly IStore _store;
        private const int MaxCacheCount = 100;
        private readonly Dictionary<uint, StateRoot> _cache = new Dictionary<uint, StateRoot>();
        private StateSnapshot _currentSnapshot;
        private StateSnapshot _stateSnapshot;
        public UInt256 CurrentLocalRootHash => _currentSnapshot.CurrentLocalRootHash();
        public uint? LocalRootIndex => _currentSnapshot.CurrentLocalRootIndex();
        public uint? ValidatedRootIndex => _currentSnapshot.CurrentValidatedRootIndex();

        private static StateStore s_singleton;
        public static StateStore Singleton
        {
            get
            {
                while (s_singleton is null) Thread.Sleep(10);
                return s_singleton;
            }
        }

        public StateStore(StatePlugin system, string path)
        {
            if (s_singleton != null) throw new InvalidOperationException(nameof(StateStore));
            _system = system;
            _store = StatePlugin._system.LoadStore(path);
            s_singleton = this;
            StatePlugin._system.ActorSystem.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
            UpdateCurrentSnapshot();
        }

        public void Dispose()
        {
            _store.Dispose();
        }

        public StateSnapshot GetSnapshot()
        {
            return new StateSnapshot(_store);
        }

        public ISnapshot GetStoreSnapshot()
        {
            return _store.GetSnapshot();
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StateRoot stateRoot:
                    OnNewStateRoot(stateRoot);
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

        private bool OnNewStateRoot(StateRoot stateRoot)
        {
            if (stateRoot?.Witness is null) return false;
            if (ValidatedRootIndex != null && stateRoot.Index <= ValidatedRootIndex) return false;
            if (LocalRootIndex is null) throw new InvalidOperationException(nameof(StateStore) + " could not get local root index");
            if (LocalRootIndex < stateRoot.Index && stateRoot.Index < LocalRootIndex + MaxCacheCount)
            {
                _cache.Add(stateRoot.Index, stateRoot);
                return true;
            }
            using var stateSnapshot = Singleton.GetSnapshot();
            var localRoot = stateSnapshot.GetStateRoot(stateRoot.Index);
            if (localRoot is null || localRoot.Witness != null) return false;
            if (!stateRoot.Verify(StatePlugin._system.Settings, StatePlugin._system.StoreView)) return false;
            if (localRoot.RootHash != stateRoot.RootHash) return false;
            stateSnapshot.AddValidatedStateRoot(stateRoot);
            stateSnapshot.Commit();
            UpdateCurrentSnapshot();
            _system.Verifier?.Tell(new VerificationService.ValidatedRootPersisted { Index = stateRoot.Index });
            return true;
        }

        // StateStore.Singleton.UpdateLocalStateRootSnapshot(block.Index, snapshot.GetChangeSet().Where(p => p.State != TrackState.None).Where(p => p.Key.Id != NativeContract.Ledger.Id).ToList());

        public void UpdateLocalStateRootSnapshot(uint height, List<DataCache.Trackable> changeSet)
        {
            _stateSnapshot = Singleton.GetSnapshot();
            foreach (var item in changeSet)
            {
                switch (item.State)
                {
                    case TrackState.Added:
                        _stateSnapshot.Trie.Put(item.Key.ToArray(), item.Item.ToArray());
                        break;
                    case TrackState.Changed:
                        _stateSnapshot.Trie.Put(item.Key.ToArray(), item.Item.ToArray());
                        break;
                    case TrackState.Deleted:
                        _stateSnapshot.Trie.Delete(item.Key.ToArray());
                        break;
                }
            }
            var rootHash = _stateSnapshot.Trie.Root.Hash;
            var stateRoot = new StateRoot
            {
                Version = StateRoot.CurrentVersion,
                Index = height,
                RootHash = rootHash,
                Witness = null,
            };
            _stateSnapshot.AddLocalStateRoot(stateRoot);
        }

        public void UpdateLocalStateRoot(uint height)
        {
            _stateSnapshot?.Commit();
            _stateSnapshot = null;
            UpdateCurrentSnapshot();
            _system.Verifier?.Tell(new VerificationService.BlockPersisted { Index = height });
            CheckValidatedStateRoot(height);
        }

        private void CheckValidatedStateRoot(uint index)
        {
            if (_cache.Remove(index, out var stateRoot))
            {
                Self.Tell(stateRoot);
            }
        }

        private void UpdateCurrentSnapshot()
        {
            Interlocked.Exchange(ref _currentSnapshot, GetSnapshot())?.Dispose();
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
