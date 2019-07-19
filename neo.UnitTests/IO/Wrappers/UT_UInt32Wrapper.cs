using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Wrappers;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_UInt32Wrapper
    {
        [TestMethod]
        public void TestGetSize()
        {
            UInt32Wrapper temp = new UInt32Wrapper();
            Assert.AreEqual(4, temp.Size);
        }

        [TestMethod]
        public void TestDeserialize()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x01 });
            stream.Seek(0, SeekOrigin.Begin);
            UInt32Wrapper temp = new UInt32Wrapper();
            temp.Deserialize(reader);
            MemoryStream stream2 = new MemoryStream();
            BinaryWriter writer2 = new BinaryWriter(stream2);
            temp.Serialize(writer2);
            stream2.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream2.Length];
            stream2.Read(byteArray, 0, (int)stream2.Length);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x00, 0x00, 0x00, 0x01 }), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestSerialize()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x01 });
            stream.Seek(0, SeekOrigin.Begin);
            UInt32Wrapper temp = new UInt32Wrapper();
            temp.Deserialize(reader);
            MemoryStream stream2 = new MemoryStream();
            BinaryWriter writer2 = new BinaryWriter(stream2);
            temp.Serialize(writer2);
            stream2.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream2.Length];
            stream2.Read(byteArray, 0, (int)stream2.Length);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x00, 0x00, 0x00, 0x01 }), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestEqualsUInt32Wrapper()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            writer.Write((uint)1);
            stream.Seek(0, SeekOrigin.Begin);
            UInt32Wrapper temp = new UInt32Wrapper();
            temp.Deserialize(reader);
            MemoryStream stream2 = new MemoryStream();
            BinaryWriter writer2 = new BinaryWriter(stream2);
            BinaryReader reader2 = new BinaryReader(stream2);
            writer2.Write((uint)1);
            stream2.Seek(0, SeekOrigin.Begin);
            UInt32Wrapper temp2 = new UInt32Wrapper();
            temp2.Deserialize(reader2);
            Assert.AreEqual(true, temp.Equals(temp2));
        }

        [TestMethod]
        public void TestOperatorUint()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            writer.Write((uint)1);
            stream.Seek(0, SeekOrigin.Begin);
            UInt32Wrapper temp = new UInt32Wrapper();
            temp.Deserialize(reader);
            uint result = temp;
            Assert.AreEqual((uint)1, result);
        }

        [TestMethod]
        public void TestOperatorUInt32Wrapper()
        {
            UInt32Wrapper temp = 1;
            Assert.AreEqual(true, temp.Equals(1));
        }
    }
}
