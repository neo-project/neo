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
            var jString = new JString(str);
            Assert.IsTrue(jString.Equals(str));
            Assert.IsTrue(jString == str);
            Assert.IsTrue(jString != "hello world2");
        }
    }
}
