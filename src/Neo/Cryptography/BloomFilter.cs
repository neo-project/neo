// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Linq;

namespace Neo.Cryptography
{
    /// <summary>
    /// Represents a bloom filter.
    /// </summary>
    public class BloomFilter
    {
        private readonly uint[] seeds;
        private readonly BitArray bits;

        /// <summary>
        /// The number of hash functions used by the bloom filter.
        /// </summary>
        public int K => seeds.Length;

        /// <summary>
        /// The size of the bit array used by the bloom filter.
        /// </summary>
        public int M => bits.Length;

        /// <summary>
        /// Used to generate the seeds of the murmur hash functions.
        /// </summary>
        public uint Tweak { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter"/> class.
        /// </summary>
        /// <param name="m">The size of the bit array used by the bloom filter.</param>
        /// <param name="k">The number of hash functions used by the bloom filter.</param>
        /// <param name="nTweak">Used to generate the seeds of the murmur hash functions.</param>
        public BloomFilter(int m, int k, uint nTweak)
        {
            if (k < 0 || m < 0) throw new ArgumentOutOfRangeException();
            this.seeds = Enumerable.Range(0, k).Select(p => (uint)p * 0xFBA4C795 + nTweak).ToArray();
            this.bits = new BitArray(m);
            this.bits.Length = m;
            this.Tweak = nTweak;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter"/> class.
        /// </summary>
        /// <param name="m">The size of the bit array used by the bloom filter.</param>
        /// <param name="k">The number of hash functions used by the bloom filter.</param>
        /// <param name="nTweak">Used to generate the seeds of the murmur hash functions.</param>
        /// <param name="elements">The initial elements contained in this <see cref="BloomFilter"/> object.</param>
        public BloomFilter(int m, int k, uint nTweak, ReadOnlyMemory<byte> elements)
        {
            if (k < 0 || m < 0) throw new ArgumentOutOfRangeException();
            this.seeds = Enumerable.Range(0, k).Select(p => (uint)p * 0xFBA4C795 + nTweak).ToArray();
            this.bits = new BitArray(elements.ToArray());
            this.bits.Length = m;
            this.Tweak = nTweak;
        }

        /// <summary>
        /// Adds an element to the <see cref="BloomFilter"/>.
        /// </summary>
        /// <param name="element">The object to add to the <see cref="BloomFilter"/>.</param>
        public void Add(ReadOnlyMemory<byte> element)
        {
            foreach (uint i in seeds.AsParallel().Select(s => element.Span.Murmur32(s)))
                bits.Set((int)(i % (uint)bits.Length), true);
        }

        /// <summary>
        /// Determines whether the <see cref="BloomFilter"/> contains a specific element.
        /// </summary>
        /// <param name="element">The object to locate in the <see cref="BloomFilter"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="element"/> is found in the <see cref="BloomFilter"/>; otherwise, <see langword="false"/>.</returns>
        public bool Check(byte[] element)
        {
            foreach (uint i in seeds.AsParallel().Select(s => element.Murmur32(s)))
                if (!bits.Get((int)(i % (uint)bits.Length)))
                    return false;
            return true;
        }

        /// <summary>
        /// Gets the bit array in this <see cref="BloomFilter"/>.
        /// </summary>
        /// <param name="newBits">The byte array to store the bits.</param>
        public void GetBits(byte[] newBits)
        {
            bits.CopyTo(newBits, 0);
        }
    }
}
