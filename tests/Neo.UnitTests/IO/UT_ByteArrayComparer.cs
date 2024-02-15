// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ByteArrayComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using System;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_ByteArrayComparer
    {
        [TestMethod]
        public void TestCompare()
        {
            ByteArrayComparer comparer = ByteArrayComparer.Default;
            byte[] x = null, y = null;
            comparer.Compare(x, y).Should().Be(0);

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = x;
            comparer.Compare(x, y).Should().Be(0);
            comparer.Compare(x, x).Should().Be(0);

            y = null;
            comparer.Compare(x, y).Should().Be(5);

            y = x;
            x = null;
            comparer.Compare(x, y).Should().Be(-5);

            x = new byte[] { 1 };
            y = Array.Empty<byte>();
            comparer.Compare(x, y).Should().Be(1);
            y = x;
            comparer.Compare(x, y).Should().Be(0);

            x = new byte[] { 1 };
            y = new byte[] { 2 };
            comparer.Compare(x, y).Should().Be(-1);

            comparer = ByteArrayComparer.Reverse;
            x = new byte[] { 3 };
            comparer.Compare(x, y).Should().Be(-1);
            y = x;
            comparer.Compare(x, y).Should().Be(0);

            x = new byte[] { 1 };
            y = new byte[] { 2 };
            comparer.Compare(x, y).Should().Be(1);
        }
    }
}
