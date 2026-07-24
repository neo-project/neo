// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractParameterType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ContractParameterType
    {
        [TestMethod]
        public void Values_MatchSpecification()
        {
            Assert.AreEqual(0x00, (byte)ContractParameterType.Any);
            Assert.AreEqual(0x10, (byte)ContractParameterType.Boolean);
            Assert.AreEqual(0x11, (byte)ContractParameterType.Integer);
            Assert.AreEqual(0x12, (byte)ContractParameterType.ByteArray);
            Assert.AreEqual(0x13, (byte)ContractParameterType.String);
            Assert.AreEqual(0x14, (byte)ContractParameterType.Hash160);
            Assert.AreEqual(0x15, (byte)ContractParameterType.Hash256);
            Assert.AreEqual(0x16, (byte)ContractParameterType.PublicKey);
            Assert.AreEqual(0x17, (byte)ContractParameterType.Signature);
            Assert.AreEqual(0x20, (byte)ContractParameterType.Array);
            Assert.AreEqual(0x22, (byte)ContractParameterType.Map);
            Assert.AreEqual(0x30, (byte)ContractParameterType.InteropInterface);
            Assert.AreEqual(0xff, (byte)ContractParameterType.Void);
        }

        [TestMethod]
        public void AllValues_AreDefined()
        {
            foreach (ContractParameterType t in System.Enum.GetValues<ContractParameterType>())
            {
                Assert.IsTrue(System.Enum.IsDefined(t));
            }
        }
    }
}
