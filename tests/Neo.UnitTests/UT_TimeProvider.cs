// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TimeProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_TimeProvider
    {
        [TestCleanup]
        public void Cleanup()
        {
            TimeProvider.ResetToDefault();
        }

        [TestMethod]
        public void Current_Default_ReturnsUtcNow()
        {
            TimeProvider.ResetToDefault();
            var before = DateTime.UtcNow.AddSeconds(-1);
            var now = TimeProvider.Current.UtcNow;
            var after = DateTime.UtcNow.AddSeconds(1);
            Assert.IsTrue(now >= before && now <= after);
        }

        [TestMethod]
        public void Current_CanBeReplaced_AndReset()
        {
            var fixedTime = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            TimeProvider.Current = new FixedTimeProvider(fixedTime);
            Assert.AreEqual(fixedTime, TimeProvider.Current.UtcNow);

            TimeProvider.ResetToDefault();
            Assert.AreNotEqual(fixedTime, TimeProvider.Current.UtcNow);
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTime _utcNow;
            public FixedTimeProvider(DateTime utcNow) => _utcNow = utcNow;
            public override DateTime UtcNow => _utcNow;
        }
    }
}
