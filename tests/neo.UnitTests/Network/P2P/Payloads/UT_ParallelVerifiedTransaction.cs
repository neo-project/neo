using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_ParallelVerifiedTransaction
    {
        [TestMethod]
        public void TestConstructor()
        {
            var tx = TestUtils.GetTransaction();
            var pvt1 = new ParallelVerifiedTransaction(tx, true);
            pvt1.Transaction.Should().Be(tx);
            pvt1.ShouldRelay.Should().BeTrue();
            pvt1.VerifyResult.Should().BeTrue();

            var pvt2 = new ParallelVerifiedTransaction(tx, true, false);
            pvt2.Transaction.Should().Be(tx);
            pvt2.ShouldRelay.Should().BeFalse();
            pvt2.VerifyResult.Should().BeTrue();
        }
    }
}
