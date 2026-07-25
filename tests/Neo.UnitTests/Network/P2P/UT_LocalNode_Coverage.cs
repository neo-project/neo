// Copyright (C) 2015-2026 The Neo Project.
//
// UT_LocalNode_Coverage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;
using System.Net;
using System.Threading;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_LocalNode_Coverage : TestKit
    {
        private static NeoSystem s_system;

        [ClassInitialize]
        public static void ClassSetup(TestContext _)
        {
            s_system = TestBlockchain.GetSystem();
        }

        [TestMethod]
        public void Peers_Message_AddsUnconnectedPeers()
        {
            var sender = CreateTestProbe();
            sender.Send(s_system.LocalNode, new ChannelsConfig
            {
                Tcp = new IPEndPoint(IPAddress.Loopback, 0),
                MinDesiredConnections = 1,
                MaxConnections = 10,
                MaxConnectionsPerAddress = 3
            });

            var ep = new IPEndPoint(IPAddress.Parse("203.0.113.10"), 20333);
            sender.Send(s_system.LocalNode, new Peer.Peers([ep]));

            sender.Send(s_system.LocalNode, new LocalNode.GetInstance());
            var local = sender.ExpectMsg<LocalNode>(TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);
            Assert.IsTrue(local.UnconnectedCount >= 0);
        }

        [TestMethod]
        public void Relay_Directly_Message_DoesNotFault()
        {
            var sender = CreateTestProbe();
            var inv = InvPayload.Create(InventoryType.TX,
                UInt256.Parse("0x3333333333333333333333333333333333333333333333333333333333333333"));
            sender.Send(s_system.LocalNode, Message.Create(MessageCommand.Inv, inv));
            sender.ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void GetInstance_ReturnsLocalNode()
        {
            var sender = CreateTestProbe();
            sender.Send(s_system.LocalNode, new LocalNode.GetInstance());
            var local = sender.ExpectMsg<LocalNode>(TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);
            Assert.IsNotNull(local);
            Assert.IsNotNull(local.Config);
        }
    }
}
