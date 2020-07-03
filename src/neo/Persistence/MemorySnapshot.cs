using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Neo.Persistence
{
    internal class MemorySnapshot : ISnapshot
    {
        private readonly ConcurrentDictionary<byte[], byte[]>[] innerData;
        private readonly ImmutableDictionary<byte[], byte[]>[] immutableData;
        private readonly ConcurrentDictionary<byte[], byte[]>[] writeBatch;

        public MemorySnapshot(ConcurrentDictionary<byte[], byte[]>[] innerData)
        {
            this.innerData = innerData;
            this.immutableData = innerData.Select(p => p.ToImmutableDictionary(ByteArrayEqualityComparer.Default)).ToArray();
            this.writeBatch = new ConcurrentDictionary<byte[], byte[]>[innerData.Length];
            for (int i = 0; i < writeBatch.Length; i++)
                writeBatch[i] = new ConcurrentDictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);
        }

        public void Commit()
        {
            for (int i = 0; i < writeBatch.Length; i++)
                foreach (var pair in writeBatch[i])
                    if (pair.Value is null)
                        innerData[i].TryRemove(pair.Key, out _);
                    else
                        innerData[i][pair.Key] = pair.Value;
        }

        public void Delete(byte table, byte[] key)
        {
            writeBatch[table][key.EnsureNotNull()] = null;
        }

        public void Dispose()
        {
        }

        public void Put(byte table, byte[] key, byte[] value)
        {
            writeBatch[table][key.EnsureNotNull()] = value;
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte table, byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            ByteArrayComparer comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            IEnumerable<KeyValuePair<byte[], byte[]>> records = immutableData[table];
            if (keyOrPrefix?.Length > 0)
                records = records.Where(p => comparer.Compare(p.Key, keyOrPrefix) >= 0);
            records = records.OrderBy(p => p.Key, comparer);
            return records.Select(p => (p.Key, p.Value));
        }

        public byte[] TryGet(byte table, byte[] key)
        {
            immutableData[table].TryGetValue(key.EnsureNotNull(), out byte[] value);
            return value;
        }
    }
}
