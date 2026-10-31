// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractBasicMethod.cs file belongs to the neo project and is free
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
    public class UT_ContractBasicMethod
    {
        [TestMethod]
        public void MethodNames_AreStable()
        {
            Assert.AreEqual("verify", ContractBasicMethod.Verify);
            Assert.AreEqual("_initialize", ContractBasicMethod.Initialize);
            Assert.AreEqual("_deploy", ContractBasicMethod.Deploy);
            Assert.AreEqual("update", ContractBasicMethod.Update);
            Assert.AreEqual("destroy", ContractBasicMethod.Destroy);
        }

        [TestMethod]
        public void ParameterCounts_MatchGuidelines()
        {
            Assert.AreEqual(-1, ContractBasicMethod.VerifyPCount);
            Assert.AreEqual(0, ContractBasicMethod.InitializePCount);
            Assert.AreEqual(2, ContractBasicMethod.DeployPCount);
            Assert.AreEqual(3, ContractBasicMethod.UpdatePCount);
            Assert.AreEqual(0, ContractBasicMethod.DestroyPCount);
        }
    }
}
