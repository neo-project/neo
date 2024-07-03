// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RoleManagement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

        [TestCleanup]
        public void Clean()
        {
            TestBlockchain.ResetStore();
        }

        [TestMethod]
        public void TestSetAndGet()
        {
            byte[] privateKey1 = new byte[32];
            var rng1 = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] privateKey2 = new byte[32];
            var rng2 = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            KeyPair key2 = new KeyPair(privateKey2);
            ECPoint[] publicKeys = new ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = publicKeys.OrderBy(p => p).ToArray();

            List<Role> roles = [Role.StateValidator, Role.Oracle, Role.NeoFSAlphabetNode, Role.P2PNotary];
            foreach (var role in roles)
            {
                var snapshot1 = _snapshot.CreateSnapshot();
                UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot1);
                List<NotifyEventArgs> notifications = [];
                EventHandler<NotifyEventArgs> ev = (o, e) => notifications.Add(e);
                ApplicationEngine.Notify += ev;
                var ret = NativeContract.RoleManagement.Call(
                    snapshot1,
                    new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                    new Block { Header = new Header() },
                    "designateAsRole",
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)role) },
                    new ContractParameter(ContractParameterType.Array) { Value = publicKeys.Select(p => new ContractParameter(ContractParameterType.ByteArray) { Value = p.ToArray() }).ToList() }
                );
                snapshot1.Commit();
                ApplicationEngine.Notify -= ev;
                notifications.Count.Should().Be(1);
                notifications[0].EventName.Should().Be("Designation");
                var snapshot2 = _snapshot.CreateSnapshot();
                ret = NativeContract.RoleManagement.Call(
                    snapshot2,
                    "getDesignatedByRole",
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)role) },
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(1u) }
                );
                ret.Should().BeOfType<VM.Types.Array>();
                (ret as VM.Types.Array).Count.Should().Be(2);
                (ret as VM.Types.Array)[0].GetSpan().ToHexString().Should().Be(publicKeys[0].ToArray().ToHexString());
                (ret as VM.Types.Array)[1].GetSpan().ToHexString().Should().Be(publicKeys[1].ToArray().ToHexString());

                ret = NativeContract.RoleManagement.Call(
                    snapshot2,
                    "getDesignatedByRole",
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)role) },
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(0) }
                );
                ret.Should().BeOfType<VM.Types.Array>();
                (ret as VM.Types.Array).Count.Should().Be(0);
            }
        }

        private void ApplicationEngine_Notify(object sender, NotifyEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
