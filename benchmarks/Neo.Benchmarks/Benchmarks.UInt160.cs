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

namespace Neo.Benchmark
{
    public class Benchmarks_UInt160
    {
        static readonly OldUInt160 s_oldUInt160 = new([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1]);
        static readonly UInt160 s_newUInt160 = new([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1]);

        [Benchmark]
        public void TestOldUInt160Gernerator1()
        {
            _ = new OldUInt160();
        }

        [Benchmark]
        public void TestOldUInt160Gernerator2()
        {
            _ = new OldUInt160(new byte[20]);
        }

        [Benchmark]
        public void TestOldUInt160CompareTo()
        {
            OldUInt160.Zero.CompareTo(OldUInt160.Zero);
            OldUInt160.Zero.CompareTo(s_oldUInt160);
            s_oldUInt160.CompareTo(OldUInt160.Zero);
        }

        [Benchmark]
        public void TestOldUInt160Equals()
        {
            OldUInt160.Zero.Equals(OldUInt160.Zero);
            OldUInt160.Zero.Equals(s_oldUInt160);
            s_oldUInt160.Equals(null);
        }

        [Benchmark]
        public void TestOldUInt160Parse()
        {
            _ = OldUInt160.Parse("0x0000000000000000000000000000000000000000");
            _ = OldUInt160.Parse("0000000000000000000000000000000000000000");
        }

        [Benchmark]
        public void TestOldUInt160TryParse()
        {
            OldUInt160.TryParse(null, out _);
            OldUInt160.TryParse("0x0000000000000000000000000000000000000000", out var temp);
            OldUInt160.TryParse("0x1230000000000000000000000000000000000000", out temp);
            OldUInt160.TryParse("000000000000000000000000000000000000000", out _);
        }

        [Benchmark]
        public void TestOldUInt160OperatorLarger()
        {
            _ = s_oldUInt160 > OldUInt160.Zero;
        }

        [Benchmark]
        public void TestOldUInt160OperatorLargerAndEqual()
        {
            _ = s_oldUInt160 >= OldUInt160.Zero;
        }

        [Benchmark]
        public void TestOldUInt160OperatorSmaller()
        {
            _ = s_oldUInt160 < OldUInt160.Zero;
        }

        [Benchmark]
        public void TestOldUInt160OperatorSmallerAndEqual()
        {
            _ = s_oldUInt160 <= OldUInt160.Zero;
        }

        [Benchmark]
        public void TestGernerator1()
        {
            _ = new UInt160();
        }

        [Benchmark]
        public void TestGernerator2()
        {
            _ = new UInt160(new byte[20]);
        }

        [Benchmark]
        public void TestCompareTo()
        {
            UInt160.Zero.CompareTo(UInt160.Zero);
            UInt160.Zero.CompareTo(s_newUInt160);
            s_newUInt160.CompareTo(UInt160.Zero);
        }

        [Benchmark]
        public void TestEquals()
        {
            UInt160.Zero.Equals(UInt160.Zero);
            UInt160.Zero.Equals(s_newUInt160);
            s_newUInt160.Equals(null);
        }

        [Benchmark]
        public void TestParse()
        {
            _ = UInt160.Parse("0x0000000000000000000000000000000000000000");
            _ = UInt160.Parse("0000000000000000000000000000000000000000");
        }

        [Benchmark]
        public void TestTryParse()
        {
            UInt160.TryParse(null, out _);
            UInt160.TryParse("0x0000000000000000000000000000000000000000", out var temp);
            UInt160.TryParse("0x1230000000000000000000000000000000000000", out temp);
            UInt160.TryParse("000000000000000000000000000000000000000", out _);
        }

        [Benchmark]
        public void TestOperatorLarger()
        {
            _ = s_newUInt160 > UInt160.Zero;
        }

        [Benchmark]
        public void TestOperatorLargerAndEqual()
        {
            _ = s_newUInt160 >= UInt160.Zero;
        }

        [Benchmark]
        public void TestOperatorSmaller()
        {
            _ = s_newUInt160 < UInt160.Zero;
        }

        [Benchmark]
        public void TestOperatorSmallerAndEqual()
        {
            _ = s_newUInt160 <= UInt160.Zero;
        }
    }
}
