using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using FluentAssertions;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_PoolItem
    {
        //private PoolItem uut;
        private static readonly Random TestRandom = new Random(1337); // use fixed seed for guaranteed determinism

        [TestInitialize]
        public void TestSetup()
        {
            var timeValues = new[] {
                new DateTime(1968, 06, 01, 0, 0, 1, DateTimeKind.Utc),
            };

            var timeMock = new Mock<TimeProvider>();
            timeMock.SetupGet(tp => tp.UtcNow).Returns(() => timeValues[0])
                                              .Callback(() => timeValues[0] = timeValues[0].Add(TimeSpan.FromSeconds(1)));
            TimeProvider.Current = timeMock.Object;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // important to leave TimeProvider correct
            TimeProvider.ResetToDefault();
        }

        [TestMethod]
        public void PoolItem_CompareTo_ClaimTx()
        {
            var tx1 = GenerateClaimTx();
            // Non-free low-priority transaction
            var tx2 = MockGenerateInvocationTx(new Fixed8(99999), 50).Object;

            var poolItem1 = new PoolItem(tx1);
            var poolItem2 = new PoolItem(tx2);
            poolItem1.CompareTo(poolItem2).Should().Be(1);
            poolItem2.CompareTo(poolItem1).Should().Be(-1);
        }

        [TestMethod]
        public void PoolItem_CompareTo_Fee()
        {
            int size1 = 50;
            int netFeeSatoshi1 = 1;
            var tx1 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshi1), size1);
            int size2 = 50;
            int netFeeSatoshi2 = 2;
            var tx2 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshi2), size2);

            PoolItem pitem1 = new PoolItem(tx1.Object);
            PoolItem pitem2 = new PoolItem(tx2.Object);

            Console.WriteLine($"item1 time {pitem1.Timestamp} item2 time {pitem2.Timestamp}");
            // pitem1 < pitem2 (fee) => -1
            pitem1.CompareTo(pitem2).Should().Be(-1);
            // pitem2 > pitem1 (fee) => 1
            pitem2.CompareTo(pitem1).Should().Be(1);
        }

        [TestMethod]
        public void PoolItem_CompareTo_Hash()
        {
            int sizeFixed = 50;
            int netFeeSatoshiFixed = 1;

            for (int testRuns = 0; testRuns < 30; testRuns++)
            {
                var tx1 = GenerateMockTxWithFirstByteOfHashGreaterThanOrEqualTo(0x80, new Fixed8(netFeeSatoshiFixed), sizeFixed);
                var tx2 = GenerateMockTxWithFirstByteOfHashLessThanOrEqualTo(0x79, new Fixed8(netFeeSatoshiFixed), sizeFixed);

                PoolItem pitem1 = new PoolItem(tx1.Object);
                PoolItem pitem2 = new PoolItem(tx2.Object);

                // pitem2 < pitem1 (fee) => -1
                pitem2.CompareTo(pitem1).Should().Be(-1);

                // pitem1 > pitem2  (fee) => 1
                pitem1.CompareTo(pitem2).Should().Be(1);
            }

            // equal hashes should be equal
            var tx3 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshiFixed), sizeFixed, new byte[] { 0x13, 0x37 });
            var tx4 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshiFixed), sizeFixed, new byte[] { 0x13, 0x37 });
            PoolItem pitem3 = new PoolItem(tx3.Object);
            PoolItem pitem4 = new PoolItem(tx4.Object);

            pitem3.CompareTo(pitem4).Should().Be(0);
            pitem4.CompareTo(pitem3).Should().Be(0);
        }

        [TestMethod]
        public void PoolItem_CompareTo_Equals()
        {
            int sizeFixed = 500;
            int netFeeSatoshiFixed = 10;
            var tx1 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshiFixed), sizeFixed, new byte[] { 0x13, 0x37 });
            var tx2 = MockGenerateInvocationTx(new Fixed8(netFeeSatoshiFixed), sizeFixed, new byte[] { 0x13, 0x37 });

            PoolItem pitem1 = new PoolItem(tx1.Object);
            PoolItem pitem2 = new PoolItem(tx2.Object);

            // pitem1 == pitem2 (fee) => 0
            pitem1.CompareTo(pitem2).Should().Be(0);
            pitem2.CompareTo(pitem1).Should().Be(0);
        }

        public Mock<InvocationTransaction> GenerateMockTxWithFirstByteOfHashGreaterThanOrEqualTo(byte firstHashByte, Fixed8 networkFee, int size)
        {
            Mock<InvocationTransaction> mockTx;
            do
            {
                mockTx = MockGenerateInvocationTx(networkFee, size);
            } while (mockTx.Object.Hash >= new UInt256(TestUtils.GetByteArray(32, firstHashByte)));

            return mockTx;
        }

        public Mock<InvocationTransaction> GenerateMockTxWithFirstByteOfHashLessThanOrEqualTo(byte firstHashByte, Fixed8 networkFee, int size)
        {
            Mock<InvocationTransaction> mockTx;
            do
            {
                mockTx = MockGenerateInvocationTx(networkFee, size);
            } while (mockTx.Object.Hash <= new UInt256(TestUtils.GetByteArray(32, firstHashByte)));

            return mockTx;
        }

        public static Transaction GenerateClaimTx()
        {
            var mockTx = new Mock<ClaimTransaction>();
            mockTx.CallBase = true;
            mockTx.SetupGet(mr => mr.NetworkFee).Returns(Fixed8.Zero);
            mockTx.SetupGet(mr => mr.Size).Returns(50);
            var tx = mockTx.Object;
            tx.Attributes = new TransactionAttribute[0];
            tx.Inputs = new CoinReference[0];
            tx.Outputs = new TransactionOutput[0];
            tx.Witnesses = new Witness[0];
            return mockTx.Object;
        }

        // Generate Mock InvocationTransaction with different sizes and prices
        public static Mock<InvocationTransaction> MockGenerateInvocationTx(Fixed8 networkFee, int size, byte[] overrideScriptBytes = null)
        {
            var mockTx = new Mock<InvocationTransaction>();
            mockTx.CallBase = true;
            mockTx.SetupGet(mr => mr.NetworkFee).Returns(networkFee);
            mockTx.SetupGet(mr => mr.Size).Returns(size);

            var tx = mockTx.Object;
            // use random bytes in the script to get a different hash since we cannot mock the Hash
            byte[] randomBytes;
            if (overrideScriptBytes != null)
                randomBytes = overrideScriptBytes;
            else
            {
                randomBytes = new byte[16];
                TestRandom.NextBytes(randomBytes);
            }
            tx.Script = randomBytes;
            tx.Attributes = new TransactionAttribute[0];
            tx.Inputs = new CoinReference[0];
            tx.Outputs = new TransactionOutput[0];
            tx.Witnesses = new Witness[0];
            return mockTx;
        }
    }
}
