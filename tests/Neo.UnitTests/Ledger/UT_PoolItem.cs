// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PoolItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_PoolItem
    {
        private static readonly Random s_testRandom = new(1337); // use fixed seed for guaranteed determinism
        private readonly IComparer<PoolItem> _comparer;
        private readonly int _comparerExpected = 1;

        private class Comparer : IComparer<PoolItem>
        {
            public int Compare(PoolItem x, PoolItem y)
            {
                if (x == null)
                {
                    if (y == null) return 0;

                    return y.CompareTo(x) * -1; // reversed
                }

                return x.CompareTo(y);
            }
        }

        public UT_PoolItem() : this(new Comparer(), 1) { }

        internal UT_PoolItem(IComparer<PoolItem> comparer, int comparerExpected)
        {
            _comparer = comparer;
            _comparerExpected = comparerExpected;
        }

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
            var size1 = 51;
            var netFeeDatoshi1 = 1;
            var tx1 = GenerateTx(netFeeDatoshi1, size1);
            var size2 = 51;
            var netFeeDatoshi2 = 2;
            var tx2 = GenerateTx(netFeeDatoshi2, size2);

            var pitem1 = new PoolItem(tx1);
            var pitem2 = new PoolItem(tx2);

            Console.WriteLine($"item1 time {pitem1.Timestamp} item2 time {pitem2.Timestamp}");
            // pitem1 < pitem2 (fee) => -1
            Assert.AreEqual(-1, _comparer.Compare(pitem1, pitem2) * _comparerExpected);
            // pitem2 > pitem1 (fee) => 1
            Assert.AreEqual(1, _comparer.Compare(pitem2, pitem1) * _comparerExpected);
        }

        [TestMethod]
        public void PoolItem_CompareTo_Hash()
        {
            var sizeFixed = 51;
            var netFeeDatoshiFixed = 1;

            var tx1 = GenerateTxWithFirstByteOfHashGreaterThanOrEqualTo(0x80, netFeeDatoshiFixed, sizeFixed);
            var tx2 = GenerateTxWithFirstByteOfHashLessThanOrEqualTo(0x79, netFeeDatoshiFixed, sizeFixed);

            tx1.Attributes = [new HighPriorityAttribute()];

            var pitem1 = new PoolItem(tx1);
            var pitem2 = new PoolItem(tx2);

            // Different priority
            Assert.AreEqual(-1, _comparer.Compare(pitem2, pitem1) * _comparerExpected);

            // Bulk test
            for (var testRuns = 0; testRuns < 30; testRuns++)
            {
                tx1 = GenerateTxWithFirstByteOfHashGreaterThanOrEqualTo(0x80, netFeeDatoshiFixed, sizeFixed);
                tx2 = GenerateTxWithFirstByteOfHashLessThanOrEqualTo(0x79, netFeeDatoshiFixed, sizeFixed);

                pitem1 = new PoolItem(tx1);
                pitem2 = new PoolItem(tx2);

                Assert.AreEqual(1, _comparer.Compare(pitem2, null) * _comparerExpected);

                // pitem2.tx.Hash < pitem1.tx.Hash => 1 descending order
                Assert.AreEqual(1, _comparer.Compare(pitem2, pitem1) * _comparerExpected);

                // pitem2.tx.Hash > pitem1.tx.Hash => -1 descending order
                Assert.AreEqual(-1, _comparer.Compare(pitem1, pitem2) * _comparerExpected);
            }
        }

        [TestMethod]
        public void PoolItem_CompareTo_Equals()
        {
            var sizeFixed = 500;
            var netFeeDatoshiFixed = 10;
            var tx = GenerateTx(netFeeDatoshiFixed, sizeFixed, [0x13, 0x37]);

            var pitem1 = new PoolItem(tx);
            var pitem2 = new PoolItem(tx);

            // pitem1 == pitem2 (fee) => 0
            Assert.AreEqual(0, _comparer.Compare(pitem1, pitem2) * _comparerExpected);
            Assert.AreEqual(0, _comparer.Compare(pitem2, pitem1) * _comparerExpected);
            Assert.AreEqual(1, _comparer.Compare(pitem2, null) * _comparerExpected);
        }

        public static Transaction GenerateTxWithFirstByteOfHashGreaterThanOrEqualTo(byte firstHashByte, long networkFee, int size)
        {
            Transaction tx;
            do
            {
                tx = GenerateTx(networkFee, size);
            } while (tx.Hash < new UInt256(TestUtils.GetByteArray(32, firstHashByte)));

            return tx;
        }

        public static Transaction GenerateTxWithFirstByteOfHashLessThanOrEqualTo(byte firstHashByte, long networkFee, int size)
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
            var tx = new Transaction
            {
                Nonce = (uint)TestUtils.TestRandom.Next(),
                Script = overrideScriptBytes ?? ReadOnlyMemory<byte>.Empty,
                NetworkFee = networkFee,
                Attributes = [],
                Signers = [],
                Witnesses = [Witness.Empty]
            };

            Assert.AreEqual(0, tx.Attributes.Length);
            Assert.AreEqual(0, tx.Signers.Length);

            var diff = size - tx.Size;
            if (diff < 0) throw new ArgumentException();
            if (diff > 0)
                tx.Witnesses[0].VerificationScript = new byte[diff];
            return tx;
        }
    }
}
