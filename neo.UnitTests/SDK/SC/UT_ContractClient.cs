using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SDK.SC;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.UnitTests.SDK.SC
{
    [TestClass]
    public class UT_ContractClient
    {
        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestMethod]
        public void TestMakeScript()
        {
            byte[] testScript = ContractClient.MakeScript(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);

            Assert.AreEqual("14000000000000000000000000000000000000000051c10962616c616e63654f66142582d1b275e86c8f0e93a9b2facd5fdb760976a168627d5b52",
                            testScript.ToHexString());
        }

    }
}
