// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ReversePoolItemComparer.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_ReversePoolItemComparer
    {
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
            var tx1 = UT_PoolItem.GenerateTx(netFeeDatoshi1, size1);
            var size2 = 51;
            var netFeeDatoshi2 = 2;
            var tx2 = UT_PoolItem.GenerateTx(netFeeDatoshi2, size2);

            var pitem1 = new PoolItem(tx1);
            var pitem2 = new PoolItem(tx2);

            Console.WriteLine($"item1 time {pitem1.Timestamp} item2 time {pitem2.Timestamp}");
            // pitem1 < pitem2 (fee) => -1
            Assert.AreEqual(1, ReversePoolItemComparer.Instance.Compare(pitem1, pitem2));
            // pitem2 > pitem1 (fee) => 1
            Assert.AreEqual(-1, ReversePoolItemComparer.Instance.Compare(pitem2, pitem1));
        }

        [TestMethod]
        public void PoolItem_CompareTo_Hash()
        {
            var sizeFixed = 51;
            var netFeeDatoshiFixed = 1;

            var tx1 = UT_PoolItem.GenerateTxWithFirstByteOfHashGreaterThanOrEqualTo(0x80, netFeeDatoshiFixed, sizeFixed);
            var tx2 = UT_PoolItem.GenerateTxWithFirstByteOfHashLessThanOrEqualTo(0x79, netFeeDatoshiFixed, sizeFixed);

            tx1.Attributes = [new HighPriorityAttribute()];

            var pitem1 = new PoolItem(tx1);
            var pitem2 = new PoolItem(tx2);

            // Different priority
            Assert.AreEqual(-1, pitem2.CompareTo(pitem1));

            // Bulk test
            for (var testRuns = 0; testRuns < 30; testRuns++)
            {
                tx1 = UT_PoolItem.GenerateTxWithFirstByteOfHashGreaterThanOrEqualTo(0x80, netFeeDatoshiFixed, sizeFixed);
                tx2 = UT_PoolItem.GenerateTxWithFirstByteOfHashLessThanOrEqualTo(0x79, netFeeDatoshiFixed, sizeFixed);

                pitem1 = new PoolItem(tx1);
                pitem2 = new PoolItem(tx2);

                Assert.AreEqual(-1, ReversePoolItemComparer.Instance.Compare(pitem2, null));

                // pitem2.tx.Hash < pitem1.tx.Hash => 1 descending order
                Assert.AreEqual(-1, ReversePoolItemComparer.Instance.Compare(pitem2, pitem1));

                // pitem2.tx.Hash > pitem1.tx.Hash => -1 descending order
                Assert.AreEqual(1, ReversePoolItemComparer.Instance.Compare(pitem1, pitem2));
            }
        }

        [TestMethod]
        public void PoolItem_CompareTo_Equals()
        {
            var sizeFixed = 500;
            var netFeeDatoshiFixed = 10;
            var tx = UT_PoolItem.GenerateTx(netFeeDatoshiFixed, sizeFixed, [0x13, 0x37]);

            var pitem1 = new PoolItem(tx);
            var pitem2 = new PoolItem(tx);

            // pitem1 == pitem2 (fee) => 0
            Assert.AreEqual(0, ReversePoolItemComparer.Instance.Compare(pitem1, pitem2));
            Assert.AreEqual(0, ReversePoolItemComparer.Instance.Compare(pitem2, pitem1));
            Assert.AreEqual(-1, ReversePoolItemComparer.Instance.Compare(pitem2, null));
        }
    }
}
