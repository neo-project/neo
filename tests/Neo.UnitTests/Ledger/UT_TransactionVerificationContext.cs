using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_TransactionVerificationContext
    {
        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            _ = TestBlockchain.TheNeoSystem;
        }

        private Transaction CreateTransactionWithFee(long networkFee, long systemFee)
        {
            Random random = new();
            var randomBytes = new byte[16];
            random.NextBytes(randomBytes);
            Mock<Transaction> mock = new();
            mock.Setup(p => p.VerifyStateDependent(It.IsAny<ProtocolSettings>(), It.IsAny<ClonedCache>(), It.IsAny<TransactionVerificationContext>(), It.IsAny<IEnumerable<Transaction>>())).Returns(VerifyResult.Succeed);
            mock.Setup(p => p.VerifyStateIndependent(It.IsAny<ProtocolSettings>())).Returns(VerifyResult.Succeed);
            mock.Object.Script = randomBytes;
            mock.Object.NetworkFee = networkFee;
            mock.Object.SystemFee = systemFee;
            mock.Object.Signers = new[] { new Signer { Account = UInt160.Zero } };
            mock.Object.Attributes = Array.Empty<TransactionAttribute>();
            mock.Object.Witnesses = new[]
            {
                new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                }
            };
            return mock.Object;
        }

        [TestMethod]
        public async Task TestDuplicateOracle()
        {
            // Fake balance
            var snapshot = TestBlockchain.GetTestSnapshot();

            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: long.MaxValue);
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, UInt160.Zero);
            await NativeContract.GAS.Burn(engine, UInt160.Zero, balance);
            _ = NativeContract.GAS.Mint(engine, UInt160.Zero, 8, false);

            // Test
            TransactionVerificationContext verificationContext = new();
            var tx = CreateTransactionWithFee(1, 2);
            tx.Attributes = new TransactionAttribute[] { new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = 1, Result = Array.Empty<byte>() } };
            var conflicts = new List<Transaction>();
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);

            tx = CreateTransactionWithFee(2, 1);
            tx.Attributes = new TransactionAttribute[] { new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = 1, Result = Array.Empty<byte>() } };
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeFalse();
        }

        [TestMethod]
        public async Task TestTransactionSenderFee()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: long.MaxValue);
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, UInt160.Zero);
            await NativeContract.GAS.Burn(engine, UInt160.Zero, balance);
            _ = NativeContract.GAS.Mint(engine, UInt160.Zero, 8, true);

            TransactionVerificationContext verificationContext = new();
            var tx = CreateTransactionWithFee(1, 2);
            var conflicts = new List<Transaction>();
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeFalse();
            verificationContext.RemoveTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeFalse();
        }

        [TestMethod]
        public async Task TestTransactionSenderFeeWithConflicts()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: long.MaxValue);
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, UInt160.Zero);
            await NativeContract.GAS.Burn(engine, UInt160.Zero, balance);
            _ = NativeContract.GAS.Mint(engine, UInt160.Zero, 3 + 3 + 1, true); // balance is enough for 2 transactions and 1 GAS is left.

            TransactionVerificationContext verificationContext = new();
            var tx = CreateTransactionWithFee(1, 2);
            var conflictingTx = CreateTransactionWithFee(1, 1); // costs 2 GAS

            var conflicts = new List<Transaction>();
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeTrue();
            verificationContext.AddTransaction(tx);
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeFalse();

            conflicts.Add(conflictingTx);
            verificationContext.CheckTransaction(tx, conflicts, snapshot).Should().BeTrue(); // 1 GAS is left on the balance + 2 GAS is free after conflicts removal => enough for one more trasnaction.
        }
    }
}
