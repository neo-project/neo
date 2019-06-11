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
            uut.IsHighPriority(Message.Create(MessageCommand.Consensus, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.FilterAdd, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.FilterClear, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.FilterLoad, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.Verack, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.Version, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.Alert, s)).Should().Be(true);
            // any random command should not have priority
            uut.IsHighPriority(Message.Create((MessageCommand)'0xff', s)).Should().Be(false);
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
            uut.ShallDrop(Message.Create(MessageCommand.GetAddr, s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create(MessageCommand.GetBlocks, s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create(MessageCommand.GetData, s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create(MessageCommand.GetHeaders, s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create(MessageCommand.Version, s), queue).Should().Be(false);
            uut.ShallDrop(Message.Create(MessageCommand.Mempool, s), queue).Should().Be(false);
            // any random command should not be dropped
            uut.ShallDrop(Message.Create((MessageCommand)'0xff', s), queue).Should().Be(false);
        }
    }
}
