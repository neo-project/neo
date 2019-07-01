using Akka.Actor;
using Akka.IO;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_RemoteNode : TestKit
    {
        private static readonly Random TestRandom = new Random(1337); // use fixed seed for guaranteed determinism

        private static NeoSystem testBlockchain;


        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            testBlockchain = TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void RemoteNode_Test_Abort_DifferentMagic()
        {
            var testProbe = CreateTestProbe();
            var connectionTestProbe = CreateTestProbe();
            //var protocolActor = ActorOfAsTestActorRef<ProtocolHandler>(() => new ProtocolHandler(testBlockchain));
            var remoteNodeActor = ActorOfAsTestActorRef<RemoteNode>(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));

            connectionTestProbe.ExpectMsg<Tcp.Write>();

            var payload = new VersionPayload()
            {
                UserAgent = "".PadLeft(1024, '0'),
                Nonce = 1,
                Magic = 2,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            };

            testProbe.Send(remoteNodeActor, payload);

            connectionTestProbe.ExpectMsg<Tcp.Abort>();
        }

        [TestMethod]
        public void RemoteNode_Test_Accept_IfSameMagic()
        {
            var testProbe = CreateTestProbe();
            var connectionTestProbe = CreateTestProbe();
            //var protocolActor = ActorOfAsTestActorRef<ProtocolHandler>(() => new ProtocolHandler(testBlockchain));
            var remoteNodeActor = ActorOfAsTestActorRef<RemoteNode>(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));

            connectionTestProbe.ExpectMsg<Tcp.Write>();

            var payload = new VersionPayload()
            {
                UserAgent = "Unit Test".PadLeft(1024, '0'),
                Nonce = 1,
                Magic = ProtocolSettings.Default.Magic,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            };

            testProbe.Send(remoteNodeActor, payload);

            var verackMessage = connectionTestProbe.ExpectMsg<Tcp.Write>();

            //Verack
            verackMessage.Data.Count.Should().Be(3);
        }
    }
}
