// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractParameterDefinition.cs file belongs to the neo project and is free
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
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractParameterDefinition
    {
        [TestMethod]
        public void FromJson_ToJson_RoundTrip()
        {
            var json = new JObject
            {
                ["name"] = "amount",
                ["type"] = nameof(ContractParameterType.Integer)
            };

            var def = ContractParameterDefinition.FromJson(json);
            Assert.AreEqual("amount", def.Name);
            Assert.AreEqual(ContractParameterType.Integer, def.Type);

            var back = def.ToJson();
            Assert.AreEqual("amount", back["name"].GetString());
            Assert.AreEqual(nameof(ContractParameterType.Integer), back["type"].GetString());
        }

        [TestMethod]
        public void FromJson_EmptyName_Throws()
        {
            var json = new JObject
            {
                ["name"] = "",
                ["type"] = nameof(ContractParameterType.Boolean)
            };
            Assert.ThrowsExactly<FormatException>(() => ContractParameterDefinition.FromJson(json));
        }

        [TestMethod]
        public void FromJson_VoidType_Throws()
        {
            var json = new JObject
            {
                ["name"] = "x",
                ["type"] = nameof(ContractParameterType.Void)
            };
            Assert.ThrowsExactly<FormatException>(() => ContractParameterDefinition.FromJson(json));
        }

        [TestMethod]
        public void StackItem_RoundTrip()
        {
            var original = new ContractParameterDefinition
            {
                Name = "data",
                Type = ContractParameterType.ByteArray
            };

            var item = original.ToStackItem();
            Assert.IsInstanceOfType<Struct>(item);

            var clone = new ContractParameterDefinition { Name = "", Type = ContractParameterType.Any };
            ((IInteroperable)clone).FromStackItem(item);

            Assert.AreEqual(original.Name, clone.Name);
            Assert.AreEqual(original.Type, clone.Type);
        }

        [TestMethod]
        public void Equals_And_Operators()
        {
            var a = new ContractParameterDefinition { Name = "n", Type = ContractParameterType.Hash160 };
            var b = new ContractParameterDefinition { Name = "n", Type = ContractParameterType.Hash160 };
            var c = new ContractParameterDefinition { Name = "n", Type = ContractParameterType.Hash256 };

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.IsFalse(a.Equals(c));
            Assert.IsTrue(a != c);
            Assert.IsFalse(a.Equals((ContractParameterDefinition)null));
            Assert.IsFalse(a.Equals((object)null));
            Assert.IsFalse(a.Equals("x"));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.IsFalse(a == null);
            Assert.IsTrue(a != null);
        }
    }
}
