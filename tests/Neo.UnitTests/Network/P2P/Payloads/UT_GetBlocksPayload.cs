// Copyright (C) 2015-2024 The Neo Project.
//
// UT_GetBlocksPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_GetBlocksPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new GetBlocksPayload() { Count = 5, HashStart = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01") };
            test.Size.Should().Be(34);

            test = new GetBlocksPayload() { Count = 1, HashStart = UInt256.Zero };
            test.Size.Should().Be(34);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = GetBlocksPayload.Create(UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01"), 5);
            var clone = test.ToArray().AsSerializable<GetBlocksPayload>();

            Assert.AreEqual(test.Count, clone.Count);
            Assert.AreEqual(test.HashStart, clone.HashStart);
            Assert.AreEqual(5, clone.Count);
            Assert.AreEqual("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01", clone.HashStart.ToString());

            Assert.ThrowsException<FormatException>(() => GetBlocksPayload.Create(UInt256.Zero, -2).ToArray().AsSerializable<GetBlocksPayload>());
            Assert.ThrowsException<FormatException>(() => GetBlocksPayload.Create(UInt256.Zero, 0).ToArray().AsSerializable<GetBlocksPayload>());
        }
    }
}
