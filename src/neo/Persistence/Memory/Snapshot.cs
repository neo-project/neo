using Neo.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Neo.Persistence.Memory
{
    internal class Snapshot : ISnapshot
    {
        private readonly ReaderWriterLockSlim readerWriterLock;
        private readonly Dictionary<byte[], byte[]>[] innerData;
        private readonly ImmutableDictionary<byte[], byte[]>[] immutableData;
        private readonly Dictionary<byte[], byte[]>[] writeBatch;

        public Snapshot(Dictionary<byte[], byte[]>[] innerData, ReaderWriterLockSlim readerWriterLock)
        {
            this.readerWriterLock = readerWriterLock;
            this.innerData = innerData;
            readerWriterLock.EnterReadLock();
            this.immutableData = innerData.Select(p => p.ToImmutableDictionary(ByteArrayEqualityComparer.Default)).ToArray();
            readerWriterLock.ExitReadLock();
            this.writeBatch = new Dictionary<byte[], byte[]>[innerData.Length];
            for (int i = 0; i < writeBatch.Length; i++)
                writeBatch[i] = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);
        }

        public void Commit()
        {
            readerWriterLock.EnterWriteLock();
            for (int i = 0; i < writeBatch.Length; i++)
                foreach (var pair in writeBatch[i])
                    if (pair.Value is null)
                        innerData[i].Remove(pair.Key);
                    else
                        innerData[i][pair.Key] = pair.Value;
            readerWriterLock.ExitWriteLock();
        }

        public void Delete(byte table, byte[] key)
        {
            writeBatch[table][key.EnsureNotNull()] = null;
        }

        public void Dispose()
        {
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Find(byte table, byte[] prefix)
        {
            IEnumerable<KeyValuePair<byte[], byte[]>> records = immutableData[table];
            if (prefix?.Length > 0)
                records = records.Where(p => p.Key.Length >= prefix.Length && p.Key.Take(prefix.Length).SequenceEqual(prefix));
            records = records.OrderBy(p => p.Key, ByteArrayComparer.Default);
            return records.Select(p => (p.Key, p.Value));
        }

        public void Put(byte table, byte[] key, byte[] value)
        {
            writeBatch[table][key.EnsureNotNull()] = value;
        }

        public byte[] TryGet(byte table, byte[] key)
        {
            immutableData[table].TryGetValue(key.EnsureNotNull(), out byte[] value);
            return value;
        }
    }
}
