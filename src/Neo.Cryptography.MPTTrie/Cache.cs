// Copyright (C) 2015-2025 The Neo Project.
//
// Cache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Persistence;
using System;
using System.Collections.Generic;

namespace Neo.Cryptography.MPTTrie
{
    public class Cache
    {
        private enum TrackState : byte
        {
            None,
            Added,
            Changed,
            Deleted
        }

        private class Trackable(Node? node, TrackState state)
        {
            public Node? Node { get; internal set; } = node;
            public TrackState State { get; internal set; } = state;
        }

        private readonly IStoreSnapshot _store;
        private readonly byte _prefix;
        private readonly Dictionary<UInt256, Trackable> _cache = [];

        public Cache(IStoreSnapshot store, byte prefix)
        {
            _store = store;
            _prefix = prefix;
        }

        private byte[] Key(UInt256 hash)
        {
            var buffer = new byte[UInt256.Length + 1];
            buffer[0] = _prefix;
            hash.Serialize(buffer.AsSpan(1));
            return buffer;
        }

        public Node? Resolve(UInt256 hash) => ResolveInternal(hash).Node?.Clone();

        private Trackable ResolveInternal(UInt256 hash)
        {
            if (_cache.TryGetValue(hash, out var t))
            {
                return t;
            }

            var n = _store.TryGet(Key(hash), out var data) ? data.AsSerializable<Node>() : null;

            t = new Trackable(n, TrackState.None);
            _cache.Add(hash, t);
            return t;
        }

        public void PutNode(Node np)
        {
            var entry = ResolveInternal(np.Hash);
            if (entry.Node is null)
            {
                np.Reference = 1;
                entry.Node = np.Clone();
                entry.State = TrackState.Added;
                return;
            }
            entry.Node.Reference++;
            entry.State = TrackState.Changed;
        }

        public void DeleteNode(UInt256 hash)
        {
            var entry = ResolveInternal(hash);
            if (entry.Node is null) return;
            if (1 < entry.Node.Reference)
            {
                entry.Node.Reference--;
                entry.State = TrackState.Changed;
                return;
            }
            entry.Node = null;
            entry.State = TrackState.Deleted;
        }

        public void Commit()
        {
            foreach (var item in _cache)
            {
                switch (item.Value.State)
                {
                    case TrackState.Added:
                    case TrackState.Changed:
                        _store.Put(Key(item.Key), item.Value.Node!.ToArray());
                        break;
                    case TrackState.Deleted:
                        _store.Delete(Key(item.Key));
                        break;
                }
            }
            _cache.Clear();
        }
    }
}
