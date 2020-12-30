using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_MethodToken
    {
        [TestMethod]
        public void TestSerialize()
        {
            var result = new MethodToken()
            {
                CallFlags = CallFlags.AllowCall,
                Hash = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"),
                Method = "myMethod",
                ParametersCount = 123,
                RVCount = 456
            };

            var copy = result.ToArray().AsSerializable<MethodToken>();

            Assert.AreEqual(CallFlags.AllowCall, copy.CallFlags);
            Assert.AreEqual("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01", copy.Hash.ToString());
            Assert.AreEqual("myMethod", copy.Method);
            Assert.AreEqual(123, copy.ParametersCount);
            Assert.AreEqual(456, copy.RVCount);
        }

        [TestMethod]
        public void TestSerializeErrors()
        {
            var result = new MethodToken()
            {
                CallFlags = (CallFlags)byte.MaxValue,
                Hash = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"),
                Method = "myLongMethod",
                ParametersCount = 123,
                RVCount = 456
            };

            Assert.ThrowsException<FormatException>(() => result.ToArray().AsSerializable<MethodToken>());

            result.CallFlags = CallFlags.All;
            result.Method += "-123123123123123123123123";
            Assert.ThrowsException<FormatException>(() => result.ToArray().AsSerializable<MethodToken>());
        }
    }
}
