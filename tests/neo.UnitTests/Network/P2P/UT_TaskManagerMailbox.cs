using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_SyncManagerMailbox : TestKit
    {
        private static readonly Random TestRandom = new Random(1337); // use fixed seed for guaranteed determinism

        SyncManagerMailbox uut;

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
            uut = new SyncManagerMailbox(akkaSettings, config);
        }

        [TestMethod]
        public void SyncManager_Test_IsHighPriority()
        {
            // high priority
            uut.IsHighPriority(new SyncManager.Register()).Should().Be(true);
            uut.IsHighPriority(new SyncManager.RestartTasks()).Should().Be(true);

            // low priority
            // -> NewTasks: generic InvPayload
            uut.IsHighPriority(new SyncManager.NewTasks { Payload = new InvPayload() }).Should().Be(false);

            // high priority
            // -> NewTasks: payload Consensus
            uut.IsHighPriority(new SyncManager.NewTasks { Payload = new InvPayload { Type = InventoryType.Consensus } }).Should().Be(true);

            // any random object should not have priority
            object obj = null;
            uut.IsHighPriority(obj).Should().Be(false);
        }
    }
}
