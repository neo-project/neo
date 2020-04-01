using Neo.Cryptography;
using Neo.IO;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Trie.MPT
{
    public class MPTProofStore : IReadOnlyStore
    {
        private Dictionary<byte[], byte[]> store = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);

        public MPTProofStore(HashSet<byte[]> proof)
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
            return records.Select(p => (p.Key, p.Value));
        }
    }
}
