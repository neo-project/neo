// Copyright (C) 2015-2026 The Neo Project.
//
// UT_NeoSystemExtensions.cs file belongs to the neo project and is free
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
    public class UT_NeoSystemExtensions
    {
        [TestMethod]
        public void Snapshot_Helpers_MatchProtocolSettings_BeforeEchidnaPolicyOverride()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = TestProtocolSettings.Default;

            Assert.AreEqual(
                TimeSpan.FromMilliseconds(settings.MillisecondsPerBlock),
                snapshot.GetTimePerBlock(settings));
            Assert.AreEqual(settings.MaxValidUntilBlockIncrement, snapshot.GetMaxValidUntilBlockIncrement(settings));
            Assert.AreEqual(settings.MaxTraceableBlocks, snapshot.GetMaxTraceableBlocks(settings));
        }

        [TestMethod]
        public void System_Helpers_MatchSnapshotHelpers()
        {
            var system = TestBlockchain.GetSystem();
            var settings = system.Settings;
            var snapshot = system.StoreView;

            Assert.AreEqual(snapshot.GetTimePerBlock(settings), system.GetTimePerBlock());
            Assert.AreEqual(snapshot.GetMaxValidUntilBlockIncrement(settings), system.GetMaxValidUntilBlockIncrement());
            Assert.AreEqual(snapshot.GetMaxTraceableBlocks(settings), system.GetMaxTraceableBlocks());
        }
    }
}
