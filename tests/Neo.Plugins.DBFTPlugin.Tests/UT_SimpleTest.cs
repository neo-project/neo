// Copyright (C) 2015-2025 The Neo Project.
//
// UT_SimpleTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Tests.TestUtils;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.Threading;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public class UT_SimpleTest
    {
        [TestMethod]
        public void TestExtensiblePayload()
        {
            // Create a simple consensus message
            var message = new ChangeView
            {
                BlockIndex = 1,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Reason = ChangeViewReason.Timeout,
                Timestamp = 12345
            };

            // Create an extensible payload
            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                Data = message.ToArray(),
                ValidBlockStart = 0,
                ValidBlockEnd = 1000,
                Sender = UInt160.Zero
            };

            // Verify the payload properties
            Assert.AreEqual("dBFT", payload.Category);
            Assert.IsTrue(payload.Data.Length > 0);
            Assert.AreEqual(0u, payload.ValidBlockStart);
            Assert.AreEqual(1000u, payload.ValidBlockEnd);
            Assert.AreEqual(UInt160.Zero, payload.Sender);
        }

        [TestMethod]
        [Timeout(5000)] // 5 second timeout
        public void TestMinimalWithTimeout()
        {
            // This test should complete quickly (under 1 second)
            // It verifies that our timeout handling works correctly

            bool testCompleted = false;

            TestUtilities.ExecuteWithTimeout(() =>
            {
                // Do some work that finishes quickly
                Thread.Sleep(100);
                testCompleted = true;
                return true;
            }, TimeSpan.FromSeconds(1));

            Assert.IsTrue(testCompleted, "Test was not able to complete");
        }

        [TestMethod]
        [Timeout(5000)] // 5 second timeout
        public void TestMinimalWithTimeProvider()
        {
            // Reset mock time provider
            MockConsensusComponents.ResetTime();

            // Get current time
            var initialTime = TimeProvider.Current.UtcNow;

            // Advance time
            MockConsensusComponents.AdvanceTime(1);

            // Verify time was advanced
            var newTime = TimeProvider.Current.UtcNow;
            Assert.IsTrue(newTime > initialTime, "Time was not advanced");

            // Reset time again
            MockConsensusComponents.ResetTime();

            // Verify time was reset (should be close to actual time)
            var resetTime = TimeProvider.Current.UtcNow;
            Assert.IsTrue((DateTime.UtcNow - resetTime).TotalSeconds < 1,
                "Time was not properly reset");
        }
    }
}
