using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using System;
using System.IO;

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
            action1.ShouldThrow<FormatException>();

            Action action2 = () => new JNumber(double.NegativeInfinity).AsString();
            action2.ShouldThrow<FormatException>();
        }

        [TestMethod]
        public void TestToTimestamp()
        {
            var num = new JNumber(1563173462);
            Action action = () => string.Format("{0:yyyy-MM-dd HH:mm:ss}", num.ToTimestamp());
            action.ShouldNotThrow<Exception>();

            Action action1 = () => minInt.ToTimestamp();
            action1.ShouldThrow<InvalidCastException>();

            Action action2 = () => maxInt.ToTimestamp();
            action2.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void TestTryGetEnum()
        {
            zero.TryGetEnum<Woo>().Should().Be(Woo.Tom);
            new JNumber(1).TryGetEnum<Woo>().Should().Be(Woo.Jerry);
            new JNumber(2).TryGetEnum<Woo>().Should().Be(Woo.James);
            new JNumber(3).TryGetEnum<Woo>().Should().Be(Woo.Tom);
        }

        [TestMethod]
        public void TestParse()
        {
            Action action1 = () => JNumber.Parse(new StringReader("100.a"));
            action1.ShouldThrow<FormatException>();

            Action action2 = () => JNumber.Parse(new StringReader("100.+"));
            action2.ShouldThrow<FormatException>();
        }
    }
}