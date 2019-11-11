using System.Collections;

namespace Neo.Cryptography
{
    public class BloomFilter
    {
        private readonly uint seed;
        private readonly BitArray bits;

        public int M => bits.Length;

        public uint Tweak { get; private set; }

        public BloomFilter(int m, uint nTweak, byte[] elements = null)
        {
            this.seed = 0xFBA4C795 + nTweak;
            this.bits = elements == null ? new BitArray(m) : new BitArray(elements);
            this.bits.Length = m;
            this.Tweak = nTweak;
        }

        public void Add(byte[] element)
        {
            bits.Set((int)(element.Murmur32(seed) % (uint)bits.Length), true);
        }

        public bool Check(byte[] element)
        {
            if (!bits.Get((int)(element.Murmur32(seed) % (uint)bits.Length)))
                return false;
            return true;
        }

        public void GetBits(byte[] newBits)
        {
            bits.CopyTo(newBits, 0);
        }
    }
}
