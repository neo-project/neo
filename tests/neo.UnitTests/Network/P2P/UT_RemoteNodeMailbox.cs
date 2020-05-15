using Akka.IO;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
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
            ISerializable s = null;

            //handshaking
            uut.IsHighPriority(Message.Create(MessageCommand.Version, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.Verack, s)).Should().Be(true);

            //connectivity
            uut.IsHighPriority(Message.Create(MessageCommand.GetAddr, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.Addr, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.Ping, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.Pong, s)).Should().Be(false);

            //synchronization
            uut.IsHighPriority(Message.Create(MessageCommand.GetHeaders, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.Headers, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.GetBlocks, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.Mempool, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.Inv, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.GetData, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.NotFound, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.Transaction, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.Block, s)).Should().Be(false);
            uut.IsHighPriority(Message.Create(MessageCommand.Consensus, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.Reject, s)).Should().Be(false);

            //SPV protocol
            uut.IsHighPriority(Message.Create(MessageCommand.FilterLoad, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.FilterAdd, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.FilterClear, s)).Should().Be(true);
            uut.IsHighPriority(Message.Create(MessageCommand.MerkleBlock, s)).Should().Be(false);

            //others
            uut.IsHighPriority(Message.Create(MessageCommand.Alert, s)).Should().Be(true);

            // high priority commands
            uut.IsHighPriority(new Tcp.ConnectionClosed()).Should().Be(true);
            uut.IsHighPriority(new Connection.Close()).Should().Be(true);
            uut.IsHighPriority(new Connection.Ack()).Should().Be(true);

            // any random object should not have priority
            object obj = null;
            uut.IsHighPriority(obj).Should().Be(false);
        }

        public void ProtocolHandlerMailbox_Test_ShallDrop()
        {
            // using this for messages
            ISerializable s = null;
            Message msg; // multiple uses
            // empty queue
            IEnumerable<object> emptyQueue = Enumerable.Empty<object>();

            // any random object (non Message) should be dropped
            object obj = null;
            uut.ShallDrop(obj, emptyQueue).Should().Be(true);

            //handshaking
            // Version (no drop)
            msg = Message.Create(MessageCommand.Version, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Verack (no drop)
            msg = Message.Create(MessageCommand.Verack, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);

            //connectivity
            // GetAddr (drop)
            msg = Message.Create(MessageCommand.GetAddr, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(true);
            // Addr (no drop)
            msg = Message.Create(MessageCommand.Addr, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Ping (no drop)
            msg = Message.Create(MessageCommand.Ping, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Pong (no drop)
            msg = Message.Create(MessageCommand.Pong, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);

            //synchronization
            // GetHeaders (drop)
            msg = Message.Create(MessageCommand.GetHeaders, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(true);
            // Headers (no drop)
            msg = Message.Create(MessageCommand.Headers, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // GetBlocks (drop)
            msg = Message.Create(MessageCommand.GetBlocks, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(true);
            // Mempool (drop)
            msg = Message.Create(MessageCommand.Mempool, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(true);
            // Inv (no drop)
            msg = Message.Create(MessageCommand.Inv, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // NotFound (no drop)
            msg = Message.Create(MessageCommand.NotFound, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Transaction (no drop)
            msg = Message.Create(MessageCommand.Transaction, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Block (no drop)
            msg = Message.Create(MessageCommand.Block, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Consensus (no drop)
            msg = Message.Create(MessageCommand.Consensus, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // Reject (no drop)
            msg = Message.Create(MessageCommand.Reject, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);

            //SPV protocol
            // FilterLoad (no drop)
            msg = Message.Create(MessageCommand.FilterLoad, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // FilterAdd (no drop)
            msg = Message.Create(MessageCommand.FilterAdd, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // FilterClear (no drop)
            msg = Message.Create(MessageCommand.FilterClear, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
            // MerkleBlock (no drop)
            msg = Message.Create(MessageCommand.MerkleBlock, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);

            //others
            // Alert (no drop)
            msg = Message.Create(MessageCommand.Alert, s);
            uut.ShallDrop(msg, emptyQueue).Should().Be(false);
            uut.ShallDrop(msg, new object[] { msg }).Should().Be(false);
        }
    }
}
