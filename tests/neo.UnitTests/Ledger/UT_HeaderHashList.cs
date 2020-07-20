using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using System.IO;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_HeaderHashList
    {
        HeaderHashList origin;

        [TestInitialize]
        public void Initialize()
        {
            origin = new HeaderHashList
            {
                Hashes = new UInt256[] { UInt256.Zero }
            };
        }

        [TestMethod]
        public void TestClone()
        {
            HeaderHashList dest = ((ICloneable<HeaderHashList>)origin).Clone();
            dest.Hashes.Should().BeEquivalentTo(origin.Hashes);
        }

        [TestMethod]
        public void TestFromReplica()
        {
            HeaderHashList dest = new HeaderHashList();
            ((ICloneable<HeaderHashList>)dest).FromReplica(origin);
            dest.Hashes.Should().BeEquivalentTo(origin.Hashes);
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
                HeaderHashList dest = new HeaderHashList();
                ((ISerializable)dest).Deserialize(reader);
                dest.Hashes.Should().BeEquivalentTo(origin.Hashes);
            }
        }

        [TestMethod]
        public void TestGetSize()
        {
            ((ISerializable)origin).Size.Should().Be(33);
        }
    }
}
