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
        public void TestToTimestamp()
        {
            var num = new JNumber(1563173462);
            string.Format("{0:yyyy-MM-dd HH:mm:ss}", num.ToTimestamp()).Should().Be("2019-07-15 14:51:02");

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
    }
}