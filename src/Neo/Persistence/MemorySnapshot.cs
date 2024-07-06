// Copyright (C) 2015-2024 The Neo Project.
//
// MemorySnapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Neo.Persistence
{
    internal class MemorySnapshot : ISnapshot
    {
        private bool isCommitted = false;
        private readonly ConcurrentDictionary<byte[], byte[]> innerData;
        private readonly ImmutableDictionary<byte[], byte[]> immutableData;
        private readonly ConcurrentDictionary<byte[], byte[]> writeBatch;

        public MemorySnapshot(ConcurrentDictionary<byte[], byte[]> innerData)
        {
            this.innerData = innerData;
            immutableData = innerData.ToImmutableDictionary(ByteArrayEqualityComparer.Default);
            writeBatch = new ConcurrentDictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);
        }

        public void Commit()
        {
            foreach (var pair in writeBatch)
                if (pair.Value is null)
                    innerData.TryRemove(pair.Key, out _);
                else
                    innerData[pair.Key] = pair.Value;
            isCommitted = true;
        }

        public void Delete(byte[] key)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            writeBatch[key] = null;
        }

        public void Dispose()
        {
        }

        public void Put(byte[] key, byte[] value)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            writeBatch[key[..]] = value[..];
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            ByteArrayComparer comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            IEnumerable<KeyValuePair<byte[], byte[]>> records = immutableData;
            if (keyOrPrefix?.Length > 0)
                records = records.Where(p => comparer.Compare(p.Key, keyOrPrefix) >= 0);
            records = records.OrderBy(p => p.Key, comparer);
            return records.Select(p => (p.Key[..], p.Value[..]));
        }

        public byte[] TryGet(byte[] key)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            immutableData.TryGetValue(key, out byte[] value);
            return value?[..];
        }

        public bool Contains(byte[] key)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            return immutableData.ContainsKey(key);
        }
    }
}
