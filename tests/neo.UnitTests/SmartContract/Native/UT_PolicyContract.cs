using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System.Linq;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_PolicyContract
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void Check_SupportedStandards() => NativeContract.Policy.SupportedStandards().Should().BeEquivalentTo(new string[] { "NEP-10" });

        [TestMethod]
        public void Check_Initialize()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var keyCount = snapshot.Storages.GetChangeSet().Count();

            NativeContract.Policy.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0));

            (keyCount + 4).Should().Be(snapshot.Storages.GetChangeSet().Count());

            var ret = NativeContract.Policy.Call(snapshot, "getMaxTransactionsPerBlock");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(512);

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1024 * 256);

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1000);

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(0);
        }

        [TestMethod]
        public void Check_SetMaxBlockSize()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            NativeContract.Policy.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0));

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(null),
                "setMaxBlockSize", new ContractParameter(ContractParameterType.Integer) { Value = 1024 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1024 * 256);

            // More than expected

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                 "setMaxBlockSize", new ContractParameter(ContractParameterType.Integer) { Value = Neo.Network.P2P.Message.PayloadMaxSize });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1024 * 256);

            // With signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "setMaxBlockSize", new ContractParameter(ContractParameterType.Integer) { Value = 1024 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1024);
        }

        [TestMethod]
        public void Check_SetMaxTransactionsPerBlock()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            NativeContract.Policy.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0));

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
                "setMaxTransactionsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxTransactionsPerBlock");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(512);

            // With signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(NativeContract.NEO.GetCommitteeAddress(snapshot)),
                "setMaxTransactionsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getMaxTransactionsPerBlock");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1);
        }

        [TestMethod]
        public void Check_SetFeePerByte()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            NativeContract.Policy.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0));

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1000);

            // With signature
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1);
        }

        [TestMethod]
        public void Check_Block_UnblockAccount()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            NativeContract.Policy.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0));

            // Block without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(0);

            // Block with signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(1);
            ((VM.Types.Array)ret)[0].GetSpan().ToArray().Should().BeEquivalentTo(UInt160.Zero.ToArray());

            // Unblock without signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(1);
            ((VM.Types.Array)ret)[0].GetSpan().ToArray().Should().BeEquivalentTo(UInt160.Zero.ToArray());

            // Unblock with signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(0);
        }

        [TestMethod]
        public void TestCheckPolicy()
        {
            Transaction tx = Blockchain.GenesisBlock.Transactions[0];
            var snapshot = Blockchain.Singleton.GetSnapshot();

            StorageKey storageKey = new StorageKey
            {
                Id = NativeContract.Policy.Id,
                Key = new byte[sizeof(byte)]
            };
            storageKey.Key[0] = 15;
            snapshot.Storages.Add(storageKey, new StorageItem
            {
                Value = new UInt160[] { tx.Sender }.ToByteArray(),
            });

            NativeContract.Policy.CheckPolicy(tx, snapshot).Should().BeFalse();

            snapshot = Blockchain.Singleton.GetSnapshot();
            NativeContract.Policy.CheckPolicy(tx, snapshot).Should().BeTrue();
        }
    }
}
