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

        private readonly IStoreSnapshot store;
        private readonly byte prefix;
        private readonly Dictionary<UInt256, Trackable> cache = new Dictionary<UInt256, Trackable>();

        public Cache(IStoreSnapshot store, byte prefix)
        {
            this.store = store;
            this.prefix = prefix;
        }

        private byte[] Key(UInt256 hash)
        {
            byte[] buffer = new byte[UInt256.Length + 1];
            using (MemoryStream ms = new MemoryStream(buffer, true))
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(prefix);
                hash.Serialize(writer);
            }
            return buffer;
        }

        public Node Resolve(UInt256 hash) => ResolveInternal(hash).Node?.Clone();

        private Trackable ResolveInternal(UInt256 hash)
        {
            if (cache.TryGetValue(hash, out var t))
            {
                return t;
            }

            var n = store.TryGet(Key(hash), out var data) ? data.AsSerializable<Node>() : null;

            t = new Trackable
            {
                Node = n,
                State = TrackState.None,
            };
            cache.Add(hash, t);
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
            foreach (var item in cache)
            {
                switch (item.Value.State)
                {
                    case TrackState.Added:
                    case TrackState.Changed:
                        store.Put(Key(item.Key), item.Value.Node.ToArray());
                        break;
                    case TrackState.Deleted:
                        store.Delete(Key(item.Key));
                        break;
                }
            }
            cache.Clear();
        }
    }
}
