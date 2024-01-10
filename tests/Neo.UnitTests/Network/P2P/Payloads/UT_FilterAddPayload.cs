// Copyright (C) 2015-2024 The Neo Project.
//
// UT_FilterAddPayload.cs file belongs to the neo project and is free
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
    public class UT_FilterAddPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new FilterAddPayload() { Data = new byte[0] };
            test.Size.Should().Be(1);

            test = new FilterAddPayload() { Data = new byte[] { 1, 2, 3 } };
            test.Size.Should().Be(4);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new FilterAddPayload() { Data = new byte[] { 1, 2, 3 } };
            var clone = test.ToArray().AsSerializable<FilterAddPayload>();

            Assert.IsTrue(test.Data.Span.SequenceEqual(clone.Data.Span));
        }
    }
}
