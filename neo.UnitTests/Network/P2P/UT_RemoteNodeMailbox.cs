using Akka.IO;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using System;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    [NotReRunnable]
    public class UT_RemoteNodeMailbox : TestKit
    {
        private static readonly Random TestRandom = new Random(1337); // use fixed seed for guaranteed determinism

        RemoteNodeMailbox uut;

        [TestCleanup]
        public void Cleanup()
        {
            Shutdown();
        }

        [TestInitialize]
        public void TestSetup()
        {
            Akka.Actor.ActorSystem system = Sys;
            var config = TestKit.DefaultConfig;
            var akkaSettings = new Akka.Actor.Settings(system, config);
            uut = new RemoteNodeMailbox(akkaSettings, config);
        }

        [TestMethod]
        public void RemoteNode_Test_IsHighPriority()
        {
            // high priority commands
            uut.IsHighPriority(new Tcp.ConnectionClosed()).Should().Be(true);
            uut.IsHighPriority(new Connection.Timer()).Should().Be(true);
            uut.IsHighPriority(new Connection.Ack()).Should().Be(true);

            // any random object should not have priority
            object obj = null;
            uut.IsHighPriority(obj).Should().Be(false);
        }
    }
}
