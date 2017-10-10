using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_TransactionResult
    {
        TransactionResult uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new TransactionResult();
        }

        [TestMethod]
        public void AssetId_Get()
        {
            uut.AssetId.Should().BeNull();
        }

        [TestMethod]
        public void AssetId_Set()
        {
            UInt256 val = new UInt256(TestUtils.GetByteArray(32, 0x42));
            uut.AssetId = val;
            uut.AssetId.Should().Be(val);
        }

        [TestMethod]
        public void Amount_Get()
        {
            uut.Amount.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void Amount_Set()
        {
            Fixed8 val = Fixed8.FromDecimal(42);
            uut.Amount = val;
            uut.Amount.Should().Be(val);
        }
    }
}
