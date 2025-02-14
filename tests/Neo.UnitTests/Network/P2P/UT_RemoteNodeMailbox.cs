// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RemoteNodeMailbox.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_RemoteNodeMailbox
    {
        RemoteNodeMailbox uut;

        [TestCleanup]
        public void Cleanup()
        {

        }

        [TestInitialize]
        public void TestSetup()
        {
            ActorSystem system = TestBlockchain.TheNeoSystem.ActorSystem;
            uut = new RemoteNodeMailbox(system.Settings, NeoSystem.SystemConfig);
        }

        [TestMethod]
        public void RemoteNode_Test_IsHighPriority()
        {
            ISerializable s = null;

            //handshaking
            Assert.IsTrue(uut.IsHighPriority(Message.Create(MessageCommand.Version, s)));
            Assert.IsTrue(uut.IsHighPriority(Message.Create(MessageCommand.Verack, s)));

            //connectivity
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.GetAddr, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.Addr, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.Ping, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.Pong, s)));

            //synchronization
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.GetHeaders, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.Headers, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.GetBlocks, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.Mempool, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.Inv, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.GetData, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.NotFound, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.Transaction, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.Block, s)));
            Assert.IsTrue(uut.IsHighPriority(Message.Create(MessageCommand.Extensible, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.Reject, s)));

            //SPV protocol
            Assert.IsTrue(uut.IsHighPriority(Message.Create(MessageCommand.FilterLoad, s)));
            Assert.IsTrue(uut.IsHighPriority(Message.Create(MessageCommand.FilterAdd, s)));
            Assert.IsTrue(uut.IsHighPriority(Message.Create(MessageCommand.FilterClear, s)));
            Assert.IsFalse(uut.IsHighPriority(Message.Create(MessageCommand.MerkleBlock, s)));

            //others
            Assert.IsTrue(uut.IsHighPriority(Message.Create(MessageCommand.Alert, s)));

            // high priority commands
            Assert.IsTrue(uut.IsHighPriority(new Tcp.ConnectionClosed()));
            Assert.IsTrue(uut.IsHighPriority(new Connection.Close()));
            Assert.IsTrue(uut.IsHighPriority(new Connection.Ack()));

            // any random object should not have priority
            object obj = null;
            Assert.IsFalse(uut.IsHighPriority(obj));
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
            Assert.IsTrue(uut.ShallDrop(obj, emptyQueue));

            //handshaking
            // Version (no drop)
            msg = Message.Create(MessageCommand.Version, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // Verack (no drop)
            msg = Message.Create(MessageCommand.Verack, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));

            //connectivity
            // GetAddr (drop)
            msg = Message.Create(MessageCommand.GetAddr, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsTrue(uut.ShallDrop(msg, new object[] { msg }));
            // Addr (no drop)
            msg = Message.Create(MessageCommand.Addr, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // Ping (no drop)
            msg = Message.Create(MessageCommand.Ping, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // Pong (no drop)
            msg = Message.Create(MessageCommand.Pong, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));

            //synchronization
            // GetHeaders (drop)
            msg = Message.Create(MessageCommand.GetHeaders, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsTrue(uut.ShallDrop(msg, new object[] { msg }));
            // Headers (no drop)
            msg = Message.Create(MessageCommand.Headers, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // GetBlocks (drop)
            msg = Message.Create(MessageCommand.GetBlocks, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsTrue(uut.ShallDrop(msg, new object[] { msg }));
            // Mempool (drop)
            msg = Message.Create(MessageCommand.Mempool, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsTrue(uut.ShallDrop(msg, new object[] { msg }));
            // Inv (no drop)
            msg = Message.Create(MessageCommand.Inv, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // NotFound (no drop)
            msg = Message.Create(MessageCommand.NotFound, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // Transaction (no drop)
            msg = Message.Create(MessageCommand.Transaction, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // Block (no drop)
            msg = Message.Create(MessageCommand.Block, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // Consensus (no drop)
            msg = Message.Create(MessageCommand.Extensible, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // Reject (no drop)
            msg = Message.Create(MessageCommand.Reject, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));

            //SPV protocol
            // FilterLoad (no drop)
            msg = Message.Create(MessageCommand.FilterLoad, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // FilterAdd (no drop)
            msg = Message.Create(MessageCommand.FilterAdd, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // FilterClear (no drop)
            msg = Message.Create(MessageCommand.FilterClear, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
            // MerkleBlock (no drop)
            msg = Message.Create(MessageCommand.MerkleBlock, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));

            //others
            // Alert (no drop)
            msg = Message.Create(MessageCommand.Alert, s);
            Assert.IsFalse(uut.ShallDrop(msg, emptyQueue));
            Assert.IsFalse(uut.ShallDrop(msg, new object[] { msg }));
        }
    }
}
