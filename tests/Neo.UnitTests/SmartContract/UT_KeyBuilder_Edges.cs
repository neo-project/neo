// Copyright (C) 2015-2026 The Neo Project.
//
// UT_KeyBuilder_Edges.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// Edge cases not covered by UT_KeyBuilder happy-path tests.
    /// </summary>
    [TestClass]
    public class UT_KeyBuilder_Edges
    {
        [TestMethod]
        public void Constructor_NegativeMaxLength_Throws()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new KeyBuilder(1, 2, -1));
        }

        [TestMethod]
        public void Add_ExceedsMaxLength_Throws()
        {
            var key = new KeyBuilder(1, 2, maxLength: 2);
            key = key.Add([0x01, 0x02]);
            Assert.ThrowsExactly<OverflowException>(() => key.Add([0x03]));
        }

        [TestMethod]
        public void Add_SingleByte()
        {
            var key = new KeyBuilder(1, 2).Add((byte)0xAB);
            Assert.AreEqual("0100000002ab", key.ToArray().ToHexString());
        }

        [TestMethod]
        public void AddBigEndian_Long()
        {
            var key = new KeyBuilder(1, 2).AddBigEndian(0x0102030405060708L);
            Assert.AreEqual("01000000020102030405060708", key.ToArray().ToHexString());
        }

        [TestMethod]
        public void ImplicitConversion_ToStorageKey()
        {
            KeyBuilder builder = new KeyBuilder(5, 9).Add((byte)0x11);
            StorageKey storageKey = builder;
            Assert.AreEqual(builder.ToArray().ToHexString(), storageKey.ToArray().ToHexString());
        }

        [TestMethod]
        public void PrefixLength_IsFive()
        {
            Assert.AreEqual(sizeof(int) + sizeof(byte), KeyBuilder.PrefixLength);
        }
    }
}
