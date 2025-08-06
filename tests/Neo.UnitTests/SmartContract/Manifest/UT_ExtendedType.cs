// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ExtendedType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ExtendedType
    {
        [TestMethod]
        public void Equals_SameValues_ShouldBeTrue()
        {
            var a = new ExtendedType
            {
                Type = ContractParameterType.Array,
                NamedType = "MyType",
                Length = 10,
                ForbidNull = true,
                Interface = Nep25Interface.IIterator,
                Key = Nep25Key.String
            };

            var b = new ExtendedType
            {
                Type = ContractParameterType.Array,
                NamedType = "MyType",
                Length = 10,
                ForbidNull = true,
                Interface = Nep25Interface.IIterator,
                Key = Nep25Key.String
            };

            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void Equals_DifferentValues_ShouldBeFalse()
        {
            var a = new ExtendedType
            {
                Type = ContractParameterType.Integer,
                NamedType = "TypeA",
                Length = 5,
                ForbidNull = false,
                Interface = Nep25Interface.IIterator,
                Key = Nep25Key.String
            };

            var b = new ExtendedType
            {
                Type = ContractParameterType.String,
                NamedType = "TypeB",
                Length = 8,
                ForbidNull = true,
                Interface = null,
                Key = Nep25Key.PublicKey
            };

            Assert.IsFalse(a.Equals(b));
            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void Equals_NullOrOtherType_ShouldBeFalse()
        {
            var a = new ExtendedType
            {
                Type = ContractParameterType.Boolean
            };

            Assert.IsFalse(a.Equals(null));
            Assert.IsFalse(a.Equals("not an ExtendedType"));
        }

        [TestMethod]
        public void FromStackItem_ShouldHandleNullFields()
        {
            var refCounter = new ReferenceCounter();
            var structItem = new Struct(refCounter)
            {
                (byte)ContractParameterType.String, // Type
                StackItem.Null, // NamedType
                StackItem.Null, // Length
                StackItem.Null, // ForbidNull
                StackItem.Null, // Interface
                StackItem.Null, // Key
                StackItem.Null, // Value
                StackItem.Null  // Fields
            };

            var extended = new ExtendedType();
            extended.FromStackItem(structItem, 0);

            Assert.AreEqual(ContractParameterType.String, extended.Type);
            Assert.IsNull(extended.NamedType);
            Assert.IsNull(extended.Length);
            Assert.IsNull(extended.ForbidNull);
            Assert.IsNull(extended.Interface);
            Assert.IsNull(extended.Key);
        }

        [TestMethod]
        public void ToStackItem_ShouldProduceNullFields()
        {
            var extended = new ExtendedType
            {
                Type = ContractParameterType.String,
                NamedType = null,
                Length = null,
                ForbidNull = null,
                Interface = null,
                Key = null,
                Value = null,
                Fields = null
            };

            var refCounter = new ReferenceCounter();
            var result = ((IInteroperable)extended).ToStackItem(refCounter) as Struct;

            Assert.IsNotNull(result);
            Assert.AreEqual(8, result.Count);
            Assert.AreEqual((byte)ContractParameterType.String, result[0].GetInteger());
            Assert.IsTrue(result[1].IsNull);
            Assert.IsTrue(result[2].IsNull);
            Assert.IsTrue(result[3].IsNull);
            Assert.IsTrue(result[4].IsNull);
            Assert.IsTrue(result[5].IsNull);
            Assert.IsTrue(result[6].IsNull);
            Assert.IsTrue(result[7].IsNull);
        }

        [TestMethod]
        public void FromJson_ValidInput_ShouldParseCorrectly()
        {
            var json = new JObject();
            json["type"] = "Integer";
            json["namedtype"] = "MyType";
            json["length"] = 123;
            json["forbidnull"] = true;
            json["interface"] = "IIterator";
            json["key"] = "String";

            var result = ExtendedType.FromJson(json);

            Assert.AreEqual(ContractParameterType.Integer, result.Type);
            Assert.AreEqual("MyType", result.NamedType);
            Assert.AreEqual(123, result.Length);
            Assert.IsTrue(result.ForbidNull);
            Assert.AreEqual(Nep25Interface.IIterator, result.Interface);
            Assert.AreEqual(Nep25Key.String, result.Key);
        }

        [TestMethod]
        public void ToJson_ShouldSerializeCorrectly()
        {
            var ext = new ExtendedType
            {
                Type = ContractParameterType.String,
                NamedType = "Test.Name",
                Length = 50,
                ForbidNull = true,
                Interface = Nep25Interface.IIterator,
                Key = Nep25Key.String
            };

            var json = ext.ToJson();

            Assert.AreEqual("String", json["type"]?.AsString());
            Assert.AreEqual("Test.Name", json["namedtype"]?.AsString());
            Assert.AreEqual(50, json["length"]?.AsNumber());
            Assert.IsTrue(json["forbidnull"]?.AsBoolean());
            Assert.AreEqual("IIterator", json["interface"]?.AsString());
            Assert.AreEqual("String", json["key"]?.AsString());
        }

        [TestMethod]
        public void FromJson_ToJson_Roundtrip()
        {
            var json = new JObject();
            json["type"] = "Boolean";
            json["namedtype"] = "Type.A";
            json["length"] = 32;
            json["forbidnull"] = false;
            json["interface"] = "IIterator";
            json["key"] = "PublicKey";

            var ext = ExtendedType.FromJson(json);
            var output = ext.ToJson();

            Assert.AreEqual("Boolean", output["type"]?.AsString());
            Assert.AreEqual("Type.A", output["namedtype"]?.AsString());
            Assert.AreEqual(32, output["length"]?.AsNumber());
            Assert.IsFalse(output["forbidnull"]?.AsBoolean());
            Assert.AreEqual("IIterator", output["interface"]?.AsString());
            Assert.AreEqual("PublicKey", output["key"]?.AsString());
        }

        [TestMethod]
        public void FromJson_InvalidType_ShouldThrow()
        {
            var json = new JObject();
            json["type"] = "InvalidType";
            Assert.ThrowsExactly<ArgumentException>(() => ExtendedType.FromJson(json));
        }

        [TestMethod]
        public void FromJson_NegativeLength_ShouldThrow()
        {
            var json = new JObject();
            json["type"] = "ByteArray";
            json["length"] = -1;
            Assert.ThrowsExactly<FormatException>(() => ExtendedType.FromJson(json));
        }

        [TestMethod]
        public void FromJson_InvalidInterface_ShouldThrow()
        {
            var json = new JObject();
            json["type"] = "InteropInterface";
            json["interface"] = "BadInterface";
            Assert.ThrowsExactly<FormatException>(() => ExtendedType.FromJson(json));
        }

        [TestMethod]
        public void FromJson_InvalidKey_ShouldThrow()
        {
            var json = new JObject();
            json["type"] = "Map";
            json["key"] = "BadKey";
            Assert.ThrowsExactly<FormatException>(() => ExtendedType.FromJson(json));
        }

        [TestMethod]
        public void ToStackItem_And_FromStackItem_ShouldRoundtrip()
        {
            var original = new ExtendedType
            {
                Type = ContractParameterType.Map,
                NamedType = "MapType",
                Length = 20,
                ForbidNull = true,
                Interface = Nep25Interface.IIterator,
                Key = Nep25Key.Hash160
            };

            var refCounter = new ReferenceCounter();
            var structItem = new Struct(refCounter);
            var stackItem = original.ToStackItem(refCounter, structItem);

            var copy = new ExtendedType();
            copy.FromStackItem((VM.Types.Array)stackItem, 0);

            Assert.AreEqual(original.Type, copy.Type);
            Assert.AreEqual(original.NamedType, copy.NamedType);
            Assert.AreEqual(original.Length, copy.Length);
            Assert.AreEqual(original.ForbidNull, copy.ForbidNull);
            Assert.AreEqual(original.Interface, copy.Interface);
            Assert.AreEqual(original.Key, copy.Key);
        }
    }
}
