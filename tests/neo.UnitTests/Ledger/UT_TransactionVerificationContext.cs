using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Numerics;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_TransactionVerificationContext
    {
        private static NeoSystem testBlockchain;

        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            testBlockchain = TestBlockchain.TheNeoSystem;
        }

        private Transaction CreateTransactionWithFee(long networkFee, long systemFee)
        {
            Random random = new Random();
            var randomBytes = new byte[16];
            random.NextBytes(randomBytes);
            Mock<Transaction> mock = new Mock<Transaction>();
            mock.Setup(p => p.Verify(It.IsAny<ClonedCache>(), It.IsAny<TransactionVerificationContext>())).Returns(VerifyResult.Succeed);
            mock.Setup(p => p.VerifyStateDependent(It.IsAny<ClonedCache>(), It.IsAny<TransactionVerificationContext>())).Returns(VerifyResult.Succeed);
            mock.Setup(p => p.VerifyStateIndependent()).Returns(VerifyResult.Succeed);
            mock.Object.Script = randomBytes;
            mock.Object.NetworkFee = networkFee;
            mock.Object.SystemFee = systemFee;
            mock.Object.Signers = new[] { new Signer { Account = UInt160.Zero } };
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
        public void TestDuplicateOracle()
        {
            // Fake balance
            var snapshot = TestBlockchain.GetTestSnapshot();

            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, long.MaxValue);
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, UInt160.Zero);
            NativeContract.GAS.Burn(engine, UInt160.Zero, balance);
            NativeContract.GAS.Mint(engine, UInt160.Zero, 8, false);

            // Test
            TransactionVerificationContext verificationContext = new TransactionVerificationContext();
            var tx = CreateTransactionWithFee(1, 2);
            tx.Attributes = new TransactionAttribute[] { new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = 1, Result = new byte[0] } };
            verificationContext.CheckTransaction(tx, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);

            tx = CreateTransactionWithFee(2, 1);
            tx.Attributes = new TransactionAttribute[] { new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = 1, Result = new byte[0] } };
            verificationContext.CheckTransaction(tx, snapshot).Should().BeFalse();
        }

        [TestMethod]
        public void TestTransactionSenderFee()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, long.MaxValue);
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, UInt160.Zero);
            NativeContract.GAS.Burn(engine, UInt160.Zero, balance);
            NativeContract.GAS.Mint(engine, UInt160.Zero, 8, true);

            TransactionVerificationContext verificationContext = new TransactionVerificationContext();
            var tx = CreateTransactionWithFee(1, 2);
            verificationContext.CheckTransaction(tx, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, snapshot).Should().BeFalse();
            verificationContext.RemoveTransaction(tx);
            verificationContext.CheckTransaction(tx, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, snapshot).Should().BeFalse();
        }
    }
}
