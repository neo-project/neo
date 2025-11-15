// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ByteArrayComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Extensions.Tests;

[TestClass]
public class UT_ByteArrayComparer
{
    [TestMethod]
    public void TestCompare()
    {
        ByteArrayComparer comparer = ByteArrayComparer.Default;
        byte[]? x = null, y = null;
        Assert.AreEqual(0, comparer.Compare(x, y));

        x = [1, 2, 3, 4, 5];
        y = x;
        Assert.AreEqual(0, comparer.Compare(x, y));
        Assert.AreEqual(0, comparer.Compare(x, x));

        y = null;
        Assert.IsGreaterThan(0, comparer.Compare(x, y));

        y = x;
        x = null;
        Assert.IsLessThan(0, comparer.Compare(x, y));

        x = [1];
        y = [];
        Assert.IsGreaterThan(0, comparer.Compare(x, y));
        y = x;
        Assert.AreEqual(0, comparer.Compare(x, y));

        x = [1];
        y = [2];
        Assert.IsLessThan(0, comparer.Compare(x, y));

        Assert.AreEqual(0, comparer.Compare(null, Array.Empty<byte>()));
        Assert.AreEqual(0, comparer.Compare(Array.Empty<byte>(), null));

        x = [1, 2, 3, 4, 5];
        y = [1, 2, 3];
        Assert.IsGreaterThan(0, comparer.Compare(x, y));

        x = [1, 2, 3, 4, 5];
        y = [1, 2, 3, 4, 5, 6];
        Assert.IsLessThan(0, comparer.Compare(x, y));

        // cases for reverse comparer
        comparer = ByteArrayComparer.Reverse;

        x = [3];
        Assert.IsLessThan(0, comparer.Compare(x, y));

        y = x;
        Assert.AreEqual(0, comparer.Compare(x, y));

        x = [1];
        y = [2];
        Assert.IsGreaterThan(0, comparer.Compare(x, y));

        Assert.AreEqual(0, comparer.Compare(null, Array.Empty<byte>()));
        Assert.AreEqual(0, comparer.Compare(Array.Empty<byte>(), null));

        x = [1, 2, 3, 4, 5];
        y = [1, 2, 3];
        Assert.IsLessThan(0, comparer.Compare(x, y));

        x = [1, 2, 3, 4, 5];
        y = [1, 2, 3, 4, 5, 6];
        Assert.IsGreaterThan(0, comparer.Compare(x, y));
    }
}
