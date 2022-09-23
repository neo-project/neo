using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_TransactionState
    {
        TransactionState origin;

        TransactionState originTrimmed;

        [TestInitialize]
        public void Initialize()
        {
            origin = new TransactionState
            {
                BlockIndex = 1,
                Transaction = new Transaction()
                {
                    Attributes = Array.Empty<TransactionAttribute>(),
                    Script = new byte[] { (byte)OpCode.PUSH1 },
                    Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                    Witnesses = new Witness[] { new Witness() {
                        InvocationScript=Array.Empty<byte>(),
                        VerificationScript=Array.Empty<byte>()
                    } }
                }
            };
            originTrimmed = new TransactionState()
            {
                Trimmed = true,
            };
        }

        [TestMethod]
        public void TestDeserialize()
        {
            var data = BinarySerializer.Serialize(((IInteroperable)origin).ToStackItem(null), 1024);
            var reader = new MemoryReader(data);

            TransactionState dest = new TransactionState();
            ((IInteroperable)dest).FromStackItem(BinarySerializer.Deserialize(ref reader, ExecutionEngineLimits.Default, null));

            dest.BlockIndex.Should().Be(origin.BlockIndex);
            dest.Transaction.Hash.Should().Be(origin.Transaction.Hash);
            dest.Trimmed.Should().Be(false);
        }

        [TestMethod]
        public void TestDeserializeTrimmed()
        {
            var data = BinarySerializer.Serialize(((IInteroperable)originTrimmed).ToStackItem(null), 1024);
            var reader = new MemoryReader(data);

            TransactionState dest = new TransactionState();
            ((IInteroperable)dest).FromStackItem(BinarySerializer.Deserialize(ref reader, ExecutionEngineLimits.Default, null));

            dest.BlockIndex.Should().Be(0);
            dest.Transaction.Should().Be(null);
            dest.Trimmed.Should().Be(true);
        }
    }
}
