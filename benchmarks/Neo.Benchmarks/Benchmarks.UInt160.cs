// Copyright (C) 2015-2024 The Neo Project.
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

namespace Neo.Benchmark;

public class Benchmarks_UInt160
{
    [Benchmark]
    public void TestOldUInt160Gernerator1()
    {
        OldUInt160 uInt160 = new OldUInt160();
    }

    [Benchmark]
    public void TestOldUInt160Gernerator2()
    {
        OldUInt160 uInt160 = new OldUInt160(new byte[20]);
    }

    [Benchmark]
    public void TestOldUInt160CompareTo()
    {
        byte[] temp = new byte[20];
        temp[19] = 0x01;
        OldUInt160 result = new OldUInt160(temp);
        OldUInt160.Zero.CompareTo(OldUInt160.Zero);
        OldUInt160.Zero.CompareTo(result);
        result.CompareTo(OldUInt160.Zero);
    }

    [Benchmark]
    public void TestOldUInt160Equals()
    {
        byte[] temp = new byte[20];
        temp[19] = 0x01;
        OldUInt160 result = new OldUInt160(temp);
        OldUInt160.Zero.Equals(OldUInt160.Zero);
        OldUInt160.Zero.Equals(result);
        result.Equals(null);
    }

    [Benchmark]
    public void TestOldUInt160Parse()
    {
        OldUInt160 result = OldUInt160.Parse("0x0000000000000000000000000000000000000000");
        OldUInt160 result1 = OldUInt160.Parse("0000000000000000000000000000000000000000");
    }

    [Benchmark]
    public void TestOldUInt160TryParse()
    {
        OldUInt160.TryParse(null, out _);
        OldUInt160.TryParse("0x0000000000000000000000000000000000000000", out var temp);
        OldUInt160.TryParse("0x1230000000000000000000000000000000000000", out temp);
        OldUInt160.TryParse("000000000000000000000000000000000000000", out _);
        OldUInt160.TryParse("0xKK00000000000000000000000000000000000000", out _);
    }

    [Benchmark]
    public void TestOldUInt160OperatorLarger()
    {
        _ = OldUInt160.Zero > OldUInt160.Zero;
    }

    [Benchmark]
    public void TestOldUInt160OperatorLargerAndEqual()
    {
        _ = OldUInt160.Zero >= OldUInt160.Zero;
    }

    [Benchmark]
    public void TestOldUInt160OperatorSmaller()
    {
        _ = OldUInt160.Zero < OldUInt160.Zero;
    }

    [Benchmark]
    public void TestOldUInt160OperatorSmallerAndEqual()
    {
        _ = OldUInt160.Zero <= OldUInt160.Zero;
    }

    [Benchmark]
    public void TestGernerator1()
    {
        UInt160 uInt160 = new UInt160();
    }

    [Benchmark]
    public void TestGernerator2()
    {
        UInt160 uInt160 = new UInt160(new byte[20]);
    }

    [Benchmark]
    public void TestCompareTo()
    {
        byte[] temp = new byte[20];
        temp[19] = 0x01;
        UInt160 result = new UInt160(temp);
        UInt160.Zero.CompareTo(UInt160.Zero);
        UInt160.Zero.CompareTo(result);
        result.CompareTo(UInt160.Zero);
    }

    [Benchmark]
    public void TestEquals()
    {
        byte[] temp = new byte[20];
        temp[19] = 0x01;
        UInt160 result = new UInt160(temp);
        UInt160.Zero.Equals(UInt160.Zero);
        UInt160.Zero.Equals(result);
        result.Equals(null);
    }

    [Benchmark]
    public void TestParse()
    {
        UInt160 result = UInt160.Parse("0x0000000000000000000000000000000000000000");
        UInt160 result1 = UInt160.Parse("0000000000000000000000000000000000000000");
    }

    [Benchmark]
    public void TestTryParse()
    {
        UInt160.TryParse(null, out _);
        UInt160.TryParse("0x0000000000000000000000000000000000000000", out var temp);
        UInt160.TryParse("0x1230000000000000000000000000000000000000", out temp);
        UInt160.TryParse("000000000000000000000000000000000000000", out _);
        UInt160.TryParse("0xKK00000000000000000000000000000000000000", out _);
    }

    [Benchmark]
    public void TestOperatorLarger()
    {
        _ = UInt160.Zero > UInt160.Zero;
    }

    [Benchmark]
    public void TestOperatorLargerAndEqual()
    {
        _ = UInt160.Zero >= UInt160.Zero;
    }

    [Benchmark]
    public void TestOperatorSmaller()
    {
        _ = UInt160.Zero < UInt160.Zero;
    }

    [Benchmark]
    public void TestOperatorSmallerAndEqual()
    {
        _ = UInt160.Zero <= UInt160.Zero;
    }
}
