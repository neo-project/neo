// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SeekDirection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_SeekDirection
    {
        [TestMethod]
        public void Values_MatchSpecification()
        {
            Assert.AreEqual(1, (sbyte)SeekDirection.Forward);
            Assert.AreEqual(-1, (sbyte)SeekDirection.Backward);
        }

        [TestMethod]
        public void AllValues_AreDefined()
        {
            foreach (SeekDirection direction in System.Enum.GetValues<SeekDirection>())
                Assert.IsTrue(System.Enum.IsDefined(direction));
        }
    }
}
