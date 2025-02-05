// Copyright (C) 2015-2025 The Neo Project.
//
// UT_FilterLoadPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_FilterLoadPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new FilterLoadPayload() { Filter = Array.Empty<byte>(), K = 1, Tweak = uint.MaxValue };
            Assert.AreEqual(6, test.Size);

            test = FilterLoadPayload.Create(new Neo.Cryptography.BloomFilter(8, 10, 123456));
            Assert.AreEqual(7, test.Size);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = FilterLoadPayload.Create(new Neo.Cryptography.BloomFilter(8, 10, 123456));
            var clone = test.ToArray().AsSerializable<FilterLoadPayload>();

            CollectionAssert.AreEqual(test.Filter.ToArray(), clone.Filter.ToArray());
            Assert.AreEqual(test.K, clone.K);
            Assert.AreEqual(test.Tweak, clone.Tweak);

            Assert.ThrowsException<FormatException>(() => new FilterLoadPayload() { Filter = Array.Empty<byte>(), K = 51, Tweak = uint.MaxValue }.ToArray().AsSerializable<FilterLoadPayload>());
        }
    }
}
