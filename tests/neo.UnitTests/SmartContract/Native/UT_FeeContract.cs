using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_FeeContract
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void Check_SupportedStandards() => NativeContract.Fee.SupportedStandards(Blockchain.Singleton.GetSnapshot()).Should().BeEquivalentTo(new string[] { "NEP-10" });

        [TestMethod]
        public void Check_Initialize()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var keyCount = snapshot.Storages.GetChangeSet().Count();

            NativeContract.Fee.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0)).Should().BeTrue();

            (keyCount + 1).Should().Be(snapshot.Storages.GetChangeSet().Count());

            var ret = NativeContract.Fee.Call(snapshot, "getRatio");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1u);
        }

        [TestMethod]
        public void Check_SetSyscallPrice()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            uint runtimeCheckWitness = "System.Runtime.CheckWitness".ToInteropMethodHash();

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            NativeContract.Fee.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0)).Should().BeTrue();

            IList<ContractParameter> values = new List<ContractParameter>();
            values.Add(new ContractParameter { Type = ContractParameterType.Integer, Value = runtimeCheckWitness });
            values.Add(new ContractParameter { Type = ContractParameterType.Integer, Value = 300 });

            // Without signature

            var ret = NativeContract.Fee.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
               "setSyscallPrice", new ContractParameter(ContractParameterType.Array) { Value = values });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeFalse();

            // With signature

            ret = NativeContract.Fee.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero),
               "setSyscallPrice", new ContractParameter(ContractParameterType.Array) { Value = values });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeTrue();

            ret = NativeContract.Fee.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero),
               "getSyscallPrice", new ContractParameter(ContractParameterType.Integer) { Value = runtimeCheckWitness });
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(300);
        }

        [TestMethod]
        public void Check_SetOpCodePrice()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            int pushdata4 = (int)OpCode.PUSHDATA4;

            // Fake blockchain

            snapshot.PersistingBlock = new Block() { Index = 1000, PrevHash = UInt256.Zero };
            snapshot.Blocks.Add(UInt256.Zero, new Neo.Ledger.TrimmedBlock() { NextConsensus = UInt160.Zero });

            NativeContract.Fee.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0)).Should().BeTrue();

            IList<ContractParameter> values = new List<ContractParameter>();
            values.Add(new ContractParameter { Type = ContractParameterType.Integer, Value = pushdata4 });
            values.Add(new ContractParameter { Type = ContractParameterType.Integer, Value = 1100 });

            // Without signature

            var ret = NativeContract.Fee.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(),
               "setOpCodePrice", new ContractParameter(ContractParameterType.Array) { Value = values });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeFalse();

            // With signature

            ret = NativeContract.Fee.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero),
               "setOpCodePrice", new ContractParameter(ContractParameterType.Array) { Value = values });
            ret.Should().BeOfType<VM.Types.Boolean>();
            ret.ToBoolean().Should().BeTrue();

            ret = NativeContract.Fee.Call(snapshot, new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero),
               "getOpCodePrice", new ContractParameter(ContractParameterType.Integer) { Value = pushdata4 });
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1100);
        }
    }
}
