using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.IO;

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
        public void TestParse()
        {
            TextReader tr = new StringReader("\"hello world\"");
            String s = JString.Parse(tr).Value;
            Assert.AreEqual("hello world", s);

            tr = new StringReader("hello world");
            Assert.ThrowsException<FormatException>(() => JString.Parse(tr));

            tr = new StringReader("\"\\s\"");
            Assert.ThrowsException<FormatException>(() => JString.Parse(tr));

            tr = new StringReader("\"\\\"\\\\\\/\\b\\f\\n\\r\\t\"");
            s = JString.Parse(tr).Value;
            Assert.AreEqual("\"\\/\b\f\n\r\t", s);

            tr = new StringReader("\"\\u0030\"");
            s = JString.Parse(tr).Value;
            Assert.AreEqual("0", s);

            tr = new StringReader("\"a");
            Assert.ThrowsException<FormatException>(() => JString.Parse(tr));

            byte[] byteArray = new byte[] { 0x22, 0x01, 0x22 };
            tr = new StringReader(System.Text.Encoding.ASCII.GetString(byteArray));
            Assert.ThrowsException<FormatException>(() => JString.Parse(tr));
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
