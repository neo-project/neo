// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractMethodDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractMethodDescriptor
    {
        [TestMethod]
        public void FromJson_ToJson_RoundTrip()
        {
            var original = new ContractMethodDescriptor
            {
                Name = "transfer",
                Parameters =
                [
                    new ContractParameterDefinition { Name = "to", Type = ContractParameterType.Hash160 },
                    new ContractParameterDefinition { Name = "amount", Type = ContractParameterType.Integer }
                ],
                ReturnType = ContractParameterType.Boolean,
                Offset = 12,
                Safe = true
            };

            var clone = ContractMethodDescriptor.FromJson(original.ToJson());
            Assert.AreEqual(original.Name, clone.Name);
            Assert.HasCount(2, clone.Parameters);
            Assert.AreEqual(ContractParameterType.Boolean, clone.ReturnType);
            Assert.AreEqual(12, clone.Offset);
            Assert.IsTrue(clone.Safe);
        }

        [TestMethod]
        public void StackItem_RoundTrip()
        {
            var original = new ContractMethodDescriptor
            {
                Name = "balanceOf",
                Parameters = [new ContractParameterDefinition { Name = "account", Type = ContractParameterType.Hash160 }],
                ReturnType = ContractParameterType.Integer,
                Offset = 0,
                Safe = true
            };

            var clone = new ContractMethodDescriptor
            {
                Name = "",
                Parameters = [],
                ReturnType = ContractParameterType.Void,
                Offset = -1,
                Safe = false
            };
            clone.FromStackItem(original.ToStackItem());

            Assert.AreEqual("balanceOf", clone.Name);
            Assert.HasCount(1, clone.Parameters);
            Assert.AreEqual(ContractParameterType.Integer, clone.ReturnType);
            Assert.AreEqual(0, clone.Offset);
            Assert.IsTrue(clone.Safe);
        }

        [TestMethod]
        public void FromJson_EmptyName_Throws()
        {
            var json = new ContractMethodDescriptor
            {
                Name = "x",
                Parameters = [],
                ReturnType = ContractParameterType.Void,
                Offset = 0,
                Safe = false
            }.ToJson();
            json["name"] = "";
            Assert.ThrowsExactly<FormatException>(() => ContractMethodDescriptor.FromJson(json));
        }

        [TestMethod]
        public void FromJson_NegativeOffset_Throws()
        {
            var json = new ContractMethodDescriptor
            {
                Name = "x",
                Parameters = [],
                ReturnType = ContractParameterType.Any,
                Offset = 0,
                Safe = false
            }.ToJson();
            json["offset"] = -1;
            Assert.ThrowsExactly<FormatException>(() => ContractMethodDescriptor.FromJson(json));
        }
    }
}
