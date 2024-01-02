// Copyright (C) 2015-2024 The Neo Project.
//
// UT_BloomFilter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
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
            filter.Check(elements).Should().BeTrue();
            byte[] anotherElements = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            filter.Check(anotherElements).Should().BeFalse();
        }

        [TestMethod]
        public void TestBloomFIlterConstructorGetKMTweak()
        {
            int m = -7, n = 10;
            uint nTweak = 123456;
            Action action = () => new BloomFilter(m, n, nTweak);
            action.Should().Throw<ArgumentOutOfRangeException>();

            m = 7;
            n = -10;
            action = () => new BloomFilter(m, n, nTweak);
            action.Should().Throw<ArgumentOutOfRangeException>();

            n = 10;
            BloomFilter filter = new BloomFilter(m, n, nTweak);
            filter.M.Should().Be(m);
            filter.K.Should().Be(n);
            filter.Tweak.Should().Be(nTweak);

            byte[] shorterElements = { 0, 1, 2, 3, 4 };
            filter = new BloomFilter(m, n, nTweak, shorterElements);
            filter.M.Should().Be(m);
            filter.K.Should().Be(n);
            filter.Tweak.Should().Be(nTweak);

            byte[] longerElements = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            filter = new BloomFilter(m, n, nTweak, longerElements);
            filter.M.Should().Be(m);
            filter.K.Should().Be(n);
            filter.Tweak.Should().Be(nTweak);
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
                value.Should().Be(0);
        }
    }
}
