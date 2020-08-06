using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using System;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_ContractIdState
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new ContractIdState() { NextId = 1 };
            ((ISerializable)test).Size.Should().Be(4);

            test = new ContractIdState() { NextId = int.MaxValue };
            ((ISerializable)test).Size.Should().Be(4);
        }

        [TestMethod]
        public void Clone()
        {
            var test = new ContractIdState() { NextId = 1 };
            var clone = ((ICloneable<ContractIdState>)test).Clone();

            Assert.AreEqual(test.NextId, clone.NextId);

            clone = new ContractIdState() { NextId = 2 };
            ((ICloneable<ContractIdState>)clone).FromReplica(test);

            Assert.AreEqual(test.NextId, clone.NextId);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new ContractIdState() { NextId = int.MaxValue };
            var clone = test.ToArray().AsSerializable<ContractIdState>();

            Assert.AreEqual(test.NextId, clone.NextId);

            test = new ContractIdState() { NextId = -1 };
            Assert.ThrowsException<FormatException>(() => test.ToArray().AsSerializable<ContractIdState>());
        }
    }
}
