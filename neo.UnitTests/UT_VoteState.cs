using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_VoteState
    {
        VoteState uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new VoteState();
        }

        [TestMethod]
        public void PublicKeys_Get()
        {
            uut.PublicKeys.Should().BeNull();
        }

        [TestMethod]
        public void PublicKeys_Set()
        {
            ECPoint[] array = new ECPoint[] { new ECPoint(), new ECPoint(), new ECPoint(), new ECPoint(), new ECPoint() };
            uut.PublicKeys = array;
            uut.PublicKeys.Length.Should().Be(5);
        }

        [TestMethod]
        public void Count_Get()
        {
            uut.Count.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void Amount_Set()
        {
            Fixed8 val = Fixed8.FromDecimal(42);
            uut.Count = val;
            uut.Count.Should().Be(val);
        }
    }
}
