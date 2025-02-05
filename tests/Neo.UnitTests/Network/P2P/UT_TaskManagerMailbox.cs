// Copyright (C) 2015-2025 The Neo Project.
//
// UT_TaskManagerMailbox.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_TaskManagerMailbox : TestKit
    {
        private static readonly Random TestRandom = new Random(1337); // use fixed seed for guaranteed determinism

        TaskManagerMailbox uut;

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
            uut = new TaskManagerMailbox(akkaSettings, config);
        }

        [TestMethod]
        public void TaskManager_Test_IsHighPriority()
        {
            // high priority
            Assert.IsTrue(uut.IsHighPriority(new TaskManager.Register()));
            Assert.IsTrue(uut.IsHighPriority(new TaskManager.RestartTasks()));

            // low priority
            // -> NewTasks: generic InvPayload
            Assert.IsFalse(uut.IsHighPriority(new TaskManager.NewTasks { Payload = new InvPayload() }));

            // high priority
            // -> NewTasks: payload Block or Consensus
            Assert.IsTrue(uut.IsHighPriority(new TaskManager.NewTasks { Payload = new InvPayload { Type = InventoryType.Block } }));
            Assert.IsTrue(uut.IsHighPriority(new TaskManager.NewTasks { Payload = new InvPayload { Type = InventoryType.Extensible } }));

            // any random object should not have priority
            object obj = null;
            Assert.IsFalse(uut.IsHighPriority(obj));
        }
    }
}
