// Copyright (C) 2015-2025 The Neo Project.
//
// UT_BloomFilter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_BloomFilter
    {
        [TestMethod]
        public void TestAddCheck()
        {
            int m = 7, n = 10;
            uint nTweak = 123456;
            byte[] elements = { 0, 1, 2, 3, 4 };
            BloomFilter filter = new BloomFilter(m, n, nTweak);
            filter.Add(elements);
            Assert.IsTrue(filter.Check(elements));
            byte[] anotherElements = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.IsFalse(filter.Check(anotherElements));
        }

        [TestMethod]
        public void TestBloomFIlterConstructorGetKMTweak()
        {
            int m = -7, n = 10;
            uint nTweak = 123456;
            Action action = () => new BloomFilter(m, n, nTweak);
            Assert.ThrowsException<ArgumentOutOfRangeException>(action);
            action = () => new BloomFilter(m, n, nTweak, new byte[] { 0, 1, 2, 3, 4 });
            Assert.ThrowsException<ArgumentOutOfRangeException>(action);

            m = 7;
            n = -10;
            action = () => new BloomFilter(m, n, nTweak);
            Assert.ThrowsException<ArgumentOutOfRangeException>(action);

            n = 10;
            BloomFilter filter = new BloomFilter(m, n, nTweak);
            Assert.AreEqual(m, filter.M);
            Assert.AreEqual(n, filter.K);
            Assert.AreEqual(nTweak, filter.Tweak);

            byte[] shorterElements = { 0, 1, 2, 3, 4 };
            filter = new BloomFilter(m, n, nTweak, shorterElements);
            Assert.AreEqual(m, filter.M);
            Assert.AreEqual(n, filter.K);
            Assert.AreEqual(nTweak, filter.Tweak);

            byte[] longerElements = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            filter = new BloomFilter(m, n, nTweak, longerElements);
            Assert.AreEqual(m, filter.M);
            Assert.AreEqual(n, filter.K);
            Assert.AreEqual(nTweak, filter.Tweak);
        }

        [TestMethod]
        public void TestGetBits()
        {
            int m = 7, n = 10;
            uint nTweak = 123456;
            byte[] elements = { 0, 1, 2, 3, 4 };
            BloomFilter filter = new BloomFilter(m, n, nTweak);
            byte[] result = new byte[m];
            filter.GetBits(result);
            foreach (byte value in result)
                Assert.AreEqual(0, value);
        }

        [TestMethod]
        public void TestInvalidArguments()
        {
            uint nTweak = 123456;
            Action action = () => new BloomFilter(0, 3, nTweak);
            Assert.ThrowsException<ArgumentOutOfRangeException>(action);

            action = () => new BloomFilter(3, 0, nTweak);
            Assert.ThrowsException<ArgumentOutOfRangeException>(action);
        }
    }
}
