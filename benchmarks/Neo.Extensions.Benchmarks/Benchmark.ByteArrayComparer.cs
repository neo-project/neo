// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmark.ByteArrayComparer.cs file belongs to the neo project and is free
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
    public class Benchmark_ByteArrayComparer
    {
        private ByteArrayComparer comparer = ByteArrayComparer.Default;

        private byte[]? x, y;

        [GlobalSetup]
        public void Setup()
        {
            comparer = ByteArrayComparer.Default;
        }

        [GlobalSetup(Target = nameof(NewCompare_50Bytes))]
        public void SetupNew50Bytes()
        {
            comparer = ByteArrayComparer.Default;

            x = new byte[50]; // 50 bytes
            y = new byte[50]; // 50 bytes
            Array.Fill(x, (byte)0xCC);
            Array.Copy(x, y, x.Length);
        }

        [Benchmark]
        public void NewCompare_50Bytes()
        {
            comparer.Compare(x, y);
        }

        [GlobalSetup(Target = nameof(NewCompare_500Bytes))]
        public void SetupNew500Bytes()
        {
            comparer = ByteArrayComparer.Default;

            x = new byte[500]; // 500 bytes
            y = new byte[500]; // 500 bytes
            Array.Fill(x, (byte)0xCC);
            Array.Copy(x, y, x.Length);
        }

        [Benchmark]
        public void NewCompare_500Bytes()
        {
            comparer.Compare(x, y);
        }

        [GlobalSetup(Target = nameof(NewCompare_5000Bytes))]
        public void SetupNew5000Bytes()
        {
            comparer = ByteArrayComparer.Default;

            x = new byte[5000]; // 5000 bytes
            y = new byte[5000]; // 5000 bytes
            Array.Fill(x, (byte)0xCC);
            Array.Copy(x, y, x.Length);
        }

        [Benchmark]
        public void NewCompare_5000Bytes()
        {
            comparer.Compare(x, y);
        }

        [GlobalSetup(Target = nameof(NewCompare_50000Bytes))]
        public void SetupNew50000Bytes()
        {
            comparer = ByteArrayComparer.Default;

            x = new byte[50000]; // 50000 bytes
            y = new byte[50000]; // 50000 bytes
            Array.Fill(x, (byte)0xCC);
            Array.Copy(x, y, x.Length);
        }

        [Benchmark]
        public void NewCompare_50000Bytes()
        {
            comparer.Compare(x, y);
        }
    }
}
