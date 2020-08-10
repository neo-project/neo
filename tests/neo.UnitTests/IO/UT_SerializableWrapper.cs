using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using System.IO;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_SerializableWrapper
    {
        [TestMethod]
        public void TestGetSize()
        {
            SerializableWrapper<uint> temp = new SerializableWrapper<uint>();
            Assert.AreEqual(4, temp.Size);
        }

        [TestMethod]
        public void TestCast()
        {
            SerializableWrapper<uint> tempA = (SerializableWrapper<uint>)123;
            SerializableWrapper<uint> tempB = tempA.ToArray().AsSerializable<SerializableWrapper<uint>>();

            Assert.IsTrue(tempA.Equals(tempB));
            Assert.AreEqual((uint)123, (uint)tempA);
        }

        [TestMethod]
        public void TestEqualsOtherObject()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            writer.Write((uint)1);
            stream.Seek(0, SeekOrigin.Begin);
            SerializableWrapper<uint> temp = new SerializableWrapper<uint>();
            temp.Deserialize(reader);
            Assert.AreEqual(true, temp.Equals(1u));
        }

        [TestMethod]
        public void TestEqualsOtherSerializableWrapper()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            writer.Write((uint)1);
            stream.Seek(0, SeekOrigin.Begin);
            SerializableWrapper<uint> temp = new SerializableWrapper<uint>();
            temp.Deserialize(reader);
            MemoryStream stream2 = new MemoryStream();
            BinaryWriter writer2 = new BinaryWriter(stream2);
            BinaryReader reader2 = new BinaryReader(stream2);
            writer2.Write((uint)1);
            stream2.Seek(0, SeekOrigin.Begin);
            SerializableWrapper<uint> temp2 = new SerializableWrapper<uint>();
            temp2.Deserialize(reader2);
            Assert.AreEqual(true, temp.Equals(temp2));
        }
    }
}
