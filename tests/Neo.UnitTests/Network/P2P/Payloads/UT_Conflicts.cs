using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Conflicts
    {
        private const byte Prefix_Transaction = 11;
        private static UInt256 _u = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            });

        [TestMethod]
        public void Size_Get()
        {
            var test = new Conflicts() { Hash = _u };
            test.Size.Should().Be(1 + 32);
        }

        [TestMethod]
        public void ToJson()
        {
            var test = new Conflicts() { Hash = _u };
            var json = test.ToJson().ToString();
            Assert.AreEqual(@"{""type"":""Conflicts"",""hash"":""0x0101010101010101010101010101010101010101010101010101010101010101""}", json);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new Conflicts() { Hash = _u };

            var clone = test.ToArray().AsSerializable<Conflicts>();
            Assert.AreEqual(clone.Type, test.Type);

            // As transactionAttribute
            byte[] buffer = test.ToArray();
            var reader = new MemoryReader(buffer);
            clone = TransactionAttribute.DeserializeFrom(ref reader) as Conflicts;
            Assert.AreEqual(clone.Type, test.Type);

            // Wrong type
            buffer[0] = 0xff;
            Assert.ThrowsException<FormatException>(() =>
            {
                var reader = new MemoryReader(buffer);
                TransactionAttribute.DeserializeFrom(ref reader);
            });
        }

        [TestMethod]
        public void Verify()
        {
            var test = new Conflicts() { Hash = _u };
            var snapshot = TestBlockchain.GetTestSnapshot();
            var key = Ledger.UT_MemoryPool.CreateStorageKey(NativeContract.Ledger.Id, Prefix_Transaction, _u.ToArray());

            snapshot.Add(key, new StorageItem());
            Assert.IsFalse(test.Verify(snapshot, new Transaction()));

            snapshot.Delete(key);
            Assert.IsTrue(test.Verify(snapshot, new Transaction()));
        }
    }
}
