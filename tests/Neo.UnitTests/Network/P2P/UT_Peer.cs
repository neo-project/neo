// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Peer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using System;
using System.Net;
using System.Reflection;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_Peer : TestKit
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void PeerRequestsMorePeersWhenBelowDesiredConnections()
        {
            var observer = CreateTestProbe();
            var peerRef = ActorOfAsTestActorRef(() => new TestPeerActor(observer));

            peerRef.Tell(new ChannelsConfig
            {
                MinDesiredConnections = 2,
                MaxConnections = -1
            });

            SendTimer(peerRef);

            observer.ExpectMsg<int>(count => count == 2, cancellationToken: TestContext.CancellationTokenSource.Token);
        }

        private static void SendTimer(IActorRef peer)
        {
            var timerType = typeof(Peer).GetNestedType("Timer", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(timerType, "Peer.Timer type not found via reflection.");
            var timerMessage = Activator.CreateInstance(timerType!);
            peer.Tell(timerMessage!);
        }

        private sealed class TestPeerActor : Peer
        {
            private readonly IActorRef _observer;

            public TestPeerActor(IActorRef observer)
            {
                _observer = observer;
            }

            protected override void NeedMorePeers(int count)
            {
                _observer.Tell(count);
            }

            protected override Props ProtocolProps(object connection, IPEndPoint remote, IPEndPoint local)
            {
                return Props.Empty;
            }
        }
    }
}
