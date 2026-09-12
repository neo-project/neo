// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractAbi.cs file belongs to the neo project and is free
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
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractAbi
    {
        private static ContractMethodDescriptor Method(string name, int pcount, int offset = 0)
        {
            var parameters = new ContractParameterDefinition[pcount];
            for (var i = 0; i < pcount; i++)
            {
                parameters[i] = new ContractParameterDefinition
                {
                    Name = $"p{i}",
                    Type = ContractParameterType.Integer
                };
            }
            return new ContractMethodDescriptor
            {
                Name = name,
                Parameters = parameters,
                ReturnType = ContractParameterType.Void,
                Offset = offset,
                Safe = true
            };
        }

        private static ContractAbi SampleAbi()
        {
            return new ContractAbi
            {
                Methods =
                [
                    Method("foo", 0, 0),
                    Method("foo", 1, 10),
                    Method("bar", 2, 20)
                ],
                Events =
                [
                    new ContractEventDescriptor
                    {
                        Name = "onTransfer",
                        Parameters =
                        [
                            new ContractParameterDefinition { Name = "from", Type = ContractParameterType.Hash160 }
                        ]
                    }
                ]
            };
        }

        [TestMethod]
        public void GetMethod_ByNameAndCount_ReturnsMatch()
        {
            var abi = SampleAbi();
            var m0 = abi.GetMethod("foo", 0);
            var m1 = abi.GetMethod("foo", 1);
            Assert.IsNotNull(m0);
            Assert.IsNotNull(m1);
            Assert.AreEqual(0, m0.Offset);
            Assert.AreEqual(10, m1.Offset);
            Assert.IsNull(abi.GetMethod("foo", 2));
        }

        [TestMethod]
        public void GetMethod_WithMinusOne_ReturnsFirstByName()
        {
            var abi = SampleAbi();
            var method = abi.GetMethod("foo", -1);
            Assert.IsNotNull(method);
            Assert.AreEqual("foo", method.Name);
            Assert.AreEqual(0, method.Parameters.Length);
        }

        [TestMethod]
        public void GetMethod_InvalidPcount_Throws()
        {
            var abi = SampleAbi();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => abi.GetMethod("foo", -2));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => abi.GetMethod("foo", ushort.MaxValue + 1));
        }

        [TestMethod]
        public void ToJson_And_FromJson_RoundTrip()
        {
            var abi = SampleAbi();
            var json = abi.ToJson();
            var restored = ContractAbi.FromJson(json);
            Assert.AreEqual(abi.Methods.Length, restored.Methods.Length);
            Assert.AreEqual(abi.Events.Length, restored.Events.Length);
            Assert.AreEqual("foo", restored.Methods[0].Name);
            Assert.AreEqual("onTransfer", restored.Events[0].Name);
        }

        [TestMethod]
        public void FromJson_EmptyMethods_Throws()
        {
            var json = new JObject
            {
                ["methods"] = new JArray(),
                ["events"] = new JArray()
            };
            Assert.ThrowsExactly<FormatException>(() => ContractAbi.FromJson(json));
        }

        [TestMethod]
        public void StackItem_RoundTrip()
        {
            var abi = SampleAbi();
            var item = abi.ToStackItem();
            var restored = new ContractAbi { Methods = [], Events = [] };
            ((IInteroperable)restored).FromStackItem(item);
            Assert.AreEqual(3, restored.Methods.Length);
            Assert.AreEqual(1, restored.Events.Length);
            Assert.AreEqual("bar", restored.Methods[2].Name);
            Assert.IsInstanceOfType<Struct>(item);
            Assert.IsInstanceOfType<Array>(((Struct)item)[0]);
        }
    }
}
