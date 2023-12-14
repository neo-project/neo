namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_JBoolean
    {
        private JBoolean jFalse;
        private JBoolean jTrue;

        [TestInitialize]
        public void SetUp()
        {
            jFalse = new JBoolean();
            jTrue = new JBoolean(true);
        }

        [TestMethod]
        public void TestAsNumber()
        {
            jFalse.AsNumber().Should().Be(0);
            jTrue.AsNumber().Should().Be(1);
        }

        [TestMethod]
        public void TestEqual()
        {
            Assert.IsTrue(jTrue.Equals(new JBoolean(true)));
            Assert.IsTrue(jTrue == new JBoolean(true));
            Assert.IsTrue(jFalse.Equals(new JBoolean()));
            Assert.IsTrue(jFalse == new JBoolean());
        }
    }
}
