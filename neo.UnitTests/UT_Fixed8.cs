using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Fixed8
    {
        [TestMethod]
        public void Basic_equality()
        {
            var a = Fixed8.FromDecimal(1.23456789m);
            var b = Fixed8.Parse("1.23456789");
            a.Should().Be(b);
        }

        [TestMethod]
        public void Can_parse_exponent_notation()
        {
            Fixed8 expected = Fixed8.FromDecimal(1.23m);
            Fixed8 actual = Fixed8.Parse("1.23E-0");
            actual.Should().Be(expected);
        }
    }
}