using Neo.IO;
using Neo.Cryptography;
using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public class MPTProofStore : IKVStore
    {
        private Dictionary<byte[], byte[]> store = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);

        public MPTProofStore(HashSet<byte[]> proof)
        {
            foreach (byte[] data in proof)
            {
                store.Add(Crypto.Default.Hash256(data), data);
            }
        }

        public byte[] Get(byte[] hash)
        {
            var result = store.TryGetValue(hash, out byte[] value);
            return result ? value : null;
        }

        public void Put(byte[] key, byte[] value)
        {
            throw new System.NotImplementedException(nameof(MPTProofStore));
        }

        public void Delete(byte[] key)
        {
            throw new System.NotImplementedException(nameof(MPTProofStore));
        }
    }
}
