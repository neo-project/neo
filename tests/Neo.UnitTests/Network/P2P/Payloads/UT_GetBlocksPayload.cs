// Copyright (C) 2015-2025 The Neo Project.
//
// UT_GetBlocksPayload.cs file belongs to the neo project and is free
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
    public class UT_GetBlocksPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new GetBlocksPayload() { Count = 5, HashStart = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01") };
            Assert.AreEqual(34, test.Size);

            test = new GetBlocksPayload() { Count = 1, HashStart = UInt256.Zero };
            Assert.AreEqual(34, test.Size);
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
