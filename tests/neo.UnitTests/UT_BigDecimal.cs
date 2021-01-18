using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Numerics;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_BigDecimal
    {
        [TestMethod]
        public void TestChangeDecimals()
        {
            BigDecimal originalValue = new BigDecimal(new BigInteger(12300), 5);
            BigDecimal result1 = originalValue.ChangeDecimals(7);
            result1.Value.Should().Be(new BigInteger(1230000));
            result1.Decimals.Should().Be(7);
            BigDecimal result2 = originalValue.ChangeDecimals(3);
            result2.Value.Should().Be(new BigInteger(123));
            result2.Decimals.Should().Be(3);
            BigDecimal result3 = originalValue.ChangeDecimals(5);
            result3.Value.Should().Be(originalValue.Value);
            Action action = () => originalValue.ChangeDecimals(2);
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void TestBigDecimalConstructor()
        {
            BigDecimal value = new BigDecimal(new BigInteger(45600), 7);
            value.Value.Should().Be(new BigInteger(45600));
            value.Decimals.Should().Be(7);

            value = new BigDecimal(new BigInteger(0), 5);
            value.Value.Should().Be(new BigInteger(0));
            value.Decimals.Should().Be(5);

            value = new BigDecimal(new BigInteger(-10), 0);
            value.Value.Should().Be(new BigInteger(-10));
            value.Decimals.Should().Be(0);

            value = new BigDecimal(123.456789M);
            value.Value.Should().Be(new BigInteger(123456789));
            value.Decimals.Should().Be(6);

            value = new BigDecimal(123.45M);
            value.Value.Should().Be(new BigInteger(12345));
            value.Decimals.Should().Be(2);

            value = new BigDecimal(123M);
            value.Value.Should().Be(new BigInteger(123));
            value.Decimals.Should().Be(0);
        }

        [TestMethod]
        public void TestGetDecimals()
        {
            BigDecimal value = new BigDecimal(new BigInteger(45600), 7);
            value.Sign.Should().Be(1);
            value = new BigDecimal(new BigInteger(0), 5);
            value.Sign.Should().Be(0);
            value = new BigDecimal(new BigInteger(-10), 0);
            value.Sign.Should().Be(-1);
        }

        [TestMethod]
        public void TestGetSign()
        {
            BigDecimal value = new BigDecimal(new BigInteger(45600), 7);
            value.Sign.Should().Be(1);
            value = new BigDecimal(new BigInteger(0), 5);
            value.Sign.Should().Be(0);
            value = new BigDecimal(new BigInteger(-10), 0);
            value.Sign.Should().Be(-1);
        }

        [TestMethod]
        public void TestParse()
        {
            string s = "12345";
            byte decimals = 0;
            BigDecimal.Parse(s, decimals).Should().Be(new BigDecimal(new BigInteger(12345), 0));

            s = "abcdEfg";
            Action action = () => BigDecimal.Parse(s, decimals);
            action.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestToString()
        {
            BigDecimal value = new BigDecimal(new BigInteger(100000), 5);
            value.ToString().Should().Be("1");
            value = new BigDecimal(new BigInteger(123456), 5);
            value.ToString().Should().Be("1.23456");
        }

        [TestMethod]
        public void TestTryParse()
        {
            string s = "";
            byte decimals = 0;
            BigDecimal result;

            s = "12345";
            decimals = 0;
            BigDecimal.TryParse(s, decimals, out result).Should().BeTrue();
            result.Should().Be(new BigDecimal(new BigInteger(12345), 0));

            s = "12345E-5";
            decimals = 5;
            BigDecimal.TryParse(s, decimals, out result).Should().BeTrue();
            result.Should().Be(new BigDecimal(new BigInteger(12345), 5));

            s = "abcdEfg";
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "123.45";
            decimals = 2;
            BigDecimal.TryParse(s, decimals, out result).Should().BeTrue();
            result.Should().Be(new BigDecimal(new BigInteger(12345), 2));

            s = "123.45E-5";
            decimals = 7;
            BigDecimal.TryParse(s, decimals, out result).Should().BeTrue();
            result.Should().Be(new BigDecimal(new BigInteger(12345), 7));

            s = "12345E-5";
            decimals = 3;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "1.2345";
            decimals = 3;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "1.2345E-5";
            decimals = 3;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "12345";
            decimals = 3;
            BigDecimal.TryParse(s, decimals, out result).Should().BeTrue();
            result.Should().Be(new BigDecimal(new BigInteger(12345000), 3));

            s = "12345E-2";
            decimals = 3;
            BigDecimal.TryParse(s, decimals, out result).Should().BeTrue();
            result.Should().Be(new BigDecimal(new BigInteger(123450), 3));

            s = "123.45";
            decimals = 3;
            BigDecimal.TryParse(s, decimals, out result).Should().BeTrue();
            result.Should().Be(new BigDecimal(new BigInteger(123450), 3));

            s = "123.45E3";
            decimals = 3;
            BigDecimal.TryParse(s, decimals, out result).Should().BeTrue();
            result.Should().Be(new BigDecimal(new BigInteger(123450000), 3));

            s = "a456bcdfg";
            decimals = 0;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "a456bce-5";
            decimals = 5;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "a4.56bcd";
            decimals = 5;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "a4.56bce3";
            decimals = 2;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "a456bcd";
            decimals = 2;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "a456bcdE3";
            decimals = 2;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "a456b.cd";
            decimals = 5;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));

            s = "a456b.cdE3";
            decimals = 5;
            BigDecimal.TryParse(s, decimals, out result).Should().BeFalse();
            result.Should().Be(default(BigDecimal));
        }
    }
}
