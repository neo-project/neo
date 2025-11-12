// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.UInt160.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.Benchmarks;

public class Benchmarks_UInt160
{
    static readonly UInt160 s_newUInt160 = new([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1]);

    [Benchmark]
    public static void TestGernerator1()
    {
        _ = new UInt160();
    }

    [Benchmark]
    public static void TestGernerator2()
    {
        _ = new UInt160(new byte[20]);
    }

    [Benchmark]
    public static void TestCompareTo()
    {
        UInt160.Zero.CompareTo(UInt160.Zero);
        UInt160.Zero.CompareTo(s_newUInt160);
        s_newUInt160.CompareTo(UInt160.Zero);
    }

    [Benchmark]
    public static void TestEquals()
    {
        UInt160.Zero.Equals(UInt160.Zero);
        UInt160.Zero.Equals(s_newUInt160);
        s_newUInt160.Equals(null);
    }

    [Benchmark]
    public static void TestParse()
    {
        _ = UInt160.Parse("0x0000000000000000000000000000000000000000");
        _ = UInt160.Parse("0000000000000000000000000000000000000000");
    }

    [Benchmark]
    public static void TestTryParse()
    {
        _ = UInt160.TryParse("0x0000000000000000000000000000000000000000", out _);
        _ = UInt160.TryParse("0x1230000000000000000000000000000000000000", out _);
        _ = UInt160.TryParse("000000000000000000000000000000000000000", out _);
    }

    [Benchmark]
    public static void TestOperatorLarger()
    {
        _ = s_newUInt160 > UInt160.Zero;
    }

    [Benchmark]
    public static void TestOperatorLargerAndEqual()
    {
        _ = s_newUInt160 >= UInt160.Zero;
    }

    [Benchmark]
    public static void TestOperatorSmaller()
    {
        _ = s_newUInt160 < UInt160.Zero;
    }

    [Benchmark]
    public static void TestOperatorSmallerAndEqual()
    {
        _ = s_newUInt160 <= UInt160.Zero;
    }
}
