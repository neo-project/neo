using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_StorageItem
    {
        StorageItem uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new StorageItem();
        }

        [TestMethod]
        public void Value_Get()
        {
            uut.Value.Should().BeNull();
        }

        [TestMethod]
        public void Value_Set()
        {
            byte[] val = new byte[] { 0x42, 0x32};
            uut.Value = val;
            uut.Value.Length.Should().Be(2);
            uut.Value[0].Should().Be(val[0]);
            uut.Value[1].Should().Be(val[1]);
        }

        [TestMethod]
        public void Size_Get()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);
            uut.Size.Should().Be(12); // 2 + 10
        }

        [TestMethod]
        public void Size_Get_Larger()
        {
            uut.Value = TestUtils.GetByteArray(88, 0x42);
            uut.Size.Should().Be(90); // 2 + 88
        }

        [TestMethod]
        public void Clone()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);

            StorageItem newSi = ((ICloneable<StorageItem>)uut).Clone();
            newSi.Value.Length.Should().Be(10);
            newSi.Value[0].Should().Be(0x42);
            for (int i=1; i<10; i++)
            {
                newSi.Value[i].Should().Be(0x20);
            }
        }

        [TestMethod]
        public void Deserialize()
        {
            byte[] data = new byte[] { 0, 10, 66, 32, 32, 32, 32, 32, 32, 32, 32, 32 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }
            uut.Value.Length.Should().Be(10);
            uut.Value[0].Should().Be(0x42);
            for (int i = 1; i < 10; i++)
            {
                uut.Value[i].Should().Be(0x20);
            }
        }

        [TestMethod]
        public void Serialize()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 10, 66, 32, 32, 32, 32, 32, 32, 32, 32, 32 };

            data.Length.Should().Be(12);
            for (int i = 0; i < 12; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

    }
}
