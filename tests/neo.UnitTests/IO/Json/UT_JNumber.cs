using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using System;

namespace Neo.UnitTests.IO.Json
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
            action1.Should().Throw<FormatException>();

            Action action2 = () => new JNumber(double.NegativeInfinity).AsString();
            action2.Should().Throw<FormatException>();

            Action action3 = () => new JNumber(double.NaN).AsString();
            action3.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestTryGetEnum()
        {
            zero.TryGetEnum<Woo>().Should().Be(Woo.Tom);
            new JNumber(1).TryGetEnum<Woo>().Should().Be(Woo.Jerry);
            new JNumber(2).TryGetEnum<Woo>().Should().Be(Woo.James);
            new JNumber(3).TryGetEnum<Woo>().Should().Be(Woo.Tom);
        }
    }
}
