using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_RoleDesignation
    {
        private StoreView _snapshot;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            _snapshot = Blockchain.Singleton.GetSnapshot();
            _snapshot.PersistingBlock = new Block() { Index = 0 };
        }

        [TestMethod]
        public void TestSetAndGet()
        {
            var snapshot1 = _snapshot.Clone();
            snapshot1.PersistingBlock = new Block
            {
                Index = 0,
            };
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot1);
            ECPoint[] validators = NativeContract.NEO.ComputeNextBlockValidators(snapshot1);
            var ret = NativeContract.RoleDesignation.Call(
                snapshot1,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "designateAsRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.StateValidator) },
                new ContractParameter(ContractParameterType.Array) { Value = validators.Select(p => new ContractParameter(ContractParameterType.ByteArray) { Value = p.ToArray() }).ToList() }
            );
            snapshot1.Commit();
            var snapshot2 = _snapshot.Clone();
            ret = NativeContract.RoleDesignation.Call(
                snapshot2,
                "getDesignatedByRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.StateValidator) },
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(1u) }
            );
            ret.Should().BeOfType<VM.Types.Array>();
            (ret as VM.Types.Array).Count.Should().Be(7);
            (ret as VM.Types.Array)[0].GetSpan().ToHexString().Should().Be(validators[0].ToArray().ToHexString());
            (ret as VM.Types.Array)[1].GetSpan().ToHexString().Should().Be(validators[1].ToArray().ToHexString());
            (ret as VM.Types.Array)[2].GetSpan().ToHexString().Should().Be(validators[2].ToArray().ToHexString());
            (ret as VM.Types.Array)[3].GetSpan().ToHexString().Should().Be(validators[3].ToArray().ToHexString());
            (ret as VM.Types.Array)[4].GetSpan().ToHexString().Should().Be(validators[4].ToArray().ToHexString());
            (ret as VM.Types.Array)[5].GetSpan().ToHexString().Should().Be(validators[5].ToArray().ToHexString());
            (ret as VM.Types.Array)[6].GetSpan().ToHexString().Should().Be(validators[6].ToArray().ToHexString());

            ret = NativeContract.RoleDesignation.Call(
                snapshot2,
                "getDesignatedByRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.StateValidator) },
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(0) }
            );
            ret.Should().BeOfType<VM.Types.Array>();
            (ret as VM.Types.Array).Count.Should().Be(0);
        }
    }
}
