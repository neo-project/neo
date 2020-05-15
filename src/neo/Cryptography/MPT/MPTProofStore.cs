using Neo.IO;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Cryptography.MPT
{
    public class MPTProofStore : IReadOnlyStore
    {
        private readonly Dictionary<byte[], byte[]> store = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);

        public MPTProofStore(IEnumerable<byte[]> proof)
        {
            foreach (byte[] data in proof)
            {
                store.Add(Crypto.Hash256(data), data);
            }
        }

        public byte[] TryGet(byte prefix, byte[] hash)
        {
            var result = store.TryGetValue(hash, out byte[] value);
            return result ? value : null;
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Find(byte table, byte[] prefix)
        {
            IEnumerable<KeyValuePair<byte[], byte[]>> records = store;
            if (prefix?.Length > 0)
                records = records.Where(p => p.Key.AsSpan().StartsWith(prefix));
            records = records.OrderBy(p => p.Key, ByteArrayComparer.Default);
            foreach (var pair in records)
                yield return (pair.Key, pair.Value);
        }
    }
}
