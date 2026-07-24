// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TrackState.cs file belongs to the neo project and is free
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
    public class UT_TrackState
    {
        [TestMethod]
        public void Values_MatchSpecification()
        {
            Assert.AreEqual(0, (byte)TrackState.None);
            Assert.AreEqual(1, (byte)TrackState.Added);
            Assert.AreEqual(2, (byte)TrackState.Changed);
            Assert.AreEqual(3, (byte)TrackState.Deleted);
            Assert.AreEqual(4, (byte)TrackState.NotFound);
        }

        [TestMethod]
        public void AllValues_AreDefined()
        {
            foreach (TrackState state in System.Enum.GetValues<TrackState>())
                Assert.IsTrue(System.Enum.IsDefined(state));
        }
    }
}
