using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_JString
    {
        [TestMethod]
        public void TestConstructor()
        {
            String s = "hello world";
            JString jstring = new JString(s);
            Assert.AreEqual(s, jstring.Value);
            Assert.ThrowsException<ArgumentNullException>(() => new JString(null));
        }

        [TestMethod]
        public void TestAsBoolean()
        {
            String s1 = "hello world";
            String s2 = "";
            JString jstring1 = new JString(s1);
            JString jstring2 = new JString(s2);
            Assert.AreEqual(true, jstring1.AsBoolean());
            Assert.AreEqual(false, jstring2.AsBoolean());
        }

        [TestMethod]
        public void TestTryGetEnum()
        {
            JString s = new JString("Signature");
            ContractParameterType cp = s.TryGetEnum<ContractParameterType>(ContractParameterType.Void, false);
            Assert.AreEqual(ContractParameterType.Signature, cp);

            s = new JString("");
            cp = s.TryGetEnum<ContractParameterType>(ContractParameterType.Void, false);
            Assert.AreEqual(ContractParameterType.Void, cp);
        }
    }
}
