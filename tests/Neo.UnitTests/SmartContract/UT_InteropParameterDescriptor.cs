// Copyright (C) 2015-2026 The Neo Project.
//
// UT_InteropParameterDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_InteropParameterDescriptor
    {
        [TestMethod]
        public void PrimitiveTypes_Converters()
        {
            Assert.IsTrue((bool)new InteropParameterDescriptor(typeof(bool)).Converter(StackItem.True)!);
            Assert.AreEqual((sbyte)1, new InteropParameterDescriptor(typeof(sbyte)).Converter(1)!);
            Assert.AreEqual((byte)2, new InteropParameterDescriptor(typeof(byte)).Converter(2)!);
            Assert.AreEqual((short)3, new InteropParameterDescriptor(typeof(short)).Converter(3)!);
            Assert.AreEqual((ushort)4, new InteropParameterDescriptor(typeof(ushort)).Converter(4)!);
            Assert.AreEqual(5, new InteropParameterDescriptor(typeof(int)).Converter(5)!);
            Assert.AreEqual(6u, new InteropParameterDescriptor(typeof(uint)).Converter(6)!);
            Assert.AreEqual(7L, new InteropParameterDescriptor(typeof(long)).Converter(7)!);
            Assert.AreEqual(8UL, new InteropParameterDescriptor(typeof(ulong)).Converter(8)!);
            Assert.AreEqual((BigInteger)9, new InteropParameterDescriptor(typeof(BigInteger)).Converter(9)!);
            Assert.AreEqual("abc", new InteropParameterDescriptor(typeof(string)).Converter((ByteString)"abc"));
        }

        [TestMethod]
        public void ByteArray_And_Null()
        {
            var descriptor = new InteropParameterDescriptor(typeof(byte[]));
            Assert.IsTrue(new byte[] { 1, 2 }.AsSpan().SequenceEqual((byte[])descriptor.Converter((byte[])[1, 2])!));
            Assert.IsNull(descriptor.Converter(StackItem.Null));
            Assert.IsNull(new InteropParameterDescriptor(typeof(string)).Converter(StackItem.Null));
        }

        [TestMethod]
        public void HashAndPoint_Converters()
        {
            var hash160 = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var hash256 = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000002");
            var point = ECCurve.Secp256r1.G;

            Assert.AreEqual(hash160, new InteropParameterDescriptor(typeof(UInt160)).Converter((ByteString)hash160.ToArray()));
            Assert.AreEqual(hash256, new InteropParameterDescriptor(typeof(UInt256)).Converter((ByteString)hash256.ToArray()));
            Assert.AreEqual(point, new InteropParameterDescriptor(typeof(ECPoint)).Converter((ByteString)point.EncodePoint(true)));
            Assert.IsNull(new InteropParameterDescriptor(typeof(UInt160)).Converter(StackItem.Null));
        }

        [TestMethod]
        public void Flags_IsEnum_IsArray_IsInterface()
        {
            var enumDesc = new InteropParameterDescriptor(typeof(CallFlags));
            Assert.IsTrue(enumDesc.IsEnum);
            Assert.IsFalse(enumDesc.IsArray);
            Assert.AreEqual(CallFlags.ReadStates, (CallFlags)(byte)enumDesc.Converter((Integer)(int)CallFlags.ReadStates)!);

            var arrayDesc = new InteropParameterDescriptor(typeof(int[]));
            Assert.IsTrue(arrayDesc.IsArray);
            Assert.IsFalse(arrayDesc.IsEnum);

            var interfaceDesc = new InteropParameterDescriptor(typeof(IDisposable));
            Assert.IsTrue(interfaceDesc.IsInterface);

            var stackItemDesc = new InteropParameterDescriptor(typeof(StackItem));
            Assert.IsFalse(stackItemDesc.IsInterface);
            Assert.AreSame(StackItem.True, stackItemDesc.Converter(StackItem.True));
        }

        [TestMethod]
        public void Nullable_And_Validate_NoValidators()
        {
            var nullable = new InteropParameterDescriptor(typeof(int?));
            Assert.IsTrue(nullable.IsNullable);
            Assert.AreEqual(typeof(int?), nullable.Type);
            nullable.Validate(StackItem.Null); // no validators → no-op
        }
    }
}
