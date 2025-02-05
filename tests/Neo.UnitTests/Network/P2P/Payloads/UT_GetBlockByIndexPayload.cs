// Copyright (C) 2015-2025 The Neo Project.
//
// UT_GetBlockByIndexPayload.cs file belongs to the neo project and is free
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
    public class UT_GetBlockByIndexPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new GetBlockByIndexPayload() { Count = 5, IndexStart = 5 };
            Assert.AreEqual(6, test.Size);

            test = GetBlockByIndexPayload.Create(1, short.MaxValue);
            Assert.AreEqual(6, test.Size);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new GetBlockByIndexPayload() { Count = -1, IndexStart = int.MaxValue };
            var clone = test.ToArray().AsSerializable<GetBlockByIndexPayload>();

            Assert.AreEqual(test.Count, clone.Count);
            Assert.AreEqual(test.IndexStart, clone.IndexStart);

            test = new GetBlockByIndexPayload() { Count = -2, IndexStart = int.MaxValue };
            Assert.ThrowsException<FormatException>(() => test.ToArray().AsSerializable<GetBlockByIndexPayload>());

            test = new GetBlockByIndexPayload() { Count = 0, IndexStart = int.MaxValue };
            Assert.ThrowsException<FormatException>(() => test.ToArray().AsSerializable<GetBlockByIndexPayload>());

            test = new GetBlockByIndexPayload() { Count = HeadersPayload.MaxHeadersCount + 1, IndexStart = int.MaxValue };
            Assert.ThrowsException<FormatException>(() => test.ToArray().AsSerializable<GetBlockByIndexPayload>());
        }
    }
}
