using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_RoleManagement
    {
        private DataCache _snapshot;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshot = TestBlockchain.GetTestSnapshot();
        }

        [TestMethod]
        public void TestSetAndGet()
        {
            var snapshot1 = _snapshot.CreateSnapshot();
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot1);
            ECPoint[] validators = NativeContract.NEO.ComputeNextBlockValidators(snapshot1, TestProtocolSettings.Default);
            List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();
            EventHandler<NotifyEventArgs> ev = (o, e) => notifications.Add(e);
            ApplicationEngine.Notify += ev;
            var ret = NativeContract.RoleManagement.Call(
                snapshot1,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                new Block { Header = new Header() },
                "designateAsRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.StateValidator) },
                new ContractParameter(ContractParameterType.Array) { Value = validators.Select(p => new ContractParameter(ContractParameterType.ByteArray) { Value = p.ToArray() }).ToList() }
            );
            snapshot1.Commit();
            ApplicationEngine.Notify -= ev;
            notifications.Count.Should().Be(1);
            notifications[0].EventName.Should().Be("Designation");
            var snapshot2 = _snapshot.CreateSnapshot();
            ret = NativeContract.RoleManagement.Call(
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

            ret = NativeContract.RoleManagement.Call(
                snapshot2,
                "getDesignatedByRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.StateValidator) },
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(0) }
            );
            ret.Should().BeOfType<VM.Types.Array>();
            (ret as VM.Types.Array).Count.Should().Be(0);
        }

        private void ApplicationEngine_Notify(object sender, NotifyEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
