using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using System.IO;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ByteArrayWrapper
    {
        [TestMethod]
        public void TestSerialize()
        {
            var w = new ByteArrayWrapper("01020a0c0001000000".HexToBytes());
            Assert.AreEqual("0901020a0c0001000000", w.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestDeserialize()
        {
            var w = "0901020a0c0001000000".HexToBytes().AsSerializable<ByteArrayWrapper>();
            Assert.AreEqual(w.Value.ToHexString(), "01020a0c0001000000");
        }

        [TestMethod]
        public void TestEqualsOtherByteArrayWrapper()
        {
            var w1 = new ByteArrayWrapper(new byte[] { 1, 11 });
            var w2 = new ByteArrayWrapper(new byte[] { 1, 11 });
            Assert.IsTrue(w1.Equals(w2));
            Assert.IsTrue(w1.Equals(new byte[] { 1, 11 }));
            Assert.IsFalse(w1.Equals(new byte[] { 1, 12 }));
            Assert.IsFalse(w1.Equals(new ByteArrayWrapper()));
            Assert.IsFalse(w1.Equals((byte[])null));
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            using BinaryReader reader = new BinaryReader(stream);
            w1.Serialize(writer);
            stream.Seek(0, SeekOrigin.Begin);
            w2.Deserialize(reader);
            Assert.IsTrue(w1.Equals(w2));
        }
    }
}
