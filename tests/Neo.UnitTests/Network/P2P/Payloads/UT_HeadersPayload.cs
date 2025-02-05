// Copyright (C) 2015-2025 The Neo Project.
//
// UT_HeadersPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_HeadersPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var header = TestUtils.MakeHeader(null, UInt256.Zero);

            var test = HeadersPayload.Create();
            Assert.AreEqual(1, test.Size);
            test = HeadersPayload.Create(header);
            Assert.AreEqual(1 + header.Size, test.Size);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var header = TestUtils.MakeHeader(null, UInt256.Zero);
            var test = HeadersPayload.Create(header);
            var clone = test.ToArray().AsSerializable<HeadersPayload>();

            Assert.AreEqual(test.Headers.Length, clone.Headers.Length);
            Assert.AreEqual(test.Headers[0], clone.Headers[0]);
        }
    }
}
