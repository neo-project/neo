// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark.StringExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.Extensions
{
    public class Benchmark_StringExtensions
    {
        private const string _testHex = "0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1e1f20";

        [Benchmark]
        public void HexToBytes()
        {
            var bytes = _testHex.HexToBytes();
            if (bytes.Length != 32)
                throw new Exception("Invalid length");
        }
    }
}
