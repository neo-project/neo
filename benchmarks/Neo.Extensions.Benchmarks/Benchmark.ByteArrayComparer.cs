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
        private ByteArrayComparer comparer = ByteArrayComparer.Default;
        private OldByteArrayComparer _oldComparer = OldByteArrayComparer.Default;
        private byte[]? x, y;

        [Benchmark]
        public void NewCompare_NullArrays()
        {
            x = null;
            y = null;
            comparer.Compare(x, y);
        }

        [Benchmark]
        public void OldCompare_NullArrays()
        {
            x = null;
            y = null;
            _oldComparer.Compare(x, y);
        }

        [Benchmark]
        public void NewCompare_SameArray()
        {
            x = new byte[] { 1, 2, 3, 4, 5 };
            y = x;
            comparer.Compare(x, y);
            comparer.Compare(x, x);
        }

        [Benchmark]
        public void OldCompare_SameArray()
        {
            x = new byte[] { 1, 2, 3, 4, 5 };
            y = x;
            _oldComparer.Compare(x, y);
            _oldComparer.Compare(x, x);
        }

        [Benchmark]
        public void NewCompare_NonNullAndNull()
        {
            x = new byte[] { 1, 2, 3, 4, 5 };
            y = null;
            comparer.Compare(x, y);
        }

        [Benchmark]
        public void OldCompare_NonNullAndNull()
        {
            x = new byte[] { 1, 2, 3, 4, 5 };
            y = null;
            _oldComparer.Compare(x, y);
        }

        [Benchmark]
        public void NewCompare_NullAndNonNull()
        {
            y = new byte[] { 1, 2, 3, 4, 5 };
            x = null;
            comparer.Compare(x, y);
        }

        [Benchmark]
        public void OldCompare_NullAndNonNull()
        {
            y = new byte[] { 1, 2, 3, 4, 5 };
            x = null;
            _oldComparer.Compare(x, y);
        }

        [Benchmark]
        public void NewCompare_EmptyArray()
        {
            x = new byte[] { 1 };
            y = Array.Empty<byte>();
            comparer.Compare(x, y);
            y = x;
            comparer.Compare(x, y);
        }

        [Benchmark]
        public void OldCompare_EmptyArray()
        {
            x = new byte[] { 1 };
            y = Array.Empty<byte>();
            _oldComparer.Compare(x, y);
            y = x;
            _oldComparer.Compare(x, y);
        }

        [Benchmark]
        public void NewCompare_SingleElementArrays()
        {
            x = new byte[] { 1 };
            y = new byte[] { 2 };
            comparer.Compare(x, y);
        }

        [Benchmark]
        public void OldCompare_SingleElementArrays()
        {
            x = new byte[] { 1 };
            y = new byte[] { 2 };
            _oldComparer.Compare(x, y);
        }

        [Benchmark]
        public void NewCompare_NullAndEmptyArray()
        {
            comparer.Compare(null, Array.Empty<byte>());
            comparer.Compare(Array.Empty<byte>(), null);
        }

        [Benchmark]
        public void OldCompare_NullAndEmptyArray()
        {
            _oldComparer.Compare(null, Array.Empty<byte>());
            _oldComparer.Compare(Array.Empty<byte>(), null);
        }

        [Benchmark]
        public void NewCompare_DifferentLengthArrays()
        {
            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3 };
            comparer.Compare(x, y);

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3, 4, 5, 6 };
            comparer.Compare(x, y);
        }

        [Benchmark]
        public void OldCompare_DifferentLengthArrays()
        {
            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3 };
            _oldComparer.Compare(x, y);

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3, 4, 5, 6 };
            _oldComparer.Compare(x, y);
        }

        [GlobalSetup(Target = nameof(NewCompare_ReverseComparer))]
        public void SetupReverseComparer()
        {
            comparer = ByteArrayComparer.Reverse;
        }

        [GlobalSetup(Target = nameof(OldCompare_ReverseComparer))]
        public void SetupTestReverseComparer()
        {
            _oldComparer = OldByteArrayComparer.Reverse;
        }

        [Benchmark]
        public void NewCompare_ReverseComparer()
        {
            x = new byte[] { 3 };
            y = new byte[] { 1, 2, 3, 4, 5, 6 };
            comparer.Compare(x, y);

            y = x;
            comparer.Compare(x, y);

            x = new byte[] { 1 };
            y = new byte[] { 2 };
            comparer.Compare(x, y);

            comparer.Compare(null, Array.Empty<byte>());
            comparer.Compare(Array.Empty<byte>(), null);

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3 };
            comparer.Compare(x, y);

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3, 4, 5, 6 };
            comparer.Compare(x, y);
        }

        [Benchmark]
        public void OldCompare_ReverseComparer()
        {
            x = new byte[] { 3 };
            y = new byte[] { 1, 2, 3, 4, 5, 6 };
            _oldComparer.Compare(x, y);

            y = x;
            _oldComparer.Compare(x, y);

            x = new byte[] { 1 };
            y = new byte[] { 2 };
            _oldComparer.Compare(x, y);

            _oldComparer.Compare(null, Array.Empty<byte>());
            _oldComparer.Compare(Array.Empty<byte>(), null);

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3 };
            _oldComparer.Compare(x, y);

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3, 4, 5, 6 };
            _oldComparer.Compare(x, y);
        }
    }
}
