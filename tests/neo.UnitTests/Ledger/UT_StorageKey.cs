using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_StorageKey
    {
        StorageKey uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new StorageKey();
        }

        [TestMethod]
        public void Id_Get()
        {
            uut.Id.Should().Be(0);
        }

        [TestMethod]
        public void Size()
        {
            var ut = new StorageKey() { Key = new byte[17], Id = 0 };
            ut.ToArray().Length.Should().Be(((ISerializable)ut).Size);

            ut = new StorageKey() { Key = new byte[0], Id = 0 };
            ut.ToArray().Length.Should().Be(((ISerializable)ut).Size);

            ut = new StorageKey() { Key = new byte[16], Id = 0 };
            ut.ToArray().Length.Should().Be(((ISerializable)ut).Size);
        }

        [TestMethod]
        public void Id_Set()
        {
            int val = 1;
            uut.Id = val;
            uut.Id.Should().Be(val);
        }

        [TestMethod]
        public void Key_Set()
        {
            byte[] val = new byte[] { 0x42, 0x32 };
            uut.Key = val;
            uut.Key.Length.Should().Be(2);
            uut.Key.Span[0].Should().Be(val[0]);
            uut.Key.Span[1].Should().Be(val[1]);
        }

        [TestMethod]
        public void Equals_SameObj()
        {
            uut.Equals(uut).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_Null()
        {
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
            uut.Id = val;
            uut.Key = keyVal;

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
            uut.Id = 0x78000000;
            uut.Key = keyVal;

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
            uut.Id = val;
            uut.Key = TestUtils.GetByteArray(10, 0x88);

            uut.Equals(newSk).Should().BeFalse();
        }

        [TestMethod]
        public void GetHashCode_Get()
        {
            uut.Id = 0x42000000;
            uut.Key = TestUtils.GetByteArray(10, 0x42);
            uut.GetHashCode().Should().Be(1374529787);
        }

        [TestMethod]
        public void Equals_Obj()
        {
            uut.Equals(1u).Should().BeFalse();
            uut.Equals((object)uut).Should().BeTrue();
        }
    }
}
