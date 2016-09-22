using System.Collections;
using System.Linq;

namespace AntShares.Cryptography
{
    public class BloomFilter
    {
        private readonly uint[] seeds;
        private readonly BitArray bits;

        public BloomFilter(int m, int k, uint nTweak, byte[] elements = null)
        {
            this.seeds = Enumerable.Range(0, k).Select(p => (uint)p * 0xFBA4C795 + nTweak).ToArray();
            this.bits = elements == null ? new BitArray(m) : new BitArray(elements);
            this.bits.Length = m;
        }

        public void Add(byte[] element)
        {
            foreach (uint i in seeds.AsParallel().Select(s => element.Murmur32(s)))
                bits.Set((int)(i % (uint)bits.Length), true);
        }

        public bool Check(byte[] element)
        {
            foreach (uint i in seeds.AsParallel().Select(s => element.Murmur32(s)))
                if (!bits.Get((int)(i % (uint)bits.Length)))
                    return false;
            return true;
        }

        public void GetBits(byte[] bits)
        {
            bits.CopyTo(bits, 0);
        }
    }
}
