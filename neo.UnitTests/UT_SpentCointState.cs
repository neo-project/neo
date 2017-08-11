using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_SpentCointState
    {
        SpentCoinState uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new SpentCoinState();
        }

        [TestMethod]
        public void TransactionHash_Get()
        {
            uut.TransactionHash.Should().BeNull();
        }

        [TestMethod]
        public void TransactionHash_Set()
        {
            UInt256 val = new UInt256();
            uut.TransactionHash = val;
            uut.TransactionHash.Should().Be(val);
        }

        [TestMethod]
        public void TransactionHeight_Get()
        {
            uut.TransactionHeight.Should().Be(0u);
        }

        [TestMethod]
        public void TransactionHeight_Set()
        {
            uint val = 4294967295;
            uut.TransactionHeight = val;
            uut.TransactionHeight.Should().Be(val);
        }

        [TestMethod]
        public void Items_Get()
        {
            uut.Items.Should().BeNull();
        }

        [TestMethod]
        public void Items_Set()
        {
            ushort key = new ushort();
            uint val = new uint();
            Dictionary<ushort, uint> dict = new Dictionary<ushort, uint>();
            dict.Add(key, val);
            uut.Items = dict;
            uut.Items[key].Should().Be(val);
        }

        [TestMethod]
        public void Size_Get_With_No_Items()
        {
            UInt256 val = new UInt256();
            Dictionary<ushort, uint> dict = new Dictionary<ushort, uint>();
            uut.TransactionHash = val;
            uut.Items = dict;
            uut.Size.Should().Be(38); // 1 + 32 + 4 + 1 + 0 * (2 + 4)
        }

        [TestMethod]
        public void Size_Get_With_Items()
        {
            UInt256 val = new UInt256();
            Dictionary<ushort, uint> dict = new Dictionary<ushort, uint>();
            uut.TransactionHash = val;
            dict.Add(42, 100);
            uut.Items = dict;
            uut.Size.Should().Be(44); // 1 + 32 + 4 + 1 + 1 * (2 + 4)
        }

        private void setupSpentCoinStateWithValues(SpentCoinState spentCoinState, out UInt256 transactionHash, out uint transactionHeight)
        {
            transactionHash = new UInt256(TestUtils.GetByteArray(32, 0x20));
            spentCoinState.TransactionHash = transactionHash;
            transactionHeight = 757859114;
            spentCoinState.TransactionHeight = transactionHeight;
            Dictionary<ushort, uint> dict = new Dictionary<ushort, uint>();
            dict.Add(42, 100);
            spentCoinState.Items = dict;
        }

        [TestMethod]
        public void DeserializeSCS()
        {
            byte[] dataArray = new byte[] { 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 42, 3, 44, 45, 1, 42, 0, 0, 0, 0, 0 };
                                          
            using (Stream stream = new MemoryStream(dataArray))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    uut.Deserialize(reader);
                }
            }
            uut.TransactionHash.Should().Be(new UInt256(TestUtils.GetByteArray(32, 0x20)));
            uut.TransactionHeight.Should().Be(757859114);
            uut.Items.Should().ContainKey(42);
            uut.Items[42].Should().Be(0);
        }

        [TestMethod]
        public void SerializeSCS()
        {
            UInt256 transactionHash;
            uint transactionHeight;
            setupSpentCoinStateWithValues(uut, out transactionHash, out transactionHeight);

            byte[] dataArray;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    dataArray = stream.ToArray();
                }
            }
            byte[] requiredData = new byte[] { 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 42, 3, 44, 45, 1, 42, 0, 100, 0, 0, 0 };
            dataArray.Length.Should().Be(44);
            for (int i = 0; i < 44; i++)
            {
                dataArray[i].Should().Be(requiredData[i]);
            }
        }
    }
}
