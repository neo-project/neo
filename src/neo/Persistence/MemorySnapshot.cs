using Neo.IO;
using Neo.IO.Caching;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Neo.Persistence
{
    internal class MemorySnapshot : ISnapshot
    {
        private readonly ConcurrentDictionary<byte[], byte[]> innerData;
        private readonly ImmutableDictionary<byte[], byte[]> immutableData;
        private readonly ConcurrentDictionary<byte[], byte[]> writeBatch;

        public MemorySnapshot(ConcurrentDictionary<byte[], byte[]> innerData)
        {
            this.innerData = innerData;
            this.immutableData = innerData.ToImmutableDictionary(ByteArrayEqualityComparer.Default);
            this.writeBatch = new ConcurrentDictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);
        }

        public void Commit()
        {
            foreach (var pair in writeBatch)
                if (pair.Value is null)
                    innerData.TryRemove(pair.Key, out _);
                else
                    innerData[pair.Key] = pair.Value;
        }

        public void Delete(byte[] key)
        {
            writeBatch[key.EnsureNotNull()] = null;
        }

        public void Dispose()
        {
        }

        public void Put(byte[] key, byte[] value)
        {
            writeBatch[key.EnsureNotNull()] = value;
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            ByteArrayComparer comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            IEnumerable<KeyValuePair<byte[], byte[]>> records = immutableData;
            if (keyOrPrefix?.Length > 0)
                records = records.Where(p => comparer.Compare(p.Key, keyOrPrefix) >= 0);
            records = records.OrderBy(p => p.Key, comparer);
            return records.Select(p => (p.Key, p.Value));
        }

        public byte[] TryGet(byte[] key)
        {
            immutableData.TryGetValue(key.EnsureNotNull(), out byte[] value);
            return value;
        }

        public bool Contains(byte[] key)
        {
            return innerData.ContainsKey(key.EnsureNotNull());
        }
    }
}
