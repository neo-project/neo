using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.UnitTests.IO.Json
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
        public void TestParse()
        {
            TextReader tr1 = new StringReader("true");
            JBoolean ret1 = JBoolean.Parse(tr1);
            ret1.AsBoolean().Should().BeTrue();

            TextReader tr2 = new StringReader("false");
            JBoolean ret2 = JBoolean.Parse(tr2);
            ret2.AsBoolean().Should().BeFalse();

            TextReader tr3 = new StringReader("aaa");
            Action action = () => JBoolean.Parse(tr3);
            action.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestParseFalse()
        {
            TextReader tr1 = new StringReader("false");
            JBoolean ret1 = JBoolean.ParseFalse(tr1);
            ret1.AsBoolean().Should().BeFalse();

            TextReader tr2 = new StringReader("aaa");
            Action action = () => JBoolean.ParseFalse(tr2);
            action.Should().Throw<FormatException>();

            TextReader tr3 = new StringReader("\t\rfalse");
            JBoolean ret3 = JBoolean.ParseFalse(tr3);
            ret3.AsBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void TestParseTrue()
        {
            TextReader tr1 = new StringReader("true");
            JBoolean ret1 = JBoolean.ParseTrue(tr1);
            ret1.AsBoolean().Should().BeTrue();

            TextReader tr2 = new StringReader("aaa");
            Action action = () => JBoolean.ParseTrue(tr2);
            action.Should().Throw<FormatException>();

            TextReader tr3 = new StringReader(" true");
            JBoolean ret3 = JBoolean.ParseTrue(tr3);
            ret3.AsBoolean().Should().BeTrue();
        }
    }
}
