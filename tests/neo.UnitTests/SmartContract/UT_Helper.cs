using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void TestGetContractHash()
        {
            var nef = new NefFile()
            {
                Compiler = "test",
                Tokens = Array.Empty<MethodToken>(),
                Script = new byte[] { 1, 2, 3 }
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);

            Assert.AreEqual("0x9b9628e4f1611af90e761eea8cc21372380c74b6", Neo.SmartContract.Helper.GetContractHash(UInt160.Zero, nef.CheckSum, new byte[0]).ToString());
            Assert.AreEqual("0x66eec404d86b918d084e62a29ac9990e3b6f4286", Neo.SmartContract.Helper.GetContractHash(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"), nef.CheckSum, new byte[0]).ToString());
        }

        [TestMethod]
        public void TestIsMultiSigContract()
        {
            var case1 = new byte[]
            {
                0, 2, 12, 33, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221,
                221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 12, 33, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 0,
            };
            Assert.IsFalse(case1.IsMultiSigContract());

            var case2 = new byte[]
            {
                18, 12, 33, 2, 111, 240, 59, 148, 146, 65, 206, 29, 173, 212, 53, 25, 230, 150, 14, 10, 133, 180, 26,
                105, 160, 92, 50, 129, 3, 170, 43, 206, 21, 148, 202, 22, 12, 33, 2, 111, 240, 59, 148, 146, 65, 206,
                29, 173, 212, 53, 25, 230, 150, 14, 10, 133, 180, 26, 105, 160, 92, 50, 129, 3, 170, 43, 206, 21, 148,
                202, 22, 18
            };
            Assert.IsFalse(case2.IsMultiSigContract());
        }
    }
}
