// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractEventDescriptor_Edges.cs file belongs to the neo project and is free
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
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    /// <summary>
    /// Edges not covered by UT_ContractEventDescriptor.TestFromJson.
    /// </summary>
    [TestClass]
    public class UT_ContractEventDescriptor_Edges
    {
        [TestMethod]
        public void StackItem_RoundTrip_WithParameters()
        {
            var original = new ContractEventDescriptor
            {
                Name = "Transfer",
                Parameters =
                [
                    new ContractParameterDefinition { Name = "from", Type = ContractParameterType.Hash160 },
                    new ContractParameterDefinition { Name = "to", Type = ContractParameterType.Hash160 },
                ]
            };

            var item = original.ToStackItem();
            var clone = new ContractEventDescriptor { Name = "", Parameters = [] };
            clone.FromStackItem(item);

            Assert.AreEqual("Transfer", clone.Name);
            Assert.HasCount(2, clone.Parameters);
            Assert.AreEqual("from", clone.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.Hash160, clone.Parameters[0].Type);
            Assert.AreEqual("to", clone.Parameters[1].Name);
        }

        [TestMethod]
        public void FromJson_EmptyName_Throws()
        {
            var json = new ContractEventDescriptor
            {
                Name = "ok",
                Parameters = []
            }.ToJson();
            json["name"] = "";
            Assert.ThrowsExactly<FormatException>(() => ContractEventDescriptor.FromJson(json));
        }

        [TestMethod]
        public void FromJson_DuplicateParameterNames_Throws()
        {
            var json = new ContractEventDescriptor
            {
                Name = "evt",
                Parameters =
                [
                    new ContractParameterDefinition { Name = "a", Type = ContractParameterType.Integer },
                    new ContractParameterDefinition { Name = "a", Type = ContractParameterType.Boolean },
                ]
            }.ToJson();
            Assert.ThrowsExactly<ArgumentException>(() => ContractEventDescriptor.FromJson(json));
        }

        [TestMethod]
        public void Equals_And_Operators()
        {
            var a = new ContractEventDescriptor
            {
                Name = "e",
                Parameters = [new ContractParameterDefinition { Name = "p", Type = ContractParameterType.Integer }]
            };
            var b = new ContractEventDescriptor
            {
                Name = "e",
                Parameters = [new ContractParameterDefinition { Name = "p", Type = ContractParameterType.Integer }]
            };
            var c = new ContractEventDescriptor { Name = "other", Parameters = [] };

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.IsFalse(a.Equals(c));
            Assert.IsTrue(a != c);
            Assert.IsFalse(a.Equals((ContractEventDescriptor)null));
            Assert.IsFalse(a.Equals((object)null));
            // GetHashCode is not asserted here: Parameters array identity makes Combine unstable
            // for equal content (see fix/contract-event-descriptor-hashcode if addressed).
        }
    }
}
