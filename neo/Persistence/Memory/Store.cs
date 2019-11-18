using Neo.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Neo.Persistence.Memory
{
    public class Store : IStore
    {
        private readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();
        private readonly Dictionary<byte[], byte[]>[] innerData;

        public Store()
        {
            innerData = new Dictionary<byte[], byte[]>[256];
            for (int i = 0; i < innerData.Length; i++)
                innerData[i] = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);
        }

        public void Delete(byte table, byte[] key)
        {
            readerWriterLock.EnterWriteLock();
            innerData[table].Remove(key.EnsureNotNull());
            readerWriterLock.ExitWriteLock();
        }

        public void Dispose()
        {
            readerWriterLock.Dispose();
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Find(byte table, byte[] prefix)
        {
            IEnumerable<KeyValuePair<byte[], byte[]>> records = innerData[table];
            if (prefix?.Length > 0)
                records = records.Where(p => p.Key.Length >= prefix.Length && p.Key.Take(prefix.Length).SequenceEqual(prefix));
            records = records.OrderBy(p => p.Key, ByteArrayComparer.Default);
            readerWriterLock.EnterReadLock();
            foreach (var pair in records)
                yield return (pair.Key, pair.Value);
            readerWriterLock.ExitReadLock();
        }

        public ISnapshot GetSnapshot()
        {
            return new Snapshot(innerData, readerWriterLock);
        }

        public void Put(byte table, byte[] key, byte[] value)
        {
            PutSync(table, key, value);
        }

        public void PutSync(byte table, byte[] key, byte[] value)
        {
            readerWriterLock.EnterWriteLock();
            innerData[table][key.EnsureNotNull()] = value;
            readerWriterLock.ExitWriteLock();
        }

        public byte[] TryGet(byte table, byte[] key)
        {
            readerWriterLock.EnterReadLock();
            innerData[table].TryGetValue(key.EnsureNotNull(), out byte[] value);
            readerWriterLock.ExitReadLock();
            return value;
        }
    }
}
