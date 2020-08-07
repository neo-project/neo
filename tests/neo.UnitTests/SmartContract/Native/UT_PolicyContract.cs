using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
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
        public void Check_Default()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            var ret = NativeContract.Policy.Call(snapshot, "getMaxTransactionsPerBlock");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(512);

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1024 * 256);

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSystemFee");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(9000 * 100000000L);

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1000);

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

            NativeContract.Policy.Initialize(ApplicationEngine.Create(TriggerType.Application, null, snapshot, 0));

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(null),
                "setMaxBlockSize", new ContractParameter(ContractParameterType.Integer) { Value = 1024 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1024 * 256);

            // More than expected

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                 "setMaxBlockSize", new ContractParameter(ContractParameterType.Integer) { Value = Neo.Network.P2P.Message.PayloadMaxSize });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1024 * 256);

            // With signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "setMaxBlockSize", new ContractParameter(ContractParameterType.Integer) { Value = 1024 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1024);
        }

        [TestMethod]
        public void Check_SetMaxBlockSystemFee()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            NativeContract.Policy.Initialize(ApplicationEngine.Create(TriggerType.Application, null, snapshot, 0));

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(null),
                "setMaxBlockSystemFee", new ContractParameter(ContractParameterType.Integer) { Value = 1024 * (long)NativeContract.GAS.Factor });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSystemFee");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(9000 * (long)NativeContract.GAS.Factor);

            // Less than expected

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                 "setMaxBlockSystemFee", new ContractParameter(ContractParameterType.Integer) { Value = -1000 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSystemFee");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(9000 * (long)NativeContract.GAS.Factor);

            // With signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "setMaxBlockSystemFee", new ContractParameter(ContractParameterType.Integer) { Value = 1024 * (long)NativeContract.GAS.Factor });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSystemFee");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1024 * (long)NativeContract.GAS.Factor);
        }

        [TestMethod]
        public void Check_SetMaxTransactionsPerBlock()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            NativeContract.Policy.Initialize(ApplicationEngine.Create(TriggerType.Application, null, snapshot, 0));

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
                "setMaxTransactionsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxTransactionsPerBlock");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(512);

            // With signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(NativeContract.NEO.GetCommitteeAddress(snapshot)),
                "setMaxTransactionsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getMaxTransactionsPerBlock");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1);
        }

        [TestMethod]
        public void Check_SetFeePerByte()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            NativeContract.Policy.Initialize(ApplicationEngine.Create(TriggerType.Application, null, snapshot, 0));

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1000);

            // With signature
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1);
        }

        [TestMethod]
        public void Check_Block_UnblockAccount()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            NativeContract.Policy.Initialize(ApplicationEngine.Create(TriggerType.Application, null, snapshot, 0));

            // Block without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(0);

            // Block with signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(1);
            ((VM.Types.Array)ret)[0].GetSpan().ToArray().Should().BeEquivalentTo(UInt160.Zero.ToArray());

            // Unblock without signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(1);
            ((VM.Types.Array)ret)[0].GetSpan().ToArray().Should().BeEquivalentTo(UInt160.Zero.ToArray());

            // Unblock with signature

            ret = NativeContract.Policy.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(0);
        }
    }
}
