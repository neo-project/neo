// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractParameter_ToStackItem.cs file belongs to the neo project and is free
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
using System.Collections.Generic;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// Covers ContractParameterExtensions.ToStackItem (not covered by UT_ContractParameter).
    /// </summary>
    [TestClass]
    public class UT_ContractParameter_ToStackItem
    {
        [TestMethod]
        public void NullParameter_Throws()
        {
            ContractParameter parameter = null;
            Assert.ThrowsExactly<ArgumentNullException>(() => parameter.ToStackItem());
        }

        [TestMethod]
        public void NullValue_ReturnsStackNull()
        {
            var parameter = new ContractParameter(ContractParameterType.Integer) { Value = null };
            Assert.AreEqual(StackItem.Null, parameter.ToStackItem());
        }

        [TestMethod]
        public void Primitives_Convert()
        {
            Assert.IsTrue(new ContractParameter { Type = ContractParameterType.Boolean, Value = true }.ToStackItem().GetBoolean());
            Assert.AreEqual(42, (int)new ContractParameter { Type = ContractParameterType.Integer, Value = (BigInteger)42 }.ToStackItem().GetInteger());
            Assert.AreEqual("hi", new ContractParameter { Type = ContractParameterType.String, Value = "hi" }.ToStackItem().GetString());
            Assert.IsTrue(new byte[] { 1, 2 }.AsSpan().SequenceEqual(
                new ContractParameter { Type = ContractParameterType.ByteArray, Value = new byte[] { 1, 2 } }.ToStackItem().GetSpan()));
            Assert.IsTrue(new byte[] { 9 }.AsSpan().SequenceEqual(
                new ContractParameter { Type = ContractParameterType.Signature, Value = new byte[] { 9 } }.ToStackItem().GetSpan()));
        }

        [TestMethod]
        public void HashAndKey_Convert()
        {
            var hash160 = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var hash256 = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000002");
            var key = ECCurve.Secp256r1.G;

            Assert.IsTrue(hash160.ToArray().AsSpan().SequenceEqual(
                new ContractParameter { Type = ContractParameterType.Hash160, Value = hash160 }.ToStackItem().GetSpan()));
            Assert.IsTrue(hash256.ToArray().AsSpan().SequenceEqual(
                new ContractParameter { Type = ContractParameterType.Hash256, Value = hash256 }.ToStackItem().GetSpan()));
            Assert.IsTrue(key.EncodePoint(true).AsSpan().SequenceEqual(
                new ContractParameter { Type = ContractParameterType.PublicKey, Value = key }.ToStackItem().GetSpan()));
        }

        [TestMethod]
        public void Array_And_Map_Convert()
        {
            var arrayParam = new ContractParameter
            {
                Type = ContractParameterType.Array,
                Value = new List<ContractParameter>
                {
                    new() { Type = ContractParameterType.Integer, Value = (BigInteger)1 },
                    new() { Type = ContractParameterType.String, Value = "x" }
                }
            };
            var arrayItem = (Array)arrayParam.ToStackItem();
            Assert.AreEqual(2, arrayItem.Count);
            Assert.AreEqual(1, (int)arrayItem[0].GetInteger());
            Assert.AreEqual("x", arrayItem[1].GetString());

            var mapParam = new ContractParameter
            {
                Type = ContractParameterType.Map,
                Value = new List<KeyValuePair<ContractParameter, ContractParameter>>
                {
                    new(
                        new ContractParameter { Type = ContractParameterType.Integer, Value = (BigInteger)7 },
                        new ContractParameter { Type = ContractParameterType.Boolean, Value = false })
                }
            };
            var mapItem = (Map)mapParam.ToStackItem();
            Assert.AreEqual(1, mapItem.Count);
            Assert.IsFalse(mapItem[(Integer)7].GetBoolean());
        }

        [TestMethod]
        public void UnsupportedType_Throws()
        {
            var parameter = new ContractParameter { Type = ContractParameterType.Void, Value = null };
            // null Value short-circuits to StackItem.Null; use non-null with unsupported type path via Any without value
            parameter = new ContractParameter { Type = ContractParameterType.InteropInterface, Value = new object() };
            Assert.ThrowsExactly<ArgumentException>(() => parameter.ToStackItem());
        }
    }
}
