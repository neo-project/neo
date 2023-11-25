using Newtonsoft.Json;
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
        public void TestDefaultConstructor()
        {
            var defaultJBoolean = new JBoolean();
            defaultJBoolean.AsNumber().Should().Be(0);
        }

        [TestMethod]
        public void TestExplicitFalse()
        {
            var explicitFalse = new JBoolean(false);
            explicitFalse.AsNumber().Should().Be(0);
        }

        [TestMethod]
        public void TestNullJBoolean()
        {
            JBoolean nullJBoolean = null;
            Assert.ThrowsException<NullReferenceException>(() => nullJBoolean.AsNumber());
        }

        [TestMethod]
        public void TestConversionToOtherTypes()
        {
            Assert.AreEqual("true", jTrue.ToString());
            Assert.AreEqual("false", jFalse.ToString());
        }

        [TestMethod]
        public void TestComparisonsWithOtherBooleans()
        {
            Assert.IsTrue(jTrue.Equals(new JBoolean(true)));
            Assert.IsFalse(jFalse.Equals(new JBoolean()));
        }

        // [TestMethod]
        // public void TestTypeCasting()
        // {
        //     bool castedValue = (bool)jTrue;
        //     Assert.AreEqual(true, castedValue);
        // }

        [TestMethod]
        public void TestSerializationAndDeserialization()
        {
            string serialized = JsonConvert.SerializeObject(jTrue);
            var deserialized = JsonConvert.DeserializeObject<JBoolean>(serialized);
            Assert.AreEqual(jTrue, deserialized);
        }

        // [TestMethod]
        // public void TestInteractionWithLogicalOperators()
        // {
        //     var result = jTrue && new JBoolean(false);
        //     Assert.IsFalse(result.Value);
        // }
    }
}
