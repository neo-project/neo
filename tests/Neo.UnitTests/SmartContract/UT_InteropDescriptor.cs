// Copyright (C) 2015-2026 The Neo Project.
//
// UT_InteropDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.SmartContract;
using System.Buffers.Binary;
using System.Reflection;
using System.Text;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_InteropDescriptor
    {
        public static void SampleHandler(int value) { }

        [TestMethod]
        public void Hash_IsSha256PrefixOfName()
        {
            var method = typeof(UT_InteropDescriptor).GetMethod(nameof(SampleHandler), BindingFlags.Public | BindingFlags.Static)!;
            var descriptor = new InteropDescriptor
            {
                Name = "System.Runtime.Log",
                Handler = method,
                FixedPrice = 1 << 15,
                RequiredCallFlags = CallFlags.AllowNotify
            };

            var expected = BinaryPrimitives.ReadUInt32LittleEndian(Encoding.ASCII.GetBytes(descriptor.Name).Sha256());
            Assert.AreEqual(expected, descriptor.Hash);
            Assert.AreEqual(expected, (uint)descriptor);
            Assert.AreEqual(expected, descriptor.Hash); // cached
        }

        [TestMethod]
        public void Parameters_ReflectHandler()
        {
            var method = typeof(UT_InteropDescriptor).GetMethod(nameof(SampleHandler), BindingFlags.Public | BindingFlags.Static)!;
            var descriptor = new InteropDescriptor
            {
                Name = "Test.Method",
                Handler = method,
                FixedPrice = 0,
                RequiredCallFlags = CallFlags.None,
                Hardfork = Hardfork.HF_Aspidochelone
            };

            Assert.HasCount(1, descriptor.Parameters);
            Assert.AreEqual(typeof(int), descriptor.Parameters[0].Type);
            Assert.AreEqual(0, descriptor.FixedPrice);
            Assert.AreEqual(CallFlags.None, descriptor.RequiredCallFlags);
            Assert.AreEqual(Hardfork.HF_Aspidochelone, descriptor.Hardfork);
            Assert.AreSame(method, descriptor.Handler);
        }
    }
}
