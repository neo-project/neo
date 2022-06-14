using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_StorageKey
    {
        [TestMethod]
        public void Id_Get()
        {
            var uut = new StorageKey { Id = 1, Key = new byte[] { 0x01 } };
            uut.Id.Should().Be(1);
        }

        [TestMethod]
        public void Id_Set()
        {
            int val = 1;
            StorageKey uut = new() { Id = val };
            uut.Id.Should().Be(val);
        }

        [TestMethod]
        public void Key_Set()
        {
            byte[] val = new byte[] { 0x42, 0x32 };
            StorageKey uut = new() { Key = val };
            uut.Key.Length.Should().Be(2);
            uut.Key.Span[0].Should().Be(val[0]);
            uut.Key.Span[1].Should().Be(val[1]);
        }

        [TestMethod]
        public void Equals_SameObj()
        {
            StorageKey uut = new();
            uut.Equals(uut).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_Null()
        {
            StorageKey uut = new();
            uut.Equals(null).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_SameHash_SameKey()
        {
            int val = 0x42000000;
            byte[] keyVal = TestUtils.GetByteArray(10, 0x42);
            StorageKey newSk = new StorageKey
            {
                Id = val,
                Key = keyVal
            };
            StorageKey uut = new() { Id = val, Key = keyVal };
            uut.Equals(newSk).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_DiffHash_SameKey()
        {
            int val = 0x42000000;
            byte[] keyVal = TestUtils.GetByteArray(10, 0x42);
            StorageKey newSk = new StorageKey
            {
                Id = val,
                Key = keyVal
            };
            StorageKey uut = new() { Id = 0x78000000, Key = keyVal };
            uut.Equals(newSk).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_SameHash_DiffKey()
        {
            int val = 0x42000000;
            byte[] keyVal = TestUtils.GetByteArray(10, 0x42);
            StorageKey newSk = new StorageKey
            {
                Id = val,
                Key = keyVal
            };
            StorageKey uut = new() { Id = val, Key = TestUtils.GetByteArray(10, 0x88) };
            uut.Equals(newSk).Should().BeFalse();
        }

        [TestMethod]
        public void GetHashCode_Get()
        {
            StorageKey uut = new() { Id = 0x42000000, Key = TestUtils.GetByteArray(10, 0x42) };
            uut.GetHashCode().Should().Be(1374529787);
        }

        [TestMethod]
        public void Equals_Obj()
        {
            StorageKey uut = new();
            uut.Equals(1u).Should().BeFalse();
            uut.Equals((object)uut).Should().BeTrue();
        }
    }
}
