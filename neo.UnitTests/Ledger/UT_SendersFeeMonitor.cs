using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Numerics;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_SendersFeeMonitor
    {
        private Transaction CreateTransactionWithFee(long networkFee, long systemFee)
        {
            Random random = new Random();
            var randomBytes = new byte[16];
            random.NextBytes(randomBytes);
            Mock<Transaction> mock = new Mock<Transaction>();
            mock.Setup(p => p.Reverify(It.IsAny<Snapshot>(), It.IsAny<BigInteger>())).Returns(true);
            mock.Setup(p => p.Verify(It.IsAny<Snapshot>(), It.IsAny<BigInteger>())).Returns(true);
            mock.Object.Script = randomBytes;
            mock.Object.Sender = UInt160.Zero;
            mock.Object.NetworkFee = networkFee;
            mock.Object.SystemFee = systemFee;
            mock.Object.Attributes = new TransactionAttribute[0];
            mock.Object.Cosigners = new Cosigner[0];
            mock.Object.Witnesses = new[]
            {
                new Witness
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                }
            };
            return mock.Object;
        }

        [TestMethod]
        public void TestMemPoolSenderFee()
        {
            Transaction transaction = CreateTransactionWithFee(1, 2);
            BigInteger originalValue = SendersFeeMonitor.GetMemPoolSenderFee(transaction.Sender);
            SendersFeeMonitor.AddMemPoolSenderFee(transaction);
            SendersFeeMonitor.GetMemPoolSenderFee(transaction.Sender).Should().Be(3 + originalValue);
            SendersFeeMonitor.AddMemPoolSenderFee(transaction);
            SendersFeeMonitor.GetMemPoolSenderFee(transaction.Sender).Should().Be(6 + originalValue);
            SendersFeeMonitor.RemoveMemPoolSenderFee(transaction);
            SendersFeeMonitor.GetMemPoolSenderFee(transaction.Sender).Should().Be(3 + originalValue);
            SendersFeeMonitor.RemoveMemPoolSenderFee(transaction);
            SendersFeeMonitor.GetMemPoolSenderFee(transaction.Sender).Should().Be(originalValue);
            SendersFeeMonitor.ClearMemPoolSenderFee();
            SendersFeeMonitor.GetMemPoolSenderFee(transaction.Sender).Should().Be(0);
        }

        [TestMethod]
        public void TestConsensusSenderFee()
        {
            Transaction transaction = CreateTransactionWithFee(1, 2);
            BigInteger originalValue = SendersFeeMonitor.GetConsensusSenderFee(transaction.Sender);
            SendersFeeMonitor.AddConsensusSenderFee(transaction);
            SendersFeeMonitor.GetConsensusSenderFee(transaction.Sender).Should().Be(3 + originalValue);
            SendersFeeMonitor.AddConsensusSenderFee(transaction);
            SendersFeeMonitor.GetConsensusSenderFee(transaction.Sender).Should().Be(6 + originalValue);
            SendersFeeMonitor.RemoveConsensusSenderFee(transaction);
            SendersFeeMonitor.GetConsensusSenderFee(transaction.Sender).Should().Be(3 + originalValue);
            SendersFeeMonitor.RemoveConsensusSenderFee(transaction);
            SendersFeeMonitor.GetConsensusSenderFee(transaction.Sender).Should().Be(originalValue);
            SendersFeeMonitor.ClearConsensusSenderFee();
            SendersFeeMonitor.GetConsensusSenderFee(transaction.Sender).Should().Be(0);
        }
    }
}
