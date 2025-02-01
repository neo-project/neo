// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DateTimeExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_DateTimeExtensions
    {
        private static readonly DateTime unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void TestToTimestamp()
        {
            var time = DateTime.Now;
            var expected = (uint)(time.ToUniversalTime() - unixEpoch).TotalSeconds;
            var actual = time.ToTimestamp();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestToTimestampMS()
        {
            var time = DateTime.Now;
            var expected = (ulong)(time.ToUniversalTime() - unixEpoch).TotalMilliseconds;
            var actual = time.ToTimestampMS();

            Assert.AreEqual(expected, actual);
        }
    }
}
