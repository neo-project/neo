// Copyright (C) 2015-2024 The Neo Project.
//
// UT_JString.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_JString
    {
        [TestMethod]
        public void TestConstructor()
        {
            string s = "hello world";
            JString jstring = new JString(s);
            Assert.AreEqual(s, jstring.Value);
            Assert.ThrowsException<ArgumentNullException>(() => new JString(null));
        }

        [TestMethod]
        public void TestAsBoolean()
        {
            string s1 = "hello world";
            string s2 = "";
            JString jstring1 = new JString(s1);
            JString jstring2 = new JString(s2);
            Assert.AreEqual(true, jstring1.AsBoolean());
            Assert.AreEqual(false, jstring2.AsBoolean());
        }

        [TestMethod]
        public void TestAsNumber()
        {
            string s1 = "hello world";
            string s2 = "123";
            string s3 = "";
            JString jstring1 = new JString(s1);
            JString jstring2 = new JString(s2);
            JString jstring3 = new JString(s3);
            Assert.AreEqual(double.NaN, jstring1.AsNumber());
            Assert.AreEqual(123, jstring2.AsNumber());
            Assert.AreEqual(0, jstring3.AsNumber());
        }

        [TestMethod]
        public void TestGetEnum()
        {
            JString s = "James";
            Woo woo = s.GetEnum<Woo>();
            Assert.AreEqual(Woo.James, woo);

            s = "";
            woo = s.AsEnum(Woo.Jerry, false);
            Assert.AreEqual(Woo.Jerry, woo);
        }
        [TestMethod]
        public void TestEqual()
        {
            var str = "hello world";
            var str2 = "hello world2";
            var jString = new JString(str);
            var jString2 = new JString(str2);

            Assert.IsTrue(jString == str);
            Assert.IsFalse(jString == null);
            Assert.IsTrue(jString != str2);
            Assert.IsFalse(jString == str2);

            Assert.AreEqual(str, jString.GetString());
            Assert.IsTrue(jString.Equals(str));
            Assert.IsFalse(jString.Equals(jString2));
            Assert.IsFalse(jString.Equals(null));
            Assert.IsFalse(jString.Equals(123));
            var reference = jString;
            Assert.IsTrue(jString.Equals(reference));
        }
    }
}
