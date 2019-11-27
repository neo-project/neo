using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_ParallelVerifyTransaction
    {
        [TestMethod]
        public void TestConstructor()
        {
            var tx = TestUtils.GetTransaction();
            var pvt1 = new ParallelVerifyTransaction(tx);
            pvt1.Transaction.Should().Be(tx);
            pvt1.ShouldRelay.Should().BeTrue();

            var pvt2 = new ParallelVerifyTransaction(tx, false);
            pvt2.Transaction.Should().Be(tx);
            pvt2.ShouldRelay.Should().BeFalse();
        }
    }
}
