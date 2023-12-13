using System;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;

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
            uut.IsHighPriority(new TaskManager.Register()).Should().Be(true);
            uut.IsHighPriority(new TaskManager.RestartTasks()).Should().Be(true);

            // low priority
            // -> NewTasks: generic InvPayload
            uut.IsHighPriority(new TaskManager.NewTasks { Payload = new InvPayload() }).Should().Be(false);

            // high priority
            // -> NewTasks: payload Block or Consensus
            uut.IsHighPriority(new TaskManager.NewTasks { Payload = new InvPayload { Type = InventoryType.Block } }).Should().Be(true);
            uut.IsHighPriority(new TaskManager.NewTasks { Payload = new InvPayload { Type = InventoryType.Extensible } }).Should().Be(true);

            // any random object should not have priority
            object obj = null;
            uut.IsHighPriority(obj).Should().Be(false);
        }
    }
}
