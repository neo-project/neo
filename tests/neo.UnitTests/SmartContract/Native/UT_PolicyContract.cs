using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using System;
using System.Linq;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_PolicyContract
    {
        private StoreView _snapshot;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            _snapshot = Blockchain.Singleton.GetSnapshot();
            _snapshot.PersistingBlock = new Block() { Index = 0 };

            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.OnPersist, _snapshot.PersistingBlock, _snapshot, 0);
            NativeContract.Management.OnPersist(engine);
        }

        [TestMethod]
        public void Check_Default()
        {
            var snapshot = _snapshot.Clone();

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
        }

        [TestMethod]
        public void Check_SetMaxBlockSize()
        {
            var snapshot = _snapshot.Clone();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(null),
                "setMaxBlockSize", new ContractParameter(ContractParameterType.Integer) { Value = 1024 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1024 * 256);

            // More than expected

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                 "setMaxBlockSize", new ContractParameter(ContractParameterType.Integer) { Value = Neo.Network.P2P.Message.PayloadMaxSize + 1 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1024 * 256);

            // With signature

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
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
            var snapshot = _snapshot.Clone();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(null),
                "setMaxBlockSystemFee", new ContractParameter(ContractParameterType.Integer) { Value = 1024 * (long)NativeContract.GAS.Factor });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSystemFee");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(9000 * (long)NativeContract.GAS.Factor);

            // Less than expected

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                 "setMaxBlockSystemFee", new ContractParameter(ContractParameterType.Integer) { Value = -1000 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getMaxBlockSystemFee");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(9000 * (long)NativeContract.GAS.Factor);

            // With signature

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
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
            var snapshot = _snapshot.Clone();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new TrimmedBlock() { NextConsensus = UInt160.Zero });

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(),
                "setMaxTransactionsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getMaxTransactionsPerBlock");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(512);

            // With signature

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(NativeContract.NEO.GetCommitteeAddress(snapshot)),
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
            var snapshot = _snapshot.Clone();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new TrimmedBlock() { NextConsensus = UInt160.Zero });

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(),
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1000);

            // With signature
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(1);
        }

        [TestMethod]
        public void Check_SetBaseExecFee()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            NativeContract.Policy.Initialize(ApplicationEngine.Create(TriggerType.Application, null, snapshot, 0));

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(),
                "setBaseExecFee", new ContractParameter(ContractParameterType.Integer) { Value = 50 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            ret = NativeContract.Policy.Call(snapshot, "getBaseExecFee");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(30);

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                    "setBaseExecFee", new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getBaseExecFee");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(30);

            // Proper set
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "setBaseExecFee", new ContractParameter(ContractParameterType.Integer) { Value = 50 });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            ret = NativeContract.Policy.Call(snapshot, "getBaseExecFee");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetInteger().Should().Be(50);
        }

        [TestMethod]
        public void Check_BlockAccount()
        {
            var snapshot = _snapshot.Clone();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new TrimmedBlock() { NextConsensus = UInt160.Zero });

            // Without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero),
                "blockAccount",
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            // With signature

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
              "blockAccount",
              new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            // Same account
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "blockAccount",
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            // Account B

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "blockAccount",
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            // Check

            NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeFalse();
            NativeContract.Policy.IsBlocked(snapshot, UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01")).Should().BeTrue();
            NativeContract.Policy.IsBlocked(snapshot, UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01")).Should().BeTrue();
        }

        [TestMethod]
        public void Check_Block_UnblockAccount()
        {
            var snapshot = _snapshot.Clone();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new TrimmedBlock() { NextConsensus = UInt160.Zero });

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // Block without signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(),
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeFalse();

            // Block with signature

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeTrue();

            // Unblock without signature

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(),
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeFalse();

            NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeTrue();

            // Unblock with signature

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.GetBoolean().Should().BeTrue();

            NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero).Should().BeFalse();
        }
    }
}
