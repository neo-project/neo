using Akka.TestKit;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P;
using Akka.Configuration;
using Neo.IO;
using System.Linq;
using System.Collections.Generic;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ProtocolHandlerMailbox  : TestKit
    {
        private static readonly Random TestRandom = new Random(1337); // use fixed seed for guaranteed determinism

        ProtocolHandlerMailbox uut;

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
            uut = new ProtocolHandlerMailbox(akkaSettings, config);
        }

        [TestMethod]
        public void ProtocolHandlerMailbox_Test_IsHighPriority()
        {
            ISerializable s = null;
            // test priority commands
            uut.IsHighPriority(Message.Create("consensus", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("filteradd", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("filterclear", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("verack", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("version", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("alert", s)).Should().Be(true);
            // any random command should not have priority
            uut.IsHighPriority(Message.Create("_", s)).Should().Be(false);
            // any random object (non Message) should not have priority
            object obj = null;
            uut.IsHighPriority(obj).Should().Be(false);
        }


        [TestMethod]
        public void ProtocolHandlerMailbox_Test_ShallDrop()
        {
            // using this for messages
            ISerializable s = null;
            // empty queue
            IEnumerable<object> queue = Enumerable.Empty<object>();

            // any random object (non Message) should be dropped
            object obj = null;
            uut.ShallDrop(obj, queue).Should().Be(true);

            // test drop for specific commands (empty queue)
            uut.ShallDrop(Message.Create("getaddr", s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create("getblocks", s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create("getdata", s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create("getheaders", s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create("version", s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create("mempool", s), queue).Should().Be(false);
            // any random command should not be dropped
            uut.ShallDrop(Message.Create("_", s), queue).Should().Be(false);
        }
    }
}
