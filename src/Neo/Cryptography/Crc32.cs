// Copyright (C) 2015-2024 The Neo Project.
//
// Crc32.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    public sealed class Crc32 : HashAlgorithm
    {
        public static readonly uint DefaultPolynomial = 0xedb88320u;
        public static readonly uint DefaultSeed = 0xffffffffu;

        private static uint[]? s_defaultTable;

        public override int HashSize => 32;

        private readonly uint _seed;
        private readonly uint[] _table;
        private uint _hash;

        public Crc32() : this(DefaultPolynomial, DefaultSeed)
        {

        }

        public Crc32(
            uint polynomial,
            uint seed)
        {
            if (BitConverter.IsLittleEndian)
                throw new PlatformNotSupportedException("Not supported on Big Endian processors");

            _table = InitializeTable(polynomial);
            _seed = seed;
        }

        public override void Initialize()
        {
            _hash = _seed;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _hash = CalculateHash(_table, _hash, array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            var hashBuffer = UInt32ToBigEndianBytes(~_hash);
            HashValue = hashBuffer;
            return hashBuffer;
        }

        public static uint Compute(byte[] buffer) =>
            Compute(DefaultSeed, buffer);

        public static uint Compute(uint seed, byte[] buffer) =>
            Compute(DefaultPolynomial, seed, buffer);

        public static uint Compute(uint polynomial, uint seed, byte[] buffer) =>
            ~CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length);

        private static uint[] InitializeTable(uint polynomial)
        {
            if (polynomial == DefaultPolynomial && s_defaultTable != null)
                return s_defaultTable;

            var createTable = new uint[256];
            for (var i = 0u; i < 256u; i++)
            {
                var entry = i;
                for (var j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry >>= 1;
                }
                createTable[i] = entry;
            }

            if (polynomial == DefaultPolynomial)
                s_defaultTable = createTable;

            return createTable;
        }

        private static uint CalculateHash(uint[] table, uint seed, IList<byte> buffer, int start, int size)
        {
            var hash = seed;
            for (var i = start; i < start + size; i++)
                hash = (hash >> 8) ^ table[buffer[i] ^ hash & 0xff];
            return hash;
        }

        private static byte[] UInt32ToBigEndianBytes(uint value)
        {
            var result = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }
    }
}
