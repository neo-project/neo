// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TaskManagerInventoryAdmission.cs file belongs to the neo project and is free
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
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_TaskManagerInventoryAdmission : TestKit
    {
        private static Dictionary<IActorRef, TaskSession> GetSessions(Akka.TestKit.TestActorRef<TaskManager> manager)
        {
            return (Dictionary<IActorRef, TaskSession>)typeof(TaskManager)
                .GetField("sessions", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(manager.UnderlyingActor)!;
        }

        private static Dictionary<UInt256, int> GetGlobalTasks(Akka.TestKit.TestActorRef<TaskManager> manager)
        {
            return (Dictionary<UInt256, int>)typeof(TaskManager)
                .GetField("globalInvTasks", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(manager.UnderlyingActor)!;
        }

        private static UInt256[] Hashes(int start, int count)
        {
            var hashes = new UInt256[count];
            for (var i = 0; i < count; i++)
            {
                var bytes = new byte[32];
                BitConverter.GetBytes(start + i).CopyTo(bytes, 0);
                hashes[i] = new UInt256(bytes);
            }
            return hashes;
        }

        private Akka.TestKit.TestProbe Register(Akka.TestKit.TestActorRef<TaskManager> manager, uint height)
        {
            var peer = CreateTestProbe();
            manager.Receive(new TaskManager.Register(new VersionPayload
            {
                UserAgent = "inventory-admission-test",
                Capabilities = [new FullNodeCapability(height)]
            }), peer);
            return peer;
        }

        private static Transaction CreateTransaction(uint nonce) => new()
        {
            Nonce = nonce,
            ValidUntilBlock = uint.MaxValue,
            Signers = [new Signer { Account = UInt160.Zero }],
            Attributes = [],
            Script = new byte[] { 0x21 },
            Witnesses = [new Witness()]
        };

        [TestMethod]
        public void NewTasks_RequestsDeferredTransactionsAfterCapacityFrees()
        {
            using var system = TestBlockchain.GetSystem();
            var manager = ActorOfAsTestActorRef(() => new TaskManager(system));
            var peer = Register(manager, NativeContract.Ledger.CurrentIndex(system.StoreView));
            var transactions = new Transaction[InvPayload.MaxHashesCount];
            for (uint i = 0; i < transactions.Length; i++)
                transactions[i] = CreateTransaction(i + 1);
            var hashes = Array.ConvertAll(transactions, tx => tx.Hash);

            manager.Receive(new TaskManager.NewTasks(InvPayload.Create(InventoryType.TX, hashes)), peer);
            var session = GetSessions(manager)[peer.Ref];
            Assert.AreEqual(TaskSession.MaxTasks, session.InvTasks.Count);
            Assert.AreEqual(InvPayload.MaxHashesCount - TaskSession.MaxTasks, session.DeferredTasks.Count);

            var initialRequests = new HashSet<UInt256>(session.InvTasks.Keys);
            Transaction completed = Array.Find(transactions, tx => session.InvTasks.ContainsKey(tx.Hash))!;
            peer.Send(manager, completed);

            Assert.AreEqual(TaskSession.MaxTasks, session.InvTasks.Count);
            Assert.AreEqual(InvPayload.MaxHashesCount - TaskSession.MaxTasks - 1, session.DeferredTasks.Count);
            Assert.IsFalse(session.InvTasks.ContainsKey(completed.Hash));
            Assert.AreEqual(TaskSession.MaxTasks, GetGlobalTasks(manager).Count);
            var newlyRequested = Array.Find(hashes, hash => session.InvTasks.ContainsKey(hash) && !initialRequests.Contains(hash));
            Assert.IsNotNull(newlyRequested);
            var request = peer.FishForMessage<Message>(
                message => message.Command == MessageCommand.GetData &&
                    Array.Exists(((InvPayload)message.Payload!).Hashes, hash => hash == newlyRequested),
                TimeSpan.FromSeconds(3), cancellationToken: System.Threading.CancellationToken.None);
            Assert.IsNotNull(request);
            Sys.Stop(manager);
        }

        [TestMethod]
        public void NewTasks_CapsFreshInventoryAcrossBatches()
        {
            using var system = TestBlockchain.GetSystem();
            var manager = ActorOfAsTestActorRef(() => new TaskManager(system));
            var peer = Register(manager, NativeContract.Ledger.CurrentIndex(system.StoreView));

            for (var batch = 0; batch < 3; batch++)
                manager.Receive(new TaskManager.NewTasks(InvPayload.Create(InventoryType.Block, Hashes(batch * 500 + 1, 500))), peer);

            var session = GetSessions(manager)[peer.Ref];
            Assert.AreEqual(TaskSession.MaxTasks, session.InvTasks.Count);
            Assert.AreEqual(TaskSession.MaxTasks, GetGlobalTasks(manager).Count);
            Assert.IsTrue(session.HasTooManyTasks);
            Sys.Stop(manager);
        }

        [TestMethod]
        public void NewTasks_CountsExistingIndexTasksAndAvailableBlocks()
        {
            using var system = TestBlockchain.GetSystem();
            var manager = ActorOfAsTestActorRef(() => new TaskManager(system));
            var height = NativeContract.Ledger.CurrentIndex(system.StoreView);
            var owners = new[] { Register(manager, height), Register(manager, height), Register(manager, height) };
            var waiter = Register(manager, height);
            var shared = Hashes(1, 50);

            foreach (var owner in owners)
                manager.Receive(new TaskManager.NewTasks(InvPayload.Create(InventoryType.Block, shared)), owner);
            manager.Receive(new TaskManager.NewTasks(InvPayload.Create(InventoryType.Block, shared)), waiter);
            var session = GetSessions(manager)[waiter.Ref];
            Assert.AreEqual(50, session.AvailableTasks.Count);

            for (uint i = 0; i < 40; i++)
                session.IndexTasks[height + i + 1] = TimeProvider.Current.UtcNow;
            manager.Receive(new TaskManager.NewTasks(InvPayload.Create(InventoryType.TX, Hashes(1000, 500))), waiter);

            Assert.AreEqual(10, session.InvTasks.Count);
            Assert.AreEqual(TaskSession.MaxTasks, session.TaskCount);
            Assert.AreEqual(60, GetGlobalTasks(manager).Count);
            Sys.Stop(manager);
        }

        [TestMethod]
        public void RequestTasks_LimitsIndexBatchToSessionCapacity()
        {
            using var system = TestBlockchain.GetSystem();
            var manager = ActorOfAsTestActorRef(() => new TaskManager(system));
            var height = NativeContract.Ledger.CurrentIndex(system.StoreView);
            GetGlobalTasks(manager)[UInt256.Zero] = 3;
            var peer = Register(manager, height + 500);
            var session = GetSessions(manager)[peer.Ref];

            Assert.AreEqual(TaskSession.MaxTasks, session.IndexTasks.Count);
            Assert.AreEqual(TaskSession.MaxTasks, session.TaskCount);
            Sys.Stop(manager);
        }
    }
}
