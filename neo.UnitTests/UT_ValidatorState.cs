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
    public class UT_ValidatorState
    {
        ValidatorState uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new ValidatorState();
        }

        [TestMethod]
        public void PublicKey_Get()
        {
            uut.PublicKey.Should().BeNull();
        }

        [TestMethod]
        public void Items_Set()
        {
            ECPoint val = new ECPoint();
            uut.PublicKey = val;
            uut.PublicKey.Should().Be(val);            
        }

        [TestMethod]
        public void Size_Get()
        {
            ECPoint val = ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1);
            uut.PublicKey = val;

            uut.Size.Should().Be(34); // 1 + 33
        }

        [TestMethod]
        public void Deserialize()
        {
            byte[] data = new byte[] { 0, 3, 178, 9, 253, 79, 83, 167, 23, 14, 164, 68, 78, 12, 176, 166, 187, 106, 83, 194, 189, 1, 105, 38, 152, 156, 248, 95, 155, 15, 186, 23, 167, 12 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }
            uut.PublicKey.ToString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
        }

        [TestMethod]
        public void Equals_SameObj()
        {
            uut.Equals(uut).Should().BeTrue();
        }


        [TestMethod]
        public void Equals_SameKey()
        {
            ValidatorState newVs = new ValidatorState();
            newVs.PublicKey = ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1);
            uut.PublicKey = ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1);
            uut.Equals(newVs).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_DifferentKey()
        {
            ValidatorState newVs = new ValidatorState();
            newVs.PublicKey = ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70b".HexToBytes(), ECCurve.Secp256r1);
            uut.PublicKey = ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1);
            uut.Equals(newVs).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_Null()
        {
            uut.Equals(null).Should().BeFalse();
        }

        [TestMethod]
        public void GetHashCode_Get()
        {
            uut.PublicKey = ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1);
            uut.GetHashCode().Should().Be(435035065);
        }


        [TestMethod]
        public void Serialize()
        {
            ECPoint val = ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1);
            uut.PublicKey = val;

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 3, 178, 9, 253, 79, 83, 167, 23, 14, 164, 68, 78, 12, 176, 166, 187, 106, 83, 194, 189, 1, 105, 38, 152, 156, 248, 95, 155, 15, 186, 23, 167, 12 };

            data.Length.Should().Be(requiredData.Length);
            for (int i = 0; i < requiredData.Length; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

    }
}
