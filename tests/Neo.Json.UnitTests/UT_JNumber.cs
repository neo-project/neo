namespace Neo.Json.UnitTests
{
    enum Woo
    {
        Tom,
        Jerry,
        James
    }

    [TestClass]
    public class UT_JNumber
    {
        private JNumber maxInt;
        private JNumber minInt;
        private JNumber zero;

        [TestInitialize]
        public void SetUp()
        {
            maxInt = new JNumber(JNumber.MAX_SAFE_INTEGER);
            minInt = new JNumber(JNumber.MIN_SAFE_INTEGER);
            zero = new JNumber();
        }

        [TestMethod]
        public void TestAsBoolean()
        {
            maxInt.AsBoolean().Should().BeTrue();
            zero.AsBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void TestAsString()
        {
            Action action1 = () => new JNumber(double.PositiveInfinity).AsString();
            action1.Should().Throw<ArgumentException>();

            Action action2 = () => new JNumber(double.NegativeInfinity).AsString();
            action2.Should().Throw<ArgumentException>();

            Action action3 = () => new JNumber(double.NaN).AsString();
            action3.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestGetEnum()
        {
            zero.GetEnum<Woo>().Should().Be(Woo.Tom);
            new JNumber(1).GetEnum<Woo>().Should().Be(Woo.Jerry);
            new JNumber(2).GetEnum<Woo>().Should().Be(Woo.James);
            new JNumber(3).AsEnum<Woo>().Should().Be(Woo.Tom);

            Action action = () => new JNumber(3).GetEnum<Woo>();
            action.Should().Throw<InvalidCastException>();
        }
    }
}
