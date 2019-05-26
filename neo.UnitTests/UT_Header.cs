using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using System.IO;
using System.Text;

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
            byte[][] signaturesVal;
            TestUtils.SetupHeaderWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out signaturesVal);
            // blockbase 4 + 32 + 32 + 4 + 4 + 20 + 1
            // header 1
            uut.Size.Should().Be(98);
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            TestUtils.SetupHeaderWithValues(new Header(), val256, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal);

            uut.MerkleRoot = merkRoot; // need to set for deserialise to be valid

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 90, 92, 165, 113, 177, 185, 161, 160, 235, 170, 231, 248, 38, 157, 47, 48, 13, 204, 6, 138, 124, 43, 26, 49, 152, 41, 204, 110, 65, 163, 39, 79, 128, 171, 4, 253, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }

            assertStandardHeaderTestVals(val256, merkRoot, val160, timestampVal, indexVal, signaturesVal);
        }

        private void assertStandardHeaderTestVals(UInt256 val256, UInt256 merkRoot, UInt160 val160, uint timestampVal, uint indexVal, byte[][] signaturesVal)
        {
            uut.PrevHash.Should().Be(val256);
            uut.MerkleRoot.Should().Be(merkRoot);
            uut.Timestamp.Should().Be(timestampVal);
            uut.Index.Should().Be(indexVal);
            uut.NextConsensus.Should().Be(val160);
            uut.Signatures.Length.Should().Be(0);
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
            byte[][] signaturesVal;
            TestUtils.SetupHeaderWithValues(newHeader, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal);
            TestUtils.SetupHeaderWithValues(uut, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal);

            uut.Equals(newHeader).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_SameObject()
        {
            uut.Equals((object)uut).Should().BeTrue();
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            TestUtils.SetupHeaderWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out signaturesVal);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 90, 92, 165, 113, 177, 185, 161, 160, 235, 170, 231, 248, 38, 157, 47, 48, 13, 204, 6, 138, 124, 43, 26, 49, 152, 41, 204, 110, 65, 163, 39, 79, 128, 171, 4, 253, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            data.Length.Should().Be(requiredData.Length);
            for (int i = 0; i < data.Length; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }
    }
}
