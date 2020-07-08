using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_TransactionVerificationContext
    {
        private Transaction CreateTransactionWithFee(long networkFee, long systemFee)
        {
            Random random = new Random();
            var randomBytes = new byte[16];
            random.NextBytes(randomBytes);
            Mock<Transaction> mock = new Mock<Transaction>();
            mock.Setup(p => p.VerifyForEachBlock(It.IsAny<StoreView>(), It.IsAny<TransactionVerificationContext>())).Returns(VerifyResult.Succeed);
            mock.Setup(p => p.Verify(It.IsAny<StoreView>(), It.IsAny<TransactionVerificationContext>())).Returns(VerifyResult.Succeed);
            mock.Object.Script = randomBytes;
            mock.Object.Sender = UInt160.Zero;
            mock.Object.NetworkFee = networkFee;
            mock.Object.SystemFee = systemFee;
            mock.Object.Attributes = Array.Empty<TransactionAttribute>();
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
            TransactionVerificationContext verificationContext = new TransactionVerificationContext();
            verificationContext.GetSenderFee(transaction.Sender).Should().Be(0);
            verificationContext.AddTransaction(transaction);
            verificationContext.GetSenderFee(transaction.Sender).Should().Be(3);
            verificationContext.AddTransaction(transaction);
            verificationContext.GetSenderFee(transaction.Sender).Should().Be(6);
            verificationContext.RemoveTransaction(transaction);
            verificationContext.GetSenderFee(transaction.Sender).Should().Be(3);
            verificationContext.RemoveTransaction(transaction);
            verificationContext.GetSenderFee(transaction.Sender).Should().Be(0);
        }
    }
}
