using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
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
