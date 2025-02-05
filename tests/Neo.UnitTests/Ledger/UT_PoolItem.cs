// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PoolItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_PoolItem
    {
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
        public void PoolItem_CompareTo_Fee()
        {
            int size1 = 51;
            int netFeeDatoshi1 = 1;
            var tx1 = GenerateTx(netFeeDatoshi1, size1);
            int size2 = 51;
            int netFeeDatoshi2 = 2;
            var tx2 = GenerateTx(netFeeDatoshi2, size2);

            PoolItem pitem1 = new PoolItem(tx1);
            PoolItem pitem2 = new PoolItem(tx2);

            Console.WriteLine($"item1 time {pitem1.Timestamp} item2 time {pitem2.Timestamp}");
            // pitem1 < pitem2 (fee) => -1
            Assert.AreEqual(-1, pitem1.CompareTo(pitem2));
            // pitem2 > pitem1 (fee) => 1
            Assert.AreEqual(1, pitem2.CompareTo(pitem1));
        }

        [TestMethod]
        public void PoolItem_CompareTo_Hash()
        {
            int sizeFixed = 51;
            int netFeeDatoshiFixed = 1;

            var tx1 = GenerateTxWithFirstByteOfHashGreaterThanOrEqualTo(0x80, netFeeDatoshiFixed, sizeFixed);
            var tx2 = GenerateTxWithFirstByteOfHashLessThanOrEqualTo(0x79, netFeeDatoshiFixed, sizeFixed);

            tx1.Attributes = new TransactionAttribute[] { new HighPriorityAttribute() };

            PoolItem pitem1 = new PoolItem(tx1);
            PoolItem pitem2 = new PoolItem(tx2);

            // Different priority
            Assert.AreEqual(-1, pitem2.CompareTo(pitem1));

            // Bulk test
            for (int testRuns = 0; testRuns < 30; testRuns++)
            {
                tx1 = GenerateTxWithFirstByteOfHashGreaterThanOrEqualTo(0x80, netFeeDatoshiFixed, sizeFixed);
                tx2 = GenerateTxWithFirstByteOfHashLessThanOrEqualTo(0x79, netFeeDatoshiFixed, sizeFixed);

                pitem1 = new PoolItem(tx1);
                pitem2 = new PoolItem(tx2);

                Assert.AreEqual(1, pitem2.CompareTo((Transaction)null));

                // pitem2.tx.Hash < pitem1.tx.Hash => 1 descending order
                Assert.AreEqual(1, pitem2.CompareTo(pitem1));

                // pitem2.tx.Hash > pitem1.tx.Hash => -1 descending order
                Assert.AreEqual(-1, pitem1.CompareTo(pitem2));
            }
        }

        [TestMethod]
        public void PoolItem_CompareTo_Equals()
        {
            int sizeFixed = 500;
            int netFeeDatoshiFixed = 10;
            var tx = GenerateTx(netFeeDatoshiFixed, sizeFixed, new byte[] { 0x13, 0x37 });

            PoolItem pitem1 = new PoolItem(tx);
            PoolItem pitem2 = new PoolItem(tx);

            // pitem1 == pitem2 (fee) => 0
            Assert.AreEqual(0, pitem1.CompareTo(pitem2));
            Assert.AreEqual(0, pitem2.CompareTo(pitem1));
            Assert.AreEqual(1, pitem2.CompareTo((PoolItem)null));
        }

        public Transaction GenerateTxWithFirstByteOfHashGreaterThanOrEqualTo(byte firstHashByte, long networkFee, int size)
        {
            Transaction tx;
            do
            {
                tx = GenerateTx(networkFee, size);
            } while (tx.Hash < new UInt256(TestUtils.GetByteArray(32, firstHashByte)));

            return tx;
        }

        public Transaction GenerateTxWithFirstByteOfHashLessThanOrEqualTo(byte firstHashByte, long networkFee, int size)
        {
            Transaction tx;
            do
            {
                tx = GenerateTx(networkFee, size);
            } while (tx.Hash > new UInt256(TestUtils.GetByteArray(32, firstHashByte)));

            return tx;
        }

        // Generate Transaction with different sizes and prices
        public static Transaction GenerateTx(long networkFee, int size, byte[] overrideScriptBytes = null)
        {
            Transaction tx = new Transaction
            {
                Nonce = (uint)TestRandom.Next(),
                Script = overrideScriptBytes ?? new byte[0],
                NetworkFee = networkFee,
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = Array.Empty<Signer>(),
                Witnesses = new[]
                {
                    new Witness
                    {
                        InvocationScript = new byte[0],
                        VerificationScript = new byte[0]
                    }
                }
            };

            Assert.AreEqual(0, tx.Attributes.Length);
            Assert.AreEqual(0, tx.Signers.Length);

            int diff = size - tx.Size;
            if (diff < 0) throw new ArgumentException();
            if (diff > 0)
                tx.Witnesses[0].VerificationScript = new byte[diff];
            return tx;
        }
    }
}
