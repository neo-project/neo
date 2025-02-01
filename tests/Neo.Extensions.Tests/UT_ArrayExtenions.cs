// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ArrayExtenions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Linq;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_ArrayExtenions
    {
        [TestMethod]
        public void TestRepeat()
        {
            var items = ((byte)0xff).Repeat(10);
            Assert.IsTrue(items.SequenceEqual(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }));

            items = ((byte)0xff).Repeat(0);
            Assert.IsTrue(items.SequenceEqual(new byte[] { }));
        }
    }
}
