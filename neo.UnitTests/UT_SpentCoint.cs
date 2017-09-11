using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_SpentCoint
    {
        SpentCoin uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new SpentCoin();
        }

        [TestMethod]
        public void Output_Get()
        {
            uut.Output.Should().BeNull();
        }

        [TestMethod]
        public void Output_Set()
        {
            TransactionOutput val = new TransactionOutput();
            uut.Output = val;
            uut.Output.Should().Be(val);
        }

        [TestMethod]
        public void StartHeight_Get()
        {
            uut.StartHeight.Should().Be(0u);
        }

        [TestMethod]
        public void StartHeight_Set()
        {
            uint val = 42;
            uut.StartHeight = val;
            uut.StartHeight.Should().Be(val);
        }

        [TestMethod]
        public void EndHeight_Get()
        {
            uut.EndHeight.Should().Be(0u);
        }

        [TestMethod]
        public void EndHeight_Set()
        {
            uint val = 42;
            uut.EndHeight = val;
            uut.EndHeight.Should().Be(val);
        }

        [TestMethod]
        public void Value_Get()
        {
            TransactionOutput val = new TransactionOutput();
            val.Value = Fixed8.FromDecimal(42);
            uut.Output = val;
            uut.Value.Should().Be(val.Value);
        }
    }
}
