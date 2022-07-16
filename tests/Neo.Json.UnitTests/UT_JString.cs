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
            JString s = "Field2";
            TestEnum e = s.GetEnum<TestEnum>();
            Assert.AreEqual(TestEnum.Field2, e);

            s = "";
            e = s.AsEnum(TestEnum.Field1, false);
            Assert.AreEqual(TestEnum.Field1, e);
        }
    }
}
