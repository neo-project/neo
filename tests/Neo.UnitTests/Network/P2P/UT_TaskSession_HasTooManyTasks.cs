// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TaskSession_HasTooManyTasks.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Network.P2P
{
    /// <summary>
    /// HasTooManyTasks threshold edges not covered by UT_TaskSession.CreateTest.
    /// </summary>
    [TestClass]
    public class UT_TaskSession_HasTooManyTasks
    {
        private static TaskSession NewSession()
        {
            return new TaskSession(new VersionPayload
            {
                Capabilities = [new FullNodeCapability(1)],
                UserAgent = ""
            });
        }

        [TestMethod]
        public void HasTooManyTasks_IsFalse_BelowThreshold()
        {
            var session = NewSession();
            for (uint i = 0; i < 99; i++)
                session.IndexTasks[i] = DateTime.UtcNow;
            Assert.IsFalse(session.HasTooManyTasks);
        }

        [TestMethod]
        public void HasTooManyTasks_IsTrue_AtThreshold()
        {
            var session = NewSession();
            for (uint i = 0; i < 100; i++)
                session.IndexTasks[i] = DateTime.UtcNow;
            Assert.IsTrue(session.HasTooManyTasks);
        }

        [TestMethod]
        public void MempoolSent_DefaultsFalse()
        {
            var session = NewSession();
            Assert.IsFalse(session.MempoolSent);
            session.MempoolSent = true;
            Assert.IsTrue(session.MempoolSent);
        }
    }
}
