using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Header
    {
        Header uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Header();
        }

        [TestMethod]
        public void Size_Get()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            TestUtils.SetupHeaderWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);
            // blockbase 4 + 32 + 32 + 4 + 4 + 8 + 20 + 1 + 3
            // header 1
            uut.Size.Should().Be(109);
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            TestUtils.SetupHeaderWithValues(new Header(), val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.MerkleRoot = merkRoot; // need to set for deserialise to be valid

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 128, 171, 4, 253, 0, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 0 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }

            assertStandardHeaderTestVals(val256, merkRoot, val160, timestampVal, indexVal, consensusDataVal, scriptVal);
        }

        private void assertStandardHeaderTestVals(UInt256 val256, UInt256 merkRoot, UInt160 val160, uint timestampVal, uint indexVal, ulong consensusDataVal, Witness scriptVal)
        {
            uut.PrevHash.Should().Be(val256);
            uut.MerkleRoot.Should().Be(merkRoot);
            uut.Timestamp.Should().Be(timestampVal);
            uut.Index.Should().Be(indexVal);
            uut.ConsensusData.Should().Be(consensusDataVal);
            uut.NextConsensus.Should().Be(val160);
            uut.Script.InvocationScript.Length.Should().Be(0);
            uut.Script.Size.Should().Be(scriptVal.Size);
            uut.Script.VerificationScript[0].Should().Be(scriptVal.VerificationScript[0]);            
        }

        [TestMethod]
        public void Equals_Null()
        {
            uut.Equals(null).Should().BeFalse();
        }


        [TestMethod]
        public void Equals_SameHeader()
        {
            uut.Equals(uut).Should().BeTrue();
        }
     
        [TestMethod]
        public void Equals_SameHash()
        {
            Header newHeader = new Header();            
            UInt256 prevHash = new UInt256(TestUtils.GetByteArray(32, 0x42));
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            TestUtils.SetupHeaderWithValues(newHeader, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);
            TestUtils.SetupHeaderWithValues(uut, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            uut.Equals(newHeader).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_SameObject()
        {
            uut.Equals((object)uut).Should().BeTrue();
        }

        [TestMethod]
        public void FromTrimmedData()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            TestUtils.SetupHeaderWithValues(new Header(), val256, out merkRoot, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 128, 171, 4, 253, 0, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 0 };

            uut = Header.FromTrimmedData(data, 0);

            assertStandardHeaderTestVals(val256, merkRoot, val160, timestampVal, indexVal, consensusDataVal, scriptVal);
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            ulong consensusDataVal;
            Witness scriptVal;
            TestUtils.SetupHeaderWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out consensusDataVal, out scriptVal);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 214, 87, 42, 69, 155, 149, 217, 19, 107, 122, 113, 60, 84, 133, 202, 112, 159, 158, 250, 79, 8, 241, 194, 93, 215, 146, 103, 45, 43, 215, 91, 251, 128, 171, 4, 253, 0, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 0 };

            data.Length.Should().Be(109);
            for (int i = 0; i < 109; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }
    }
}
