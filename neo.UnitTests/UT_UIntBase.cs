using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_UIntBenchmarks
    {
        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestMethod]
        public void Test_UInt160_Parse()
        {
            string uint160strbig = "0x0001020304050607080900010203040506070809";
            UInt160 num1 = UInt160.Parse(uint160strbig);
            num1.ToString().Should().Be("0x0001020304050607080900010203040506070809");

            string uint160strbig2 = "0X0001020304050607080900010203040506070809";
            UInt160 num2 = UInt160.Parse(uint160strbig2);
            num2.ToString().Should().Be("0x0001020304050607080900010203040506070809");
        }
    }
}
