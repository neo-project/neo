// Copyright (C) 2015-2024 The Neo Project.
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
        private NewByteArrayCompare comparer;
        private OldByteArrayComparer _oldComparer;
        private byte[] x, y;

        [GlobalSetup]
        public void Setup()
        {
            comparer = NewByteArrayCompare.Default;
            _oldComparer = OldByteArrayComparer.Default;
        }

        [GlobalSetup(Target = nameof(NewCompare_50Bytes))]
        public void SetupNew50Bytes()
        {
            comparer = NewByteArrayCompare.Default;

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

        [GlobalSetup(Target = nameof(OldCompare_50Bytes))]
        public void SetupOld50Bytes()
        {
            _oldComparer = OldByteArrayComparer.Default;

            x = new byte[50]; // 50 bytes
            y = new byte[50]; // 50 bytes
            Array.Fill(x, (byte)0xCC);
            Array.Copy(x, y, x.Length);
        }

        [Benchmark]
        public void OldCompare_50Bytes()
        {
            _oldComparer.Compare(x, y);
        }

        [GlobalSetup(Target = nameof(NewCompare_500Bytes))]
        public void SetupNew500Bytes()
        {
            comparer = NewByteArrayCompare.Default;

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

        [GlobalSetup(Target = nameof(OldCompare_500Bytes))]
        public void SetupOld500Bytes()
        {
            _oldComparer = OldByteArrayComparer.Default;

            x = new byte[500]; // 500 bytes
            y = new byte[500]; // 500 bytes
            Array.Fill(x, (byte)0xCC);
            Array.Copy(x, y, x.Length);
        }

        [Benchmark]
        public void OldCompare_500Bytes()
        {
            _oldComparer.Compare(x, y);
        }

        [GlobalSetup(Target = nameof(NewCompare_5000Bytes))]
        public void SetupNew5000Bytes()
        {
            comparer = NewByteArrayCompare.Default;

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

        [GlobalSetup(Target = nameof(OldCompare_5000Bytes))]
        public void SetupOld5000Bytes()
        {
            _oldComparer = OldByteArrayComparer.Default;

            x = new byte[5000]; // 5000 bytes
            y = new byte[5000]; // 5000 bytes
            Array.Fill(x, (byte)0xCC);
            Array.Copy(x, y, x.Length);
        }

        [Benchmark]
        public void OldCompare_5000Bytes()
        {
            _oldComparer.Compare(x, y);
        }

        [GlobalSetup(Target = nameof(NewCompare_50000Bytes))]
        public void SetupNew50000Bytes()
        {
            comparer = NewByteArrayCompare.Default;

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

        [GlobalSetup(Target = nameof(OldCompare_50000Bytes))]
        public void SetupOld50000Bytes()
        {
            _oldComparer = OldByteArrayComparer.Default;

            x = new byte[50000]; // 50000 bytes
            y = new byte[50000]; // 50000 bytes
            Array.Fill(x, (byte)0xCC);
            Array.Copy(x, y, x.Length);
        }

        [Benchmark]
        public void OldCompare_50000Bytes()
        {
            _oldComparer.Compare(x, y);
        }
    }
}
