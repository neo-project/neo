// Copyright (C) 2015-2024 The Neo Project.
//
// UT_HeadersPayload.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_HeadersPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var header = new Header();
            TestUtils.SetupHeaderWithValues(header, UInt256.Zero, out _, out _, out _, out _, out _, out _);

            var test = HeadersPayload.Create();
            test.Size.Should().Be(1);
            test = HeadersPayload.Create(header);
            test.Size.Should().Be(1 + header.Size);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var header = new Header();
            TestUtils.SetupHeaderWithValues(header, UInt256.Zero, out _, out _, out _, out _, out _, out _);
            var test = HeadersPayload.Create(header);
            var clone = test.ToArray().AsSerializable<HeadersPayload>();

            Assert.AreEqual(test.Headers.Length, clone.Headers.Length);
            Assert.AreEqual(test.Headers[0], clone.Headers[0]);
        }
    }
}
