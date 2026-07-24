// Copyright (C) 2015-2026 The Neo Project.
//
// UT_BloomFilter_AfterAdd.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System.Linq;

namespace Neo.UnitTests.Cryptography
{
    /// <summary>
    /// Bit-array state after Add not covered by UT_BloomFilter constructor tests.
    /// </summary>
    [TestClass]
    public class UT_BloomFilter_AfterAdd
    {
        [TestMethod]
        public void GetBits_AfterAdd_HasSomeBitsSet()
        {
            var filter = new BloomFilter(64, 3, 1);
            byte[] element = [1, 2, 3, 4, 5];
            filter.Add(element);

            var bits = new byte[(filter.M + 7) / 8];
            filter.GetBits(bits);
            Assert.IsTrue(bits.Any(b => b != 0));
            Assert.IsTrue(filter.Check(element));
        }

        [TestMethod]
        public void Add_SameElementTwice_StillChecksTrue()
        {
            var filter = new BloomFilter(32, 2, 7);
            byte[] element = [9, 8, 7];
            filter.Add(element);
            filter.Add(element);
            Assert.IsTrue(filter.Check(element));
        }
    }
}
