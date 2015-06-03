using System;
using System.Linq;

namespace AntShares.Cryptography
{
    public static class MerkleTree
    {
        public static UInt256 ComputeRoot(params UInt256[] hashes)
        {
            if (hashes.Length == 0)
                throw new ArgumentException();
            if (hashes.Length == 1)
                return hashes[0];
            return new UInt256(ComputeRoot(hashes.Select(p => p.ToArray()).ToArray()));
        }

        private static byte[] ComputeRoot(byte[][] hashes)
        {
            if (hashes.Length == 0)
                throw new ArgumentException();
            if (hashes.Length == 1)
                return hashes[0];
            if (hashes.Length % 2 == 1)
            {
                hashes = hashes.Concat(new byte[][] { hashes[hashes.Length - 1] }).ToArray();
            }
            byte[][] hashes_new = new byte[hashes.Length / 2][];
            for (int i = 0; i < hashes_new.Length; i++)
            {
                hashes_new[i] = hashes[i * 2].Concat(hashes[i * 2 + 1]).Sha256().Sha256();
            }
            return ComputeRoot(hashes_new);
        }
    }
}
