// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ByteArrayComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_ByteArrayComparer
    {
        [TestMethod]
        public void TestCompare()
        {
            ByteArrayComparer comparer = ByteArrayComparer.Default;
            byte[]? x = null, y = null;
            Assert.AreEqual(0, comparer.Compare(x, y));

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = x;
            Assert.AreEqual(0, comparer.Compare(x, y));
            Assert.AreEqual(0, comparer.Compare(x, x));

            y = null;
            Assert.IsTrue(comparer.Compare(x, y) > 0);

            y = x;
            x = null;
            Assert.IsTrue(comparer.Compare(x, y) < 0);

            x = new byte[] { 1 };
            y = Array.Empty<byte>();
            Assert.IsTrue(comparer.Compare(x, y) > 0);
            y = x;
            Assert.AreEqual(0, comparer.Compare(x, y));

            x = new byte[] { 1 };
            y = new byte[] { 2 };
            Assert.IsTrue(comparer.Compare(x, y) < 0);

            Assert.AreEqual(0, comparer.Compare(null, Array.Empty<byte>()));
            Assert.AreEqual(0, comparer.Compare(Array.Empty<byte>(), null));

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3 };
            Assert.IsTrue(comparer.Compare(x, y) > 0);

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3, 4, 5, 6 };
            Assert.IsTrue(comparer.Compare(x, y) < 0);

            // cases for reverse comparer
            comparer = ByteArrayComparer.Reverse;

            x = new byte[] { 3 };
            Assert.IsTrue(comparer.Compare(x, y) < 0);

            y = x;
            Assert.AreEqual(0, comparer.Compare(x, y));

            x = new byte[] { 1 };
            y = new byte[] { 2 };
            Assert.IsTrue(comparer.Compare(x, y) > 0);

            Assert.AreEqual(0, comparer.Compare(null, Array.Empty<byte>()));
            Assert.AreEqual(0, comparer.Compare(Array.Empty<byte>(), null));

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3 };
            Assert.IsTrue(comparer.Compare(x, y) < 0);

            x = new byte[] { 1, 2, 3, 4, 5 };
            y = new byte[] { 1, 2, 3, 4, 5, 6 };
            Assert.IsTrue(comparer.Compare(x, y) > 0);
        }
    }
}
