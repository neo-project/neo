// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TaskManager.cs file belongs to the neo project and is free
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
using System.Threading;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_TaskManager : TestKit
    {
        private static NeoSystem s_system;

        [ClassInitialize]
        public static void ClassSetup(TestContext _)
        {
            s_system = TestBlockchain.GetSystem();
        }

        private static VersionPayload MakeVersion(uint startHeight)
        {
            return new VersionPayload
            {
                UserAgent = "TaskManagerUT",
                Nonce = 0xBEEF,
                Network = TestProtocolSettings.Default.Network,
                Timestamp = 1,
                Version = LocalNode.ProtocolVersion,
                Capabilities = [new FullNodeCapability(startHeight)]
            };
        }

        [TestMethod]
        public void Register_AtOrBelowLocalHeight_RequestsMempool()
        {
            var remote = CreateTestProbe("remote-mempool");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));

            var msg = remote.FishForMessage<Message>(
                m => m.Command == MessageCommand.Mempool,
                TimeSpan.FromSeconds(3),
                cancellationToken: CancellationToken.None);
            Assert.IsNotNull(msg);
            Assert.AreEqual(MessageCommand.Mempool, msg.Command);
        }

        [TestMethod]
        public void Register_HigherPeer_RequestsWork()
        {
            var remote = CreateTestProbe("remote-headers");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height + 50)));

            var msg = remote.FishForMessage<Message>(
                m => m.Command is MessageCommand.GetHeaders or MessageCommand.GetBlockByIndex or MessageCommand.Mempool,
                TimeSpan.FromSeconds(3),
                cancellationToken: CancellationToken.None);
            Assert.IsNotNull(msg);
        }

        [TestMethod]
        public void Update_WithoutSession_IsIgnored()
        {
            var remote = CreateTestProbe("remote-orphan-update");
            remote.Send(s_system.TaskManager, new TaskManager.Update(123));
            remote.ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void Update_AfterRegister_DoesNotThrow()
        {
            var remote = CreateTestProbe("remote-update");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            remote.Send(s_system.TaskManager, new TaskManager.Update(height + 10));
            remote.ReceiveOne(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void NewTasks_TxInventory_DoesNotFaultActor()
        {
            var remote = CreateTestProbe("remote-newtasks");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            var hash = UInt256.Parse("0x1111111111111111111111111111111111111111111111111111111111111111");
            remote.Send(s_system.TaskManager, new TaskManager.NewTasks(
                InvPayload.Create(InventoryType.TX, hash)));

            // TX NewTasks may produce GetData (or nothing); either is fine — actor must stay healthy.
            _ = remote.ReceiveOne(TimeSpan.FromMilliseconds(400), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void RestartTasks_DoesNotFaultActor()
        {
            var remote = CreateTestProbe("remote-restart");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            var hash = UInt256.Parse("0x2222222222222222222222222222222222222222222222222222222222222222");
            remote.Send(s_system.TaskManager, new TaskManager.RestartTasks(
                InvPayload.Create(InventoryType.Block, hash)));

            remote.ExpectNoMsg(TimeSpan.FromMilliseconds(200), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void PersistCompleted_IsAccepted()
        {
            var remote = CreateTestProbe("remote-persist");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height + 5)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            var block = NativeContract.Ledger.GetBlock(s_system.StoreView, 0);
            Assert.IsNotNull(block);
            s_system.TaskManager.Tell(new Blockchain.PersistCompleted(block), remote);
            remote.ReceiveOne(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void InventoryCompleted_IsAccepted()
        {
            var remote = CreateTestProbe("remote-inv-done");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            var block = NativeContract.Ledger.GetBlock(s_system.StoreView, 0);
            remote.Send(s_system.TaskManager, block);
            remote.ReceiveOne(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
        }
    }
}
