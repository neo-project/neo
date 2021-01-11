using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract.Native;
using System.IO;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_HashIndexState
    {
        HashIndexState origin;

        [TestInitialize]
        public void Initialize()
        {
            origin = new HashIndexState
            {
                Hash = UInt256.Zero,
                Index = 10
            };
        }

        [TestMethod]
        public void TestGetSize()
        {
            ((ISerializable)origin).Size.Should().Be(36);
        }

        [TestMethod]
        public void TestDeserialize()
        {
            using (MemoryStream ms = new MemoryStream(1024))
            using (BinaryWriter writer = new BinaryWriter(ms))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ((ISerializable)origin).Serialize(writer);
                ms.Seek(0, SeekOrigin.Begin);
                HashIndexState dest = new HashIndexState();
                ((ISerializable)dest).Deserialize(reader);
                dest.Hash.Should().Be(origin.Hash);
                dest.Index.Should().Be(origin.Index);
            }
        }
    }
}
