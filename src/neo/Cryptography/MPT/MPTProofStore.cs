using Neo.Persistence;
using System;
using System.Collections.Generic;

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
            foreach (var pair in store)
            {
                if (prefix is null || pair.Key.AsSpan().StartsWith(prefix))
                {
                    yield return (pair.Key, pair.Value);
                }
            }
        }
    }
}
