// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ContractEventAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_ContractEventAttribute
    {
        [TestMethod]
        public void TestConstructorOneArg()
        {
            var arg = new ContractEventAttribute(Hardfork.HF_Basilisk, 0, "1", "a1", ContractParameterType.String);

            Assert.AreEqual(Hardfork.HF_Basilisk, arg.ActiveIn);
            Assert.AreEqual(0, arg.Order);
            Assert.AreEqual("1", arg.Descriptor.Name);
            Assert.AreEqual(1, arg.Descriptor.Parameters.Length);
            Assert.AreEqual("a1", arg.Descriptor.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.String, arg.Descriptor.Parameters[0].Type);

            arg = new ContractEventAttribute(1, "1", "a1", ContractParameterType.String);

            Assert.IsNull(arg.ActiveIn);
            Assert.AreEqual(1, arg.Order);
            Assert.AreEqual("1", arg.Descriptor.Name);
            Assert.AreEqual(1, arg.Descriptor.Parameters.Length);
            Assert.AreEqual("a1", arg.Descriptor.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.String, arg.Descriptor.Parameters[0].Type);
        }

        [TestMethod]
        public void TestConstructorTwoArg()
        {
            var arg = new ContractEventAttribute(Hardfork.HF_Basilisk, 0, "2",
                "a1", ContractParameterType.String,
                "a2", ContractParameterType.Integer);

            Assert.AreEqual(Hardfork.HF_Basilisk, arg.ActiveIn);
            Assert.AreEqual(0, arg.Order);
            Assert.AreEqual("2", arg.Descriptor.Name);
            Assert.AreEqual(2, arg.Descriptor.Parameters.Length);
            Assert.AreEqual("a1", arg.Descriptor.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.String, arg.Descriptor.Parameters[0].Type);
            Assert.AreEqual("a2", arg.Descriptor.Parameters[1].Name);
            Assert.AreEqual(ContractParameterType.Integer, arg.Descriptor.Parameters[1].Type);

            arg = new ContractEventAttribute(1, "2",
                "a1", ContractParameterType.String,
                "a2", ContractParameterType.Integer);

            Assert.IsNull(arg.ActiveIn);
            Assert.AreEqual(1, arg.Order);
            Assert.AreEqual("2", arg.Descriptor.Name);
            Assert.AreEqual(2, arg.Descriptor.Parameters.Length);
            Assert.AreEqual("a1", arg.Descriptor.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.String, arg.Descriptor.Parameters[0].Type);
            Assert.AreEqual("a2", arg.Descriptor.Parameters[1].Name);
            Assert.AreEqual(ContractParameterType.Integer, arg.Descriptor.Parameters[1].Type);
        }

        [TestMethod]
        public void TestConstructorThreeArg()
        {
            var arg = new ContractEventAttribute(Hardfork.HF_Basilisk, 0, "3",
                "a1", ContractParameterType.String,
                "a2", ContractParameterType.Integer,
                "a3", ContractParameterType.Boolean);

            Assert.AreEqual(Hardfork.HF_Basilisk, arg.ActiveIn);
            Assert.AreEqual(0, arg.Order);
            Assert.AreEqual("3", arg.Descriptor.Name);
            Assert.AreEqual(3, arg.Descriptor.Parameters.Length);
            Assert.AreEqual("a1", arg.Descriptor.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.String, arg.Descriptor.Parameters[0].Type);
            Assert.AreEqual("a2", arg.Descriptor.Parameters[1].Name);
            Assert.AreEqual(ContractParameterType.Integer, arg.Descriptor.Parameters[1].Type);
            Assert.AreEqual("a3", arg.Descriptor.Parameters[2].Name);
            Assert.AreEqual(ContractParameterType.Boolean, arg.Descriptor.Parameters[2].Type);

            arg = new ContractEventAttribute(1, "3",
                "a1", ContractParameterType.String,
                "a2", ContractParameterType.Integer,
                "a3", ContractParameterType.Boolean);

            Assert.IsNull(arg.ActiveIn);
            Assert.AreEqual(1, arg.Order);
            Assert.AreEqual("3", arg.Descriptor.Name);
            Assert.AreEqual(3, arg.Descriptor.Parameters.Length);
            Assert.AreEqual("a1", arg.Descriptor.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.String, arg.Descriptor.Parameters[0].Type);
            Assert.AreEqual("a2", arg.Descriptor.Parameters[1].Name);
            Assert.AreEqual(ContractParameterType.Integer, arg.Descriptor.Parameters[1].Type);
            Assert.AreEqual("a3", arg.Descriptor.Parameters[2].Name);
            Assert.AreEqual(ContractParameterType.Boolean, arg.Descriptor.Parameters[2].Type);
        }

        [TestMethod]
        public void TestConstructorFourArg()
        {
            var arg = new ContractEventAttribute(Hardfork.HF_Basilisk, 0, "4",
                "a1", ContractParameterType.String,
                "a2", ContractParameterType.Integer,
                "a3", ContractParameterType.Boolean,
                "a4", ContractParameterType.Array);

            Assert.AreEqual(Hardfork.HF_Basilisk, arg.ActiveIn);
            Assert.AreEqual(0, arg.Order);
            Assert.AreEqual("4", arg.Descriptor.Name);
            Assert.AreEqual(4, arg.Descriptor.Parameters.Length);
            Assert.AreEqual("a1", arg.Descriptor.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.String, arg.Descriptor.Parameters[0].Type);
            Assert.AreEqual("a2", arg.Descriptor.Parameters[1].Name);
            Assert.AreEqual(ContractParameterType.Integer, arg.Descriptor.Parameters[1].Type);
            Assert.AreEqual("a3", arg.Descriptor.Parameters[2].Name);
            Assert.AreEqual(ContractParameterType.Boolean, arg.Descriptor.Parameters[2].Type);
            Assert.AreEqual("a4", arg.Descriptor.Parameters[3].Name);
            Assert.AreEqual(ContractParameterType.Array, arg.Descriptor.Parameters[3].Type);

            arg = new ContractEventAttribute(1, "4",
                "a1", ContractParameterType.String,
                "a2", ContractParameterType.Integer,
                "a3", ContractParameterType.Boolean,
                "a4", ContractParameterType.Array);

            Assert.IsNull(arg.ActiveIn);
            Assert.AreEqual(1, arg.Order);
            Assert.AreEqual("4", arg.Descriptor.Name);
            Assert.AreEqual(4, arg.Descriptor.Parameters.Length);
            Assert.AreEqual("a1", arg.Descriptor.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.String, arg.Descriptor.Parameters[0].Type);
            Assert.AreEqual("a2", arg.Descriptor.Parameters[1].Name);
            Assert.AreEqual(ContractParameterType.Integer, arg.Descriptor.Parameters[1].Type);
            Assert.AreEqual("a3", arg.Descriptor.Parameters[2].Name);
            Assert.AreEqual(ContractParameterType.Boolean, arg.Descriptor.Parameters[2].Type);
            Assert.AreEqual("a4", arg.Descriptor.Parameters[3].Name);
            Assert.AreEqual(ContractParameterType.Array, arg.Descriptor.Parameters[3].Type);
        }
    }
}
