// Copyright (C) 2015-2024 The Neo Project.
//
// UT_TaskSession.cs file belongs to the neo project and is free
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
using Xunit.Sdk;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_TaskSession
    {
        [TestMethod]
        public void CreateTest()
        {
            var ses = new TaskSession(new VersionPayload() { Capabilities = new NodeCapability[] { new FullNodeCapability(123) } });

            Assert.IsFalse(ses.HasTooManyTasks);
            Assert.AreEqual((uint)123, ses.LastBlockIndex);
            Assert.AreEqual(0, ses.IndexTasks.Count);
            Assert.IsTrue(ses.IsFullNode);

            ses = new TaskSession(new VersionPayload() { Capabilities = Array.Empty<NodeCapability>() });

            Assert.IsFalse(ses.HasTooManyTasks);
            Assert.AreEqual((uint)0, ses.LastBlockIndex);
            Assert.AreEqual(0, ses.IndexTasks.Count);
            Assert.IsFalse(ses.IsFullNode);
        }
    }
}
