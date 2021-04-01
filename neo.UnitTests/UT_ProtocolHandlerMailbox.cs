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
    public class UT_ProtocolHandlerMailbox : TestKit
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

            // known commands
            uut.IsHighPriority(Message.Create("addr", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("block", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("consensus", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("filteradd", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("filterclear", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("filterload", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("getaddr", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("getblocks", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("getdata", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("getheaders", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("headers", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("inv", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("mempool", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("ping", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("pong", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("tx", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("verack", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("version", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("alert", s)).Should().Be(true);
            uut.IsHighPriority(Message.Create("merkleblock", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("notfound", s)).Should().Be(false);
            uut.IsHighPriority(Message.Create("reject", s)).Should().Be(false);

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
            Message msg = null; // multiple uses
            // empty queue
            IEnumerable<object> emptyQueue = Enumerable.Empty<object>();

            // any random object (non Message) should be dropped
            object obj = null;
            uut.ShallDrop(obj, emptyQueue).Should().Be(true);

            //handshaking
            // Version (no drop)
            msg = Message.Create("version", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Verack (no drop)
            msg = Message.Create("verack", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);

            //connectivity
            // GetAddr (drop)
            msg = Message.Create("getaddr", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(true);
            // Addr (no drop)
            msg = Message.Create("addr", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Ping (no drop)
            msg = Message.Create("ping", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Pong (no drop)
            msg = Message.Create("pong", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);

            //synchronization
            // GetHeaders (drop)
            msg = Message.Create("getheaders", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(true);
            // Headers (no drop)
            msg = Message.Create("headers", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // GetBlocks (drop)
            msg = Message.Create("getblocks", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(true);
            // Mempool (drop)
            msg = Message.Create("mempool", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(true);
            // Inv (no drop)
            msg = Message.Create("inv", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // GetData (drop)
            msg = Message.Create("getdata", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(true);
            // NotFound (no drop)
            msg = Message.Create("notfound", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Transaction (no drop)
            msg = Message.Create("tx", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Block (no drop)
            msg = Message.Create("block", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Consensus (no drop)
            msg = Message.Create("consensus", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Reject (no drop)
            msg = Message.Create("reject", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);

            //SPV protocol
            // FilterLoad (no drop)
            msg = Message.Create("filterload", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // FilterAdd (no drop)
            msg = Message.Create("filteradd", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // FilterClear (no drop)
            msg = Message.Create("filterclear", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // MerkleBlock (no drop)
            msg = Message.Create("merkleblock", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);

            //others
            // Alert (no drop)
            msg = Message.Create("alert", s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);

            // any random command should not be dropped
            uut.ShallDrop(Message.Create("_", s), emptyQueue).Should().Be(false);
        }
    }
}
