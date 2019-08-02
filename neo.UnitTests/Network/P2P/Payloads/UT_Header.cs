using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using System.IO;
using System.Text;

namespace Neo.UnitTests.Network.P2P.Payloads
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
            TestUtils.SetupHeaderWithValues(uut, val256, out _, out _, out _, out _, out _);
            // blockbase 4 + 64 + 32 + 4 + 4 + 20 + 4
            // header 1
            uut.Size.Should().Be(105);
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupHeaderWithValues(new Header(), val256, out UInt256 merkRoot, out UInt160 val160, out ulong timestampVal, out uint indexVal, out Witness scriptVal);

            uut.MerkleRoot = merkRoot; // need to set for deserialise to be valid

            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15, 41, 176, 215, 72, 169, 204, 248, 197, 175, 60, 222, 16, 219, 62, 54, 236, 154, 95, 114, 6, 67, 162, 188, 180, 173, 215, 107, 61, 175, 65, 216, 233, 19, 255, 133, 76, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 0 };

            int index = 0;
            using (MemoryStream ms = new MemoryStream(requiredData, index, requiredData.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }

            assertStandardHeaderTestVals(val256, merkRoot, val160, timestampVal, indexVal, scriptVal);
        }

        private void assertStandardHeaderTestVals(UInt256 val256, UInt256 merkRoot, UInt160 val160, ulong timestampVal, uint indexVal, Witness scriptVal)
        {
            uut.PrevHash.Should().Be(val256);
            uut.MerkleRoot.Should().Be(merkRoot);
            uut.Timestamp.Should().Be(timestampVal);
            uut.Index.Should().Be(indexVal);
            uut.NextConsensus.Should().Be(val160);
            uut.Witness.InvocationScript.Length.Should().Be(0);
            uut.Witness.Size.Should().Be(scriptVal.Size);
            uut.Witness.VerificationScript[0].Should().Be(scriptVal.VerificationScript[0]);
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
            TestUtils.SetupHeaderWithValues(newHeader, prevHash, out _, out _, out _, out _, out _);
            TestUtils.SetupHeaderWithValues(uut, prevHash, out _, out _, out _, out _, out _);

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
            TestUtils.SetupHeaderWithValues(uut, val256, out _, out _, out _, out _, out _);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15, 41, 176, 215, 72, 169, 204, 248, 197, 175, 60, 222, 16, 219, 62, 54, 236, 154, 95, 114, 6, 67, 162, 188, 180, 173, 215, 107, 61, 175, 65, 216, 233, 19, 255, 133, 76, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 0 };

            data.Length.Should().Be(requiredData.Length);

            for (int i = 0; i < data.Length; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }
    }
}
