using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public partial class UT_ApplicationEngine
    {
        private string eventName = null;

        [TestMethod]
        public void TestNotify()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(System.Array.Empty<byte>());
            ApplicationEngine.Notify += Test_Notify1;
            const string notifyEvent = "TestEvent";

            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            eventName.Should().Be(notifyEvent);

            ApplicationEngine.Notify += Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            eventName.Should().Be(null);

            eventName = notifyEvent;
            ApplicationEngine.Notify -= Test_Notify1;
            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            eventName.Should().Be(null);

            ApplicationEngine.Notify -= Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            eventName.Should().Be(null);
        }

        private void Test_Notify1(object sender, NotifyEventArgs e)
        {
            eventName = e.EventName;
        }

        private void Test_Notify2(object sender, NotifyEventArgs e)
        {
            eventName = null;
        }

        [TestMethod]
        public void TestCreateDummyBlock()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            ApplicationEngine engine = ApplicationEngine.Run(SyscallSystemRuntimeCheckWitnessHash, snapshot, settings: TestProtocolSettings.Default);
            engine.PersistingBlock.Version.Should().Be(0);
            engine.PersistingBlock.PrevHash.Should().Be(TestBlockchain.TheNeoSystem.GenesisBlock.Hash);
            engine.PersistingBlock.MerkleRoot.Should().Be(new UInt256());
        }

        [TestMethod]
        public void TestCheckingHardfork()
        {
            var allHardforks = Enum.GetValues(typeof(Hardfork)).Cast<Hardfork>().ToList();

            var builder = ImmutableDictionary.CreateBuilder<Hardfork, uint>();
            builder.Add(Hardfork.HF_Aspidochelone, 0);
            builder.Add(Hardfork.HF_Basilisk, 1);

            var setting = builder.ToImmutable();

            // Check for continuity in configured hardforks
            var sortedHardforks = setting.Keys
                .OrderBy(h => allHardforks.IndexOf(h))
                .ToList();

            for (int i = 0; i < sortedHardforks.Count - 1; i++)
            {
                int currentIndex = allHardforks.IndexOf(sortedHardforks[i]);
                int nextIndex = allHardforks.IndexOf(sortedHardforks[i + 1]);

                // If they aren't consecutive, return false.
                var inc = nextIndex - currentIndex;
                inc.Should().Be(1);
            }

            // Check that block numbers are not higher in earlier hardforks than in later ones
            for (int i = 0; i < sortedHardforks.Count - 1; i++)
            {
                (setting[sortedHardforks[i]] > setting[sortedHardforks[i + 1]]).Should().Be(false);
            }
        }
    }
}
