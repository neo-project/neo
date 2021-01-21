using Neo.IO;
using Neo.IO.Caching;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Persistence
{
    public class MemoryStore : IStore
    {
        private readonly ConcurrentDictionary<byte[], byte[]> innerData = new ConcurrentDictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);

        public void Delete(byte[] key)
        {
            innerData.TryRemove(key.EnsureNotNull(), out _);
        }

        public void Dispose()
        {
        }

        public ISnapshot GetSnapshot()
        {
            return new MemorySnapshot(innerData);
        }

        public void Put(byte[] key, byte[] value)
        {
            innerData[key.EnsureNotNull()] = value;
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            ByteArrayComparer comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            IEnumerable<KeyValuePair<byte[], byte[]>> records = innerData;
            if (keyOrPrefix?.Length > 0)
                records = records.Where(p => comparer.Compare(p.Key, keyOrPrefix) >= 0);
            records = records.OrderBy(p => p.Key, comparer);
            foreach (var pair in records)
                yield return (pair.Key, pair.Value);
        }

        public byte[] TryGet(byte[] key)
        {
            innerData.TryGetValue(key.EnsureNotNull(), out byte[] value);
            return value;
        }

        public bool Contains(byte[] key)
        {
            return innerData.ContainsKey(key.EnsureNotNull());
        }
    }
}
