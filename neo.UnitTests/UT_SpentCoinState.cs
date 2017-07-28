using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using Neo.Core;
using System.Text;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_SpentCointState
    {
        SpentCoinState scs;

        [TestInitialize]
        public void TestSetup()
        {
            scs = new SpentCoinState();
        }

        [TestMethod]
        public void TransactionHash_Get()
        {
            scs.TransactionHash.Should().BeNull();
        }

        [TestMethod]
        public void TransactionHash_Set()
        {
            UInt256 val = new UInt256();
            scs.TransactionHash = val;
            scs.TransactionHash.Should().Be(val);
        }
        [TestMethod]
        public void TransactionHeight_Get()
        {
            scs.TransactionHeight.Should().Be(0u);
        }

        [TestMethod]
        public void TransactionHeight_Set()
        {
            uint val = 69;
            scs.TransactionHeight = val;
            scs.TransactionHeight.Should().Be(val);
        }
        [TestMethod]
        public void Items_Get()
        {
            scs.Items.Should().BeNull();
        }

        [TestMethod]
        public void Items_Set()
        {
            ushort key = new ushort();
            uint val = new uint();
            Dictionary<ushort, uint> dict = new Dictionary<ushort, uint>();
            dict.Add(key, val);
            scs.Items = dict;
            scs.Items[key].Should().Be(val);
        }
        private void setupSpentCoinStateWithValues(SpentCoinState spentCoinState, out UInt256 transactionHash, out uint transactionHeight, out ushort key, out uint dictVal)
        {
            transactionHash = new UInt256(TestUtils.GetByteArray(32, 0x20));
            spentCoinState.TransactionHash = transactionHash;
            transactionHeight = 69u;
            spentCoinState.TransactionHeight = transactionHeight;
            key = new ushort();
            dictVal = new uint();
            Dictionary<ushort, uint> dict = new Dictionary<ushort, uint>();
            dict.Add(key, dictVal);
            spentCoinState.Items = dict;
        }
        [TestMethod]
        public void DeserializeSCS()
        {
            UInt256 transactionHash;
            uint transactionHeight;
            ushort key;
            uint dictVal;
            setupSpentCoinStateWithValues(new SpentCoinState(), out transactionHash, out transactionHeight, out key, out dictVal);

            byte[] dataArray = new byte[] { 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 42, 3, 44, 45, 48, 42, 0, 0, 0, 0, 0, 0, 0, 43, 0, 0, 0, 0, 0, 0, 0, 66, 0, 44, 0, 0, 0, 0, 0, 0, 0, 33, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32 };
            Stream stream = new MemoryStream(dataArray);
            using (BinaryReader reader = new BinaryReader(stream))
            {
                scs.Deserialize(reader);
            }
            scs.TransactionHash.Should().Be(transactionHash);
        }
        [TestMethod]
        public void SerializeSCS()
        {
            UInt256 transactionHash;
            uint transactionHeight;
            ushort key;
            uint dictVal;
            setupSpentCoinStateWithValues(scs, out transactionHash, out transactionHeight, out key, out dictVal);

            byte[] dataArray;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    scs.Serialize(writer);
                    dataArray = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 42, 3, 44, 45, 48, 42, 0, 0, 0, 0, 0, 0, 0, 43, 0, 0, 0, 0, 0, 0, 0, 66, 0, 44, 0, 0, 0, 0, 0, 0, 0, 33, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32 };
            dataArray.Length.Should().Be(44);
            for (int i = 0; i < 44; i++)
            {
                dataArray[i].Should().Be(requiredData[i]);
            }
        }

    }


}

