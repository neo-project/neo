// Copyright (C) 2015-2025 The Neo Project.
//
// StateSnapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.MPTTrie;
using Neo.Extensions;
using Neo.Persistence;
using Neo.Plugins.StateService.Network;
using System;

namespace Neo.Plugins.StateService.Storage
{
    class StateSnapshot : IDisposable
    {
        private readonly ISnapshot snapshot;
        public Trie Trie;

        public StateSnapshot(IStore store)
        {
            snapshot = store.GetSnapshot();
            Trie = new Trie(snapshot, CurrentLocalRootHash(), Settings.Default.FullState);
        }

        public StateRoot GetStateRoot(uint index)
        {
            return snapshot.TryGet(Keys.StateRoot(index), out var data) ? data.AsSerializable<StateRoot>() : null;
        }

        public void AddLocalStateRoot(StateRoot state_root)
        {
            snapshot.Put(Keys.StateRoot(state_root.Index), state_root.ToArray());
            snapshot.Put(Keys.CurrentLocalRootIndex, BitConverter.GetBytes(state_root.Index));
        }

        public uint? CurrentLocalRootIndex()
        {
            if (snapshot.TryGet(Keys.CurrentLocalRootIndex, out var bytes))
                return BitConverter.ToUInt32(bytes);
            return null;
        }

        public UInt256 CurrentLocalRootHash()
        {
            var index = CurrentLocalRootIndex();
            if (index is null) return null;
            return GetStateRoot((uint)index)?.RootHash;
        }

        public void AddValidatedStateRoot(StateRoot state_root)
        {
            if (state_root?.Witness is null)
                throw new ArgumentException(nameof(state_root) + " missing witness in invalidated state root");
            snapshot.Put(Keys.StateRoot(state_root.Index), state_root.ToArray());
            snapshot.Put(Keys.CurrentValidatedRootIndex, BitConverter.GetBytes(state_root.Index));
        }

        public uint? CurrentValidatedRootIndex()
        {
            if (snapshot.TryGet(Keys.CurrentValidatedRootIndex, out var bytes))
                return BitConverter.ToUInt32(bytes);
            return null;
        }

        public UInt256 CurrentValidatedRootHash()
        {
            var index = CurrentLocalRootIndex();
            if (index is null) return null;
            var state_root = GetStateRoot((uint)index);
            if (state_root is null || state_root.Witness is null)
                throw new InvalidOperationException(nameof(CurrentValidatedRootHash) + " could not get validated state root");
            return state_root.RootHash;
        }

        public void Commit()
        {
            Trie.Commit();
            snapshot.Commit();
        }

        public void Dispose()
        {
            snapshot.Dispose();
        }
    }
}
