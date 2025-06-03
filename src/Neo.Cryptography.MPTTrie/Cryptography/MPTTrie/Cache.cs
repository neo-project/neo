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
using System.Collections.Generic;
using System.IO;

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

        private class Trackable
        {
            public Node Node;
            public TrackState State;
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
            byte[] buffer = new byte[UInt256.Length + 1];
            using (var ms = new MemoryStream(buffer, true))
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(_prefix);
                hash.Serialize(writer);
            }
            return buffer;
        }

        public Node Resolve(UInt256 hash)
        {
            if (_cache.TryGetValue(hash, out var t))
            {
                return t.Node?.Clone();
            }

            var n = _store.TryGet(Key(hash), out var data) ? data.AsSerializable<Node>() : null;
            _cache.Add(hash, new Trackable
            {
                Node = n,
                State = TrackState.None,
            });
            return n?.Clone();
        }

        public void PutNode(Node np)
        {
            var n = Resolve(np.Hash);
            if (n is null)
            {
                np.Reference = 1;
                _cache[np.Hash] = new Trackable
                {
                    Node = np.Clone(),
                    State = TrackState.Added,
                };
                return;
            }
            var entry = _cache[np.Hash];
            entry.Node.Reference++;
            entry.State = TrackState.Changed;
        }

        public void DeleteNode(UInt256 hash)
        {
            var n = Resolve(hash);
            if (n is null) return;
            if (1 < n.Reference)
            {
                var entry = _cache[hash];
                entry.Node.Reference--;
                entry.State = TrackState.Changed;
                return;
            }
            _cache[hash] = new Trackable
            {
                Node = null,
                State = TrackState.Deleted,
            };
        }

        public void Commit()
        {
            foreach (var item in _cache)
            {
                switch (item.Value.State)
                {
                    case TrackState.Added:
                    case TrackState.Changed:
                        _store.Put(Key(item.Key), item.Value.Node.ToArray());
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
