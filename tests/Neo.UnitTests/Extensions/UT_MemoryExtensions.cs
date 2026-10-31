// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MemoryExtensions.cs file belongs to the neo project and is free
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
using System;

namespace Neo.UnitTests.Extensions
{
    [TestClass]
    public class UT_MemoryExtensions
    {
        [TestMethod]
        public void AsSerializable_Empty_Throws()
        {
            ReadOnlyMemory<byte> empty = ReadOnlyMemory<byte>.Empty;
            Assert.ThrowsExactly<FormatException>(() => empty.AsSerializable<UInt160>());
            Assert.ThrowsExactly<FormatException>(() => empty.AsSerializableArray<UInt160>());
        }

        [TestMethod]
        public void AsSerializable_Generic_RoundTrip()
        {
            var hash = UInt160.Parse("0x0000000000000000000000000000000000000001");
            ReadOnlyMemory<byte> memory = hash.ToArray();
            var clone = memory.AsSerializable<UInt160>();
            Assert.AreEqual(hash, clone);
        }

        [TestMethod]
        public void AsSerializable_ByType_RoundTrip()
        {
            var hash = UInt256.Zero;
            ReadOnlyMemory<byte> memory = hash.ToArray();
            var clone = memory.AsSerializable(typeof(UInt256));
            Assert.IsInstanceOfType<UInt256>(clone);
            Assert.AreEqual(hash, clone);
        }

        [TestMethod]
        public void AsSerializable_ByType_NotISerializable_Throws()
        {
            ReadOnlyMemory<byte> data = new byte[] { 1 };
            Assert.ThrowsExactly<InvalidCastException>(() => data.AsSerializable(typeof(string)));
        }

        [TestMethod]
        public void AsSerializableArray_RoundTrip()
        {
            UInt160[] hashes =
            [
                UInt160.Zero,
                UInt160.Parse("0x0000000000000000000000000000000000000002")
            ];
            ReadOnlyMemory<byte> memory = hashes.ToByteArray();
            var clone = memory.AsSerializableArray<UInt160>();
            Assert.HasCount(2, clone);
            Assert.AreEqual(hashes[0], clone[0]);
            Assert.AreEqual(hashes[1], clone[1]);
        }

        [TestMethod]
        public void GetVarSize_IncludesPrefixAndLength()
        {
            ReadOnlyMemory<byte> data = new byte[] { 1, 2, 3 };
            Assert.AreEqual(1 + 3, data.GetVarSize());
            Assert.AreEqual(0.GetVarSize() + 0, ReadOnlyMemory<byte>.Empty.GetVarSize());
        }
    }
}
