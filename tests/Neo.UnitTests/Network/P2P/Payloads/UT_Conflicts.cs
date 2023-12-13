using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;

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

            // Conflicting transaction is in the Conflicts attribute of some other on-chain transaction.
            var conflict = new TransactionState();
            snapshot.Add(key, new StorageItem(conflict));
            Assert.IsTrue(test.Verify(snapshot, new Transaction()));

            // Conflicting transaction is on-chain.
            snapshot.Delete(key);
            conflict = new TransactionState
            {
                BlockIndex = 123,
                Transaction = new Transaction(),
                State = VMState.NONE
            };
            snapshot.Add(key, new StorageItem(conflict));
            Assert.IsFalse(test.Verify(snapshot, new Transaction()));

            // There's no conflicting transaction at all.
            snapshot.Delete(key);
            Assert.IsTrue(test.Verify(snapshot, new Transaction()));
        }
    }
}
