using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
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
                Transaction = new Neo.Network.P2P.Payloads.Transaction()
                {
                    Attributes = Array.Empty<TransactionAttribute>(),
                    Script = new byte[] { 0x01 },
                    Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                    Witnesses = new Witness[] { new Witness() {
                        InvocationScript=Array.Empty<byte>(),
                        VerificationScript=Array.Empty<byte>()
                    } }
                }
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
                dest.Transaction.Hash.Should().Be(origin.Transaction.Hash);
            }
        }

        [TestMethod]
        public void TestGetSize()
        {
            ((ISerializable)origin).Size.Should().Be(58);
        }
    }
}
