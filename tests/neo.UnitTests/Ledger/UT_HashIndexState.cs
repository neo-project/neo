using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
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
        public void TestDeserialize()
        {
            using MemoryStream ms = new MemoryStream(1024);
            using BinaryReader reader = new BinaryReader(ms);

            var data = BinarySerializer.Serialize(((IInteroperable)origin).ToStackItem(null), 1024);
            ms.Write(data);
            ms.Seek(0, SeekOrigin.Begin);

            HashIndexState dest = new HashIndexState();
            ((IInteroperable)dest).FromStackItem(BinarySerializer.Deserialize(reader, 1024, 1024, 2048, null));

            dest.Hash.Should().Be(origin.Hash);
            dest.Index.Should().Be(origin.Index);
        }
    }
}
