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

#nullable enable

using Akka.Actor;
using Neo.Extensions;
using Neo.IEventHandlers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.Plugins.StateService.Network;
using Neo.Plugins.StateService.Verification;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Neo.Plugins.StateService.Storage
{
    class StateStore : UntypedActor
    {
        private readonly StatePlugin _system;
        private readonly IStore _store;
        private const int MaxCacheCount = 100;
        private readonly Dictionary<uint, StateRoot> _cache = [];
        private StateSnapshot _currentSnapshot;
        private StateSnapshot? _stateSnapshot;
        public UInt256 CurrentLocalRootHash => _currentSnapshot.CurrentLocalRootHash();
        public uint? LocalRootIndex => _currentSnapshot.CurrentLocalRootIndex();
        public uint? ValidatedRootIndex => _currentSnapshot.CurrentValidatedRootIndex();

        private static StateStore? _singleton;
        public static StateStore Singleton
        {
            get
            {
                while (_singleton is null) Thread.Sleep(10);
                return _singleton;
            }
        }

        public StateStore(StatePlugin system, string path)
        {
            if (_singleton != null) throw new InvalidOperationException(nameof(StateStore));
            _system = system;
            _store = StatePlugin.NeoSystem.LoadStore(path);
            _singleton = this;
            StatePlugin.NeoSystem.ActorSystem.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
            _currentSnapshot = GetSnapshot();
        }

        public void Dispose()
        {
            _store.Dispose();
        }

        public StateSnapshot GetSnapshot()
        {
            return new StateSnapshot(_store);
        }

        public IStoreSnapshot GetStoreSnapshot()
        {
            return _store.GetSnapshot();
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

        private bool OnNewStateRoot(StateRoot stateRoot)
        {
            if (stateRoot.Witness is null)
            {
                PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                    StateServiceTelemetryEventType.SnapshotError,
                    stateRoot.Index,
                    LocalRootIndex,
                    ValidatedRootIndex,
                    stage: "validate",
                    reason: "MissingWitness"));
                return false;
            }

            if (ValidatedRootIndex != null && stateRoot.Index <= ValidatedRootIndex)
            {
                PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                    StateServiceTelemetryEventType.SnapshotError,
                    stateRoot.Index,
                    LocalRootIndex,
                    ValidatedRootIndex,
                    stage: "validate",
                    reason: "DuplicateOrOldStateRoot"));
                return false;
            }

            if (LocalRootIndex is null)
            {
                PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                    StateServiceTelemetryEventType.SnapshotError,
                    stateRoot.Index,
                    null,
                    ValidatedRootIndex,
                    stage: "validate",
                    reason: "LocalRootUnavailable"));
                throw new InvalidOperationException(nameof(StateStore) + " could not get local root index");
            }

            if (LocalRootIndex < stateRoot.Index && stateRoot.Index < LocalRootIndex + MaxCacheCount)
            {
                _cache[stateRoot.Index] = stateRoot;
                PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                    StateServiceTelemetryEventType.SnapshotApplied,
                    stateRoot.Index,
                    LocalRootIndex,
                    ValidatedRootIndex,
                    stage: "cache"));
                return true;
            }

            using var stateSnapshot = Singleton.GetSnapshot();
            var localRoot = stateSnapshot.GetStateRoot(stateRoot.Index);
            if (localRoot is null)
            {
                PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                    StateServiceTelemetryEventType.SnapshotError,
                    stateRoot.Index,
                    LocalRootIndex,
                    ValidatedRootIndex,
                    stage: "validate",
                    reason: "MissingLocalStateRoot"));
                return false;
            }

            if (localRoot.Witness != null)
            {
                PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                    StateServiceTelemetryEventType.SnapshotError,
                    stateRoot.Index,
                    LocalRootIndex,
                    ValidatedRootIndex,
                    stage: "validate",
                    reason: "StateRootAlreadyValidated"));
                return false;
            }

            var stopwatch = Stopwatch.StartNew();
            if (!stateRoot.Verify(StatePlugin.NeoSystem.Settings, StatePlugin.NeoSystem.StoreView))
            {
                stopwatch.Stop();
                PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                    StateServiceTelemetryEventType.SnapshotError,
                    stateRoot.Index,
                    LocalRootIndex,
                    ValidatedRootIndex,
                    duration: stopwatch.Elapsed,
                    stage: "validate",
                    reason: "WitnessVerificationFailed"));
                return false;
            }

            if (localRoot.RootHash != stateRoot.RootHash)
            {
                stopwatch.Stop();
                PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                    StateServiceTelemetryEventType.SnapshotError,
                    stateRoot.Index,
                    LocalRootIndex,
                    ValidatedRootIndex,
                    duration: stopwatch.Elapsed,
                    stage: "validate",
                    reason: "RootHashMismatch"));
                return false;
            }

            stateSnapshot.AddValidatedStateRoot(stateRoot);
            stateSnapshot.Commit();
            stopwatch.Stop();
            UpdateCurrentSnapshot();
            PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                StateServiceTelemetryEventType.ValidatedRootAdvanced,
                stateRoot.Index,
                LocalRootIndex,
                stateRoot.Index,
                stopwatch.Elapsed,
                stage: "validate"));
            _system.Verifier?.Tell(new VerificationService.ValidatedRootPersisted { Index = stateRoot.Index });
            return true;
        }

        public void UpdateLocalStateRootSnapshot(uint height, IEnumerable<KeyValuePair<StorageKey, DataCache.Trackable>> changeSet)
        {
            _stateSnapshot?.Dispose();
            _stateSnapshot = Singleton.GetSnapshot();
            var stopwatch = Stopwatch.StartNew();
            var changeCount = 0;
            foreach (var item in changeSet)
            {
                switch (item.Value.State)
                {
                    case TrackState.Added:
                        _stateSnapshot.Trie.Put(item.Key.ToArray(), item.Value.Item.ToArray());
                        changeCount++;
                        break;
                    case TrackState.Changed:
                        _stateSnapshot.Trie.Put(item.Key.ToArray(), item.Value.Item.ToArray());
                        changeCount++;
                        break;
                    case TrackState.Deleted:
                        _stateSnapshot.Trie.Delete(item.Key.ToArray());
                        changeCount++;
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
            stopwatch.Stop();
            PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                StateServiceTelemetryEventType.SnapshotApplied,
                height,
                height,
                ValidatedRootIndex,
                stopwatch.Elapsed,
                changeCount,
                "apply"));
        }

        public void UpdateLocalStateRoot(uint height)
        {
            TimeSpan? duration = null;
            if (_stateSnapshot != null)
            {
                var stopwatch = Stopwatch.StartNew();
                _stateSnapshot.Commit();
                stopwatch.Stop();
                duration = stopwatch.Elapsed;
                _stateSnapshot.Dispose();
                _stateSnapshot = null;
            }
            UpdateCurrentSnapshot();
            _system.Verifier?.Tell(new VerificationService.BlockPersisted { Index = height });
            CheckValidatedStateRoot(height);
            PublishStateTelemetry(new StateServiceTelemetryEventArgs(
                StateServiceTelemetryEventType.LocalRootCommitted,
                height,
                LocalRootIndex,
                ValidatedRootIndex,
                duration,
                stage: "commit"));
        }

        private void CheckValidatedStateRoot(uint index)
        {
            if (_cache.TryGetValue(index, out var stateRoot))
            {
                _cache.Remove(index);
                Self.Tell(stateRoot);
            }
        }

        private void UpdateCurrentSnapshot()
        {
            Interlocked.Exchange(ref _currentSnapshot, GetSnapshot())?.Dispose();
        }

        private static void PublishStateTelemetry(StateServiceTelemetryEventArgs args)
        {
            foreach (var handler in Plugin.Plugins.OfType<IStateServiceDiagnosticsHandler>())
            {
                try
                {
                    handler.OnStateServiceTelemetry(args);
                }
                catch
                {
                    // Telemetry consumers should not disrupt state service execution.
                }
            }
        }

        protected override void PostStop()
        {
            _currentSnapshot?.Dispose();
            _store?.Dispose();
            base.PostStop();
        }

        public static Props Props(StatePlugin system, string path)
        {
            return Akka.Actor.Props.Create(() => new StateStore(system, path));
        }
    }
}

#nullable disable
