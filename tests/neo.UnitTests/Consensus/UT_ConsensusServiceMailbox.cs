using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Consensus;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Consensus
{
    [TestClass]
    public class UT_ConsensusServiceMailbox : TestKit
    {
        private static readonly Random TestRandom = new Random(1337); // use fixed seed for guaranteed determinism

        ConsensusServiceMailbox uut;

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
            uut = new ConsensusServiceMailbox(akkaSettings, config);
        }

        [TestMethod]
        public void ConsensusServiceMailbox_Test_IsHighPriority()
        {
            // high priority
            uut.IsHighPriority(new ExtensiblePayload()).Should().Be(true);
            uut.IsHighPriority(new ConsensusService.SetViewNumber()).Should().Be(true);
            uut.IsHighPriority(new ConsensusService.Timer()).Should().Be(true);
            uut.IsHighPriority(new Blockchain.PersistCompleted()).Should().Be(true);

            // any random object should not have priority
            object obj = null;
            uut.IsHighPriority(obj).Should().Be(false);
        }
    }
}
