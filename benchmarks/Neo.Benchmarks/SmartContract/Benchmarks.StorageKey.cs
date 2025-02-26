// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.StorageKey.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using System.Text;

namespace Neo.SmartContract.Benchmark
{
    public class Benchmarks_StorageKey
    {
        // for avoiding overhead of encoding
        private static readonly byte[] testBytes = Encoding.ASCII.GetBytes("StorageKey");

        private const int prefixSize = sizeof(int) + sizeof(byte);

        [Benchmark]
        public void KeyBuilder_AddInt()
        {
            var key = new KeyBuilder(1, 0)
                .AddBigEndian(1)
                .AddBigEndian(2)
                .AddBigEndian(3);

            var bytes = key.ToArray();
            if (bytes.Length != prefixSize + 3 * sizeof(int))
                throw new InvalidOperationException();
        }

        [Benchmark]
        public void KeyBuilder_AddBytes()
        {
            var key = new KeyBuilder(1, 0)
                .Add(testBytes)
                .Add(testBytes)
                .Add(testBytes);

            var bytes = key.ToArray();
            if (bytes.Length != prefixSize + 3 * testBytes.Length)
                throw new InvalidOperationException();
        }

        [Benchmark]
        public void KeyBuilder_AddUInt160()
        {
            Span<byte> value = stackalloc byte[UInt160.Length];
            var key = new KeyBuilder(1, 0)
                .Add(new UInt160(value));

            var bytes = key.ToArray();
            if (bytes.Length != prefixSize + UInt160.Length)
                throw new InvalidOperationException();
        }
    }
}
