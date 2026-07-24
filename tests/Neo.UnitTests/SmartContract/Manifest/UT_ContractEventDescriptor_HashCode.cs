// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractEventDescriptor_HashCode.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractEventDescriptor_HashCode
    {
        [TestMethod]
        public void GetHashCode_EqualContent_SameHash_WhenEquals()
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

            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }
    }
}
