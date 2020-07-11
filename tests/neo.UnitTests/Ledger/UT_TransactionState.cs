using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using System.IO;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_TransactionState
    {
        TransactionState origin;

        [TestInitialize]
        public void Initialize()
        {
            origin = new TransactionState
            {
                BlockIndex = 1,
                VMState = VM.VMState.NONE,
                Transaction = Blockchain.GenesisBlock.Transactions[0]
            };
        }

        [TestMethod]
        public void TestClone()
        {
            TransactionState dest = ((ICloneable<TransactionState>)origin).Clone();
            dest.BlockIndex.Should().Be(origin.BlockIndex);
            dest.VMState.Should().Be(origin.VMState);
            dest.Transaction.Should().Be(origin.Transaction);
        }

        [TestMethod]
        public void TestFromReplica()
        {
            TransactionState dest = new TransactionState();
            ((ICloneable<TransactionState>)dest).FromReplica(origin);
            dest.BlockIndex.Should().Be(origin.BlockIndex);
            dest.VMState.Should().Be(origin.VMState);
            dest.Transaction.Should().Be(origin.Transaction);
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
                TransactionState dest = new TransactionState();
                ((ISerializable)dest).Deserialize(reader);
                dest.BlockIndex.Should().Be(origin.BlockIndex);
                dest.VMState.Should().Be(origin.VMState);
                dest.Transaction.Should().Be(origin.Transaction);
            }
        }

        [TestMethod]
        public void TestGetSize()
        {
            ((ISerializable)origin).Size.Should().Be(63);
        }
    }
}
