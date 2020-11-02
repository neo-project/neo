using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Designate;
using Neo.UnitTests.Extensions;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_DesignateContract
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestSetAndGet()
        {
            using var snapshot1 = Blockchain.Singleton.GetSnapshot();
            snapshot1.PersistingBlock = new Block
            {
                Index = 1,
            };
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot1);
            ECPoint[] validators = NativeContract.NEO.ComputeNextBlockValidators(snapshot1);
            var ret = NativeContract.Designate.Call(
                snapshot1,
                new Nep5NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                "designateAsRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.StateValidator) },
                new ContractParameter(ContractParameterType.Array) { Value = validators.Select(p => new ContractParameter(ContractParameterType.ByteArray) { Value = p.ToArray() }).ToList() }
            );
            snapshot1.Commit();
            using var snapshot2 = Blockchain.Singleton.GetSnapshot();
            ret = NativeContract.Designate.Call(
                snapshot2,
                "getDesignatedByRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.StateValidator) },
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(2u) }
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

            ret = NativeContract.Designate.Call(
                snapshot2,
                "getDesignatedByRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.StateValidator) },
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(2) }
            );
            ret.Should().BeOfType<VM.Types.Array>();
            (ret as VM.Types.Array).Count.Should().Be(7);

            ret = NativeContract.Designate.Call(
                snapshot2,
                "getDesignatedByRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.StateValidator) },
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(1) }
            );
            ret.Should().BeOfType<VM.Types.Array>();
            (ret as VM.Types.Array).Count.Should().Be(0);
        }
    }
}
