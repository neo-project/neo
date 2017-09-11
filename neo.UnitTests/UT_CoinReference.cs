using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using System.IO;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_CoinReference
    {
        CoinReference uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new CoinReference();
        }

        [TestMethod]
        public void PrevHash_Get()
        {
            uut.PrevHash.Should().BeNull();
        }

        [TestMethod]
        public void PrevHash_Set()
        {
            UInt256 val = new UInt256(TestUtils.GetByteArray(32, 0x42));
            uut.PrevHash = val;

            uut.PrevHash.Should().Be(val);
        }

        [TestMethod]
        public void PrevIndex_Get()
        {
            uut.PrevIndex.Should().Be(0);
        }

        [TestMethod]
        public void PrevIndex_Set()
        {
            ushort val = 42;
            uut.PrevIndex = val;

            uut.PrevIndex.Should().Be(val);
        }

        [TestMethod]
        public void Size()
        {
            uut.PrevHash = new UInt256(TestUtils.GetByteArray(32, 0x42));
            uut.Size.Should().Be(34);
        }

        private void setupCoinReferenceWithVals(CoinReference coinRef, out UInt256 prevHashVal, out ushort prevIndexVal)
        {
            prevHashVal = new UInt256(TestUtils.GetByteArray(32, 0x42));
            prevIndexVal = 22;
            coinRef.PrevHash = prevHashVal;
            coinRef.PrevIndex = prevIndexVal;
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 prevHashVal;
            ushort prevIndexVal;
            setupCoinReferenceWithVals(uut, out prevHashVal, out prevIndexVal);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    ((ISerializable)uut).Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 66, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 22, 0 };
            data.Length.Should().Be(34);
            for (int i = 0; i < 34; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 prevHashVal;
            ushort prevIndexVal;
            setupCoinReferenceWithVals(uut, out prevHashVal, out prevIndexVal);

            byte[] data = new byte[] { 66, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 22, 0 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    ((ISerializable)uut).Deserialize(reader);
                }
            }

            uut.PrevHash.Should().Be(prevHashVal);
            uut.PrevIndex.Should().Be(prevIndexVal);
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
        public void Equals_SameHash()
        {

            UInt256 prevHashVal;
            ushort prevIndexVal;
            setupCoinReferenceWithVals(uut, out prevHashVal, out prevIndexVal);
            CoinReference newCoinRef = new CoinReference();
            setupCoinReferenceWithVals(newCoinRef, out prevHashVal, out prevIndexVal);           

            uut.Equals(newCoinRef).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_DiffHash()
        {

            UInt256 prevHashVal;
            ushort prevIndexVal;
            setupCoinReferenceWithVals(uut, out prevHashVal, out prevIndexVal);
            CoinReference newCoinRef = new CoinReference();
            setupCoinReferenceWithVals(newCoinRef, out prevHashVal, out prevIndexVal);
            newCoinRef.PrevHash = new UInt256(TestUtils.GetByteArray(32, 0x78));

            uut.Equals(newCoinRef).Should().BeFalse();
        }


        [TestMethod]
        public void Equals_SameIndex()
        {

            UInt256 prevHashVal;
            ushort prevIndexVal;
            setupCoinReferenceWithVals(uut, out prevHashVal, out prevIndexVal);
            CoinReference newCoinRef = new CoinReference();
            setupCoinReferenceWithVals(newCoinRef, out prevHashVal, out prevIndexVal);

            uut.Equals(newCoinRef).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_DiffIndex()
        {

            UInt256 prevHashVal;
            ushort prevIndexVal;
            setupCoinReferenceWithVals(uut, out prevHashVal, out prevIndexVal);
            CoinReference newCoinRef = new CoinReference();
            setupCoinReferenceWithVals(newCoinRef, out prevHashVal, out prevIndexVal);
            newCoinRef.PrevIndex = 73;

            uut.Equals(newCoinRef).Should().BeFalse();
        }

        [TestMethod]
        public void Class_GetHashCode()
        {
            UInt256 prevHashVal;
            ushort prevIndexVal;
            setupCoinReferenceWithVals(uut, out prevHashVal, out prevIndexVal);          

            uut.GetHashCode().Should().Be(538976344);
        }

        [TestMethod]
        public void ToJson()
        {
            UInt256 prevHashVal;
            ushort prevIndexVal;
            setupCoinReferenceWithVals(uut, out prevHashVal, out prevIndexVal);

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["txid"].AsString().Should().Be("0x2020202020202020202020202020202020202020202020202020202020202042");
            jObj["vout"].AsNumber().Should().Be(prevIndexVal);
        }

    }
}
