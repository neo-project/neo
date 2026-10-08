// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SmartAccountIntegerDomain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_SmartAccountIntegerDomain
    {
        [TestMethod]
        public void Unsigned256UpperHalfCannotUseIntegerAbi()
        {
            Assert.AreEqual(32, Integer.MaxSize);
            BigInteger firstUnrepresentable = BigInteger.One << 255;
            Assert.ThrowsExactly<ArgumentException>(() => new Integer(firstUnrepresentable));
            using ScriptBuilder builder = new();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => builder.EmitPush(firstUnrepresentable));
        }

        [TestMethod]
        public void LargestNonnegativeIntegerHas191ChannelBitsAnd64SequenceBits()
        {
            BigInteger maximum = (BigInteger.One << 255) - 1;
            Integer item = new(maximum);
            byte[] bytes = BinarySerializer.Serialize(item, ExecutionEngineLimits.Default);
            Assert.AreEqual(34, bytes.Length);
            Assert.AreEqual(maximum, BinarySerializer.Deserialize(bytes, ExecutionEngineLimits.Default).GetInteger());
            Assert.AreEqual((BigInteger.One << 191) - 1, maximum >> 64);
            Assert.AreEqual(new BigInteger(ulong.MaxValue), maximum & ulong.MaxValue);
        }
    }
}
