// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks.Hash.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Cryptography;
using System.Diagnostics;
using System.IO.Hashing;
using System.Text;

namespace Neo.Benchmarks;

public class Benchmarks_Hash
{
    // 256 KiB
    static readonly byte[] data = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("Hello, World!^_^", 16 * 1024)));

    static readonly byte[] hash = "9182abedfbb9b18d81a05d8bcb45489e7daa2858".HexToBytes();

    [Benchmark]
    public static void XxHash32_HashToUInt32()
    {
        var result = XxHash32.HashToUInt32(data);
        Debug.Assert(result == 682967318u);
    }

    [Benchmark]
    public static void XxHash3_HashToUInt64()
    {
        var result = (uint)XxHash3.HashToUInt64(data);
        Debug.Assert(result == 1389469485u);
    }

    [Benchmark]
    public static void Murmur32_HashToUInt32()
    {
        var result = data.Murmur32(0);
        Debug.Assert(result == 3731881930u);
    }

    [Benchmark]
    public static void Murmur128_ComputeHash()
    {
        var result = data.Murmur128(0);
        if (result.Length != 16)
            throw new InvalidOperationException($"Invalid hash length {result.Length}");
    }
}
