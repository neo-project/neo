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
            var s = "hello world";
            var jstring = new JString(s);
            Assert.AreEqual(s, jstring.Value);
            Assert.ThrowsException<ArgumentNullException>(() => new JString(null));
        }

        [TestMethod]
        public void TestAsBoolean()
        {
            var s1 = "hello world";
            var s2 = "";
            var jstring1 = new JString(s1);
            var jstring2 = new JString(s2);
            Assert.AreEqual(true, jstring1.AsBoolean());
            Assert.AreEqual(false, jstring2.AsBoolean());
        }

        [TestMethod]
        public void TestAsNumber()
        {
            var s1 = "hello world";
            var s2 = "123";
            var s3 = "";
            var jstring1 = new JString(s1);
            var jstring2 = new JString(s2);
            var jstring3 = new JString(s3);
            Assert.AreEqual(double.NaN, jstring1.AsNumber());
            Assert.AreEqual(123, jstring2.AsNumber());
            Assert.AreEqual(0, jstring3.AsNumber());
        }

        [TestMethod]
        public void TestGetEnum()
        {
            JString s = "James";
            var woo = s.GetEnum<Woo>();
            Assert.AreEqual(Woo.James, woo);

            s = "";
            woo = s.AsEnum(Woo.Jerry, false);
            Assert.AreEqual(Woo.Jerry, woo);
        }
        [TestMethod]
        public void TestEqual()
        {
            var str = "hello world";
            var jString = new JString(str);
            Assert.IsTrue(jString.Equals(str));
            Assert.IsTrue(jString == str);
            Assert.IsTrue(jString != "hello world2");
        }
    }
}
