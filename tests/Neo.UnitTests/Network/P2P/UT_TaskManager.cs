// Copyright (C) 2015-2025 The Neo Project.
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
using Akka.TestKit;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_TaskManager : TestKit
    {
        public UT_TaskManager()
            : base($"task-manager-mailbox {{ mailbox-type: \"{typeof(TaskManagerMailbox).AssemblyQualifiedName}\" }}")
        {
        }

        private NeoSystem _system;
        private IActorRef _taskManager;
        private TestTimeProvider _timeProvider;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetUp()
        {
            _system = TestBlockchain.GetSystem();
            _timeProvider = new TestTimeProvider(DateTime.UtcNow);
            TimeProvider.Current = _timeProvider;
            _taskManager = Sys.ActorOf(TaskManager.Props(_system));
        }

        [TestCleanup]
        public void TearDown()
        {
            if (_taskManager != null && _taskManager != ActorRefs.Nobody)
            {
                Sys.Stop(_taskManager);
                _taskManager = ActorRefs.Nobody;
            }
            TimeProvider.ResetToDefault();
            Shutdown();
        }

        [TestMethod]
        public void RegisterFullNodeRequestsHeadersImmediately()
        {
            var remoteProbe = CreateTestProbe();
            var version = VersionPayload.Create(
                _system.Settings.Network,
                nonce: 1,
                userAgent: LocalNode.UserAgent,
                new FullNodeCapability(10));

            remoteProbe.Send(_taskManager, new TaskManager.Register { Version = version });

            var message = remoteProbe.ExpectMsg<Message>(TimeSpan.FromSeconds(1), cancellationToken: TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(MessageCommand.GetHeaders, message.Command);
            var payload = (GetBlockByIndexPayload)message.Payload;
            Assert.AreEqual(1u, payload.IndexStart);
        }

        [TestMethod]
        public void MempoolRequestIsSentOnlyOnceWhenSynced()
        {
            var remoteProbe = CreateTestProbe();
            var currentHeight = NativeContract.Ledger.CurrentIndex(_system.StoreView);
            var version = VersionPayload.Create(
                _system.Settings.Network,
                nonce: 2,
                userAgent: LocalNode.UserAgent,
                new FullNodeCapability(currentHeight));

            remoteProbe.Send(_taskManager, new TaskManager.Register { Version = version });

            var firstMessage = remoteProbe.ExpectMsg<Message>(TimeSpan.FromSeconds(1), cancellationToken: TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(MessageCommand.Mempool, firstMessage.Command);

            TriggerTimer();
#pragma warning disable MSTEST0049
            remoteProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(200), TestContext.CancellationTokenSource.Token);
#pragma warning restore MSTEST0049
        }

        [TestMethod]
        public void TaskManager_RequestsDataForUnknownBlocks()
        {
            var remoteProbe = CreateTestProbe();
            var currentHeight = NativeContract.Ledger.CurrentIndex(_system.StoreView);
            var version = VersionPayload.Create(
                _system.Settings.Network,
                nonce: 3,
                userAgent: LocalNode.UserAgent,
                new FullNodeCapability(currentHeight + 5));

            remoteProbe.Send(_taskManager, new TaskManager.Register { Version = version });

            // Drain initial header sync
            ExpectCommand(remoteProbe, MessageCommand.GetHeaders);

            Span<byte> buffer = stackalloc byte[UInt256.Length];
            RandomNumberGenerator.Fill(buffer);
            var hash = new UInt256(buffer);

            remoteProbe.Send(_taskManager, new TaskManager.NewTasks
            {
                Payload = InvPayload.Create(InventoryType.Block, hash)
            });

            var getData = ExpectCommand(remoteProbe, MessageCommand.GetData);
            var payload = (InvPayload)getData.Payload;
            CollectionAssert.Contains(payload.Hashes, hash);
        }

        [TestMethod]
        public void RestartTasks_ForwardsGetDataToLocalNode()
        {
            var customSystem = TestBlockchain.GetSystem();
            var localNodeProbe = CreateTestProbe();
            var originalLocalNode = customSystem.LocalNode;
            customSystem.EnsureStopped(originalLocalNode);
            typeof(NeoSystem).GetField("<LocalNode>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(customSystem, localNodeProbe.Ref);

            var manager = Sys.ActorOf(TaskManager.Props(customSystem));

            Span<byte> buffer = stackalloc byte[UInt256.Length];
            RandomNumberGenerator.Fill(buffer);
            var hash = new UInt256(buffer);

            manager.Tell(new TaskManager.RestartTasks
            {
                Payload = InvPayload.Create(InventoryType.Block, hash)
            });

            var message = localNodeProbe.ExpectMsg<Message>(TimeSpan.FromSeconds(1), cancellationToken: TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(MessageCommand.GetData, message.Command);
            var payload = (InvPayload)message.Payload;
            CollectionAssert.Contains(payload.Hashes, hash);

            Sys.Stop(manager);
            customSystem.Dispose();
        }

        [TestMethod]
        public void RestartTasks_GroupsRequestsInBatchesOfFiveHundred()
        {
            var customSystem = TestBlockchain.GetSystem();
            var localNodeProbe = CreateTestProbe();
            var originalLocalNode = customSystem.LocalNode;
            customSystem.EnsureStopped(originalLocalNode);
            typeof(NeoSystem).GetField("<LocalNode>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(customSystem, localNodeProbe.Ref);

            var manager = Sys.ActorOf(TaskManager.Props(customSystem));

            const int total = InvPayload.MaxHashesCount + 123;
            var hashes = new UInt256[total];
            var temp = new byte[UInt256.Length];
            for (int i = 0; i < total; i++)
            {
                RandomNumberGenerator.Fill(temp);
                hashes[i] = new UInt256(temp);
            }

            manager.Tell(new TaskManager.RestartTasks
            {
                Payload = InvPayload.Create(InventoryType.Block, hashes)
            });

            var first = localNodeProbe.ExpectMsg<Message>(TimeSpan.FromSeconds(1), cancellationToken: TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(MessageCommand.GetData, first.Command);
            var firstPayload = (InvPayload)first.Payload;
            Assert.AreEqual(InvPayload.MaxHashesCount, firstPayload.Hashes.Length);

            var second = localNodeProbe.ExpectMsg<Message>(TimeSpan.FromSeconds(1), cancellationToken: TestContext.CancellationTokenSource.Token);
            Assert.AreEqual(MessageCommand.GetData, second.Command);
            var secondPayload = (InvPayload)second.Payload;
            Assert.AreEqual(total - InvPayload.MaxHashesCount, secondPayload.Hashes.Length);

            CollectionAssert.AreEquivalent(hashes, firstPayload.Hashes.Concat(secondPayload.Hashes).ToArray());

            Sys.Stop(manager);
            customSystem.Dispose();
        }

        private void TriggerTimer()
        {
            var timerType = typeof(TaskManager).GetNestedType("Timer", BindingFlags.NonPublic);
            var timerMessage = Activator.CreateInstance(timerType!);
            _taskManager.Tell(timerMessage!);
        }

        private Message ExpectCommand(TestProbe probe, MessageCommand command)
        {
            for (int i = 0; i < 10; i++)
            {
                var obj = probe.ExpectMsg<object>(TimeSpan.FromSeconds(1), cancellationToken: TestContext.CancellationTokenSource.Token);
                if (obj is Message message && message.Command == command)
                    return message;
            }

            Assert.Fail($"Expected {command} but did not observe it.");
            return null;
        }

        private sealed class TestTimeProvider : TimeProvider
        {
            public TestTimeProvider(DateTime now) => Now = now;

            public DateTime Now { get; set; }

            public override DateTime UtcNow => Now;
        }
    }
}
