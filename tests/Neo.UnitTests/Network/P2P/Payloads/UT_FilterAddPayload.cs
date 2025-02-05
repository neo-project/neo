// Copyright (C) 2015-2025 The Neo Project.
//
// UT_FilterAddPayload.cs file belongs to the neo project and is free
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
    public class UT_FilterAddPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new FilterAddPayload() { Data = new byte[0] };
            Assert.AreEqual(1, test.Size);

            test = new FilterAddPayload() { Data = new byte[] { 1, 2, 3 } };
            Assert.AreEqual(4, test.Size);
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
