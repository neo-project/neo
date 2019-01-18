using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using FluentAssertions;
using Neo.Cryptography.ECC;
using Neo.IO.Wrappers;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_PoolItem
    {
        //private PoolItem uut;

        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestMethod]
        public void PoolItem_CompareTo_Fee()
        {
            int timeIndex = 0;
            var timeValues = new[] {
                new DateTime(1968, 06, 01, 0, 0, 1, DateTimeKind.Utc),
                new DateTime(1968, 06, 01, 0, 0, 2, DateTimeKind.Utc),
                new DateTime(1968, 06, 01, 0, 0, 3, DateTimeKind.Utc),
                new DateTime(1968, 06, 01, 0, 0, 4, DateTimeKind.Utc),
                new DateTime(1968, 06, 01, 0, 0, 5, DateTimeKind.Utc)
            };
            var timeMock = new Mock<TimeProvider>();
            timeMock.SetupGet(tp => tp.UtcNow).Returns(() => timeValues[timeIndex])
                                              .Callback(() => timeIndex++);
            TimeProvider.Current = timeMock.Object;

            int size1 = 50;
            int netFeeSatoshi1 = 1;
            var tx1 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshi1), size1, UInt256.Zero);
            int size2 = 50;
            int netFeeSatoshi2 = 2;
            var tx2 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshi2), size2, UInt256.Zero);

            PoolItem pitem1 = new PoolItem(tx1.Object);
            PoolItem pitem2 = new PoolItem(tx2.Object);
            // pitem1 < pitem2 (fee) => -1
            pitem1.CompareTo(pitem2).Should().Be(-1);
        }

        [TestMethod]
        public void PoolItem_CompareTo_Hash()
        {
            int sizeFixed = 50;
            int netFeeSatoshiFixed = 1;
            var tx1 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshiFixed), sizeFixed, UInt256.Zero);
            var tx2 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshiFixed), sizeFixed, UInt256.Zero + 1);

            PoolItem pitem1 = new PoolItem(tx1.Object);
            PoolItem pitem2 = new PoolItem(tx2.Object);
            // pitem2 < pitem1 (fee) => -1
            pitem2.CompareTo(pitem1).Should().Be(-1);
        }

        [TestMethod]
        public void PoolItem_CompareTo_Equals()
        {
        }

        // Generate Mock InvocationTransaction with different sizes and prices
        public static Mock<Transaction> MockGenerateInvocationTx(Fixed8 networkFee, int size, UInt256 hash)
        {
            var mockTx = new Mock<Transaction>(TransactionType.InvocationTransaction);
            mockTx.SetupGet(mr => mr.NetworkFee).Returns(networkFee);
            mockTx.SetupGet(mr => mr.Size).Returns(size);
            //mockTx.SetupGet(mr => mr.Hash).Returns(hash); // cannot overwrite this method, will see GetHashData
            return mockTx;
        }
    }
}
