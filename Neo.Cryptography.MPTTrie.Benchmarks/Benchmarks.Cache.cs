// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.Cache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using System;
using System.Security.Policy;

namespace Neo.Cryptography.MPTTrie.Benchmarks
{
    public class Benchmarks_Cache
    {
        private readonly byte _prefix = 0x01;
        private readonly UInt256 _hash;

        public Benchmarks_Cache()
        {
            var randomBytes = new byte[UInt256.Length];
            new Random(42).NextBytes(randomBytes);
            _hash = new UInt256(randomBytes);
        }

        [Benchmark]
        public byte[] Key_Original()
        {
            var buffer = new byte[UInt256.Length + 1];
            using (var ms = new MemoryStream(buffer, true))
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(_prefix);
                _hash.Serialize(writer);
            }
            return buffer;
        }

        [Benchmark]
        public byte[] Key_Optimized()
        {
            var buffer = new byte[UInt256.Length + 1];
            buffer[0] = _prefix;
            _hash.GetSpan().CopyTo(buffer.AsSpan(1));
            return buffer;
        }
    }
}
