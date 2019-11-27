using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_VerifyingSenderFeeMonitor
    {
        [TestMethod]
        public void TestSenderVerifyingFee()
        {
            var tx = TestUtils.GetTransaction();
            tx.SystemFee = 10;
            tx.NetworkFee = 20;
            var monitor = new VerifyingSenderFeeMonitor();
            monitor.AddSenderVerifyingFee(tx);
            monitor.GetSenderVerifyingFee(tx.Sender).Should().Be(30);
            monitor.AddSenderVerifyingFee(tx);
            monitor.GetSenderVerifyingFee(tx.Sender).Should().Be(60);

            monitor.RemoveSenderVerifyingFee(tx);
            monitor.GetSenderVerifyingFee(tx.Sender).Should().Be(30);
            monitor.RemoveSenderVerifyingFee(tx);
            monitor.GetSenderVerifyingFee(tx.Sender).Should().Be(0);
        }
    }
}
