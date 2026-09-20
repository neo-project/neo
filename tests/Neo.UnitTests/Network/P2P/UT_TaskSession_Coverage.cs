// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TaskSession_Coverage.cs file belongs to the neo project and is free
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
    [TestClass]
    public class UT_TaskSession_Coverage
    {
        private static TaskSession FullNodeSession(uint height = 10)
        {
            return new TaskSession(new VersionPayload
            {
                Capabilities = [new FullNodeCapability(height)],
                UserAgent = "ut"
            });
        }

        [TestMethod]
        public void AvailableTasks_And_ReceivedBlockHashes_AreIndependentCollections()
        {
            var session = FullNodeSession();
            var hash = UInt256.Zero;
            session.AvailableTasks.Add(hash);
            Assert.IsTrue(session.AvailableTasks.Contains(hash));
            Assert.IsEmpty(session.ReceivedBlockHashes);
            Assert.IsEmpty(session.InvTasks);
        }

        [TestMethod]
        public void HasTooManyTasks_CountsInvAndIndexTogether()
        {
            var session = FullNodeSession();
            for (uint i = 0; i < 60; i++)
                session.IndexTasks[i] = DateTime.UtcNow;
            for (var i = 0; i < 40; i++)
            {
                var bytes = new byte[32];
                bytes[0] = (byte)i;
                bytes[1] = (byte)(i >> 8);
                session.InvTasks[new UInt256(bytes)] = DateTime.UtcNow;
            }
            Assert.IsTrue(session.HasTooManyTasks);
        }

        [TestMethod]
        public void LastBlockIndex_CanBeUpdated()
        {
            var session = FullNodeSession(5);
            Assert.AreEqual(5u, session.LastBlockIndex);
            session.LastBlockIndex = 99;
            Assert.AreEqual(99u, session.LastBlockIndex);
        }

        [TestMethod]
        public void NonFullNode_HasZeroLastBlockIndex()
        {
            var session = new TaskSession(new VersionPayload
            {
                Capabilities = [new ServerCapability(NodeCapabilityType.TcpServer, 10333)],
                UserAgent = "light"
            });
            Assert.IsFalse(session.IsFullNode);
            Assert.AreEqual(0u, session.LastBlockIndex);
        }

        [TestMethod]
        public void MempoolSent_DefaultsFalse_AndCanBeSet()
        {
            var session = FullNodeSession();
            Assert.IsFalse(session.MempoolSent);
            session.MempoolSent = true;
            Assert.IsTrue(session.MempoolSent);
        }

        [TestMethod]
        public void HasTooManyTasks_FalseWhenUnderLimit()
        {
            var session = FullNodeSession();
            for (uint i = 0; i < 50; i++)
                session.IndexTasks[i] = DateTime.UtcNow;
            Assert.IsFalse(session.HasTooManyTasks);
        }
    }
}
