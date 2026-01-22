// Copyright (C) 2015-2026 The Neo Project.
//
// UT_RoleManagement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Array = Neo.VM.Types.Array;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_RoleManagement
    {
        [TestInitialize]
        public void TestSetup()
        {
            var system = TestBlockchain.GetSystem();
            system.ResetStore();
        }

        [TestMethod]
        public void TestSetAndGet()
        {
            var privateKey1 = new byte[32];
            var rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            var key1 = new KeyPair(privateKey1);
            var privateKey2 = new byte[32];
            var rng2 = RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            var key2 = new KeyPair(privateKey2);
            var publicKeys = new ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = [.. publicKeys.OrderBy(p => p)];

            List<Role> roles = [Role.StateValidator, Role.Oracle, Role.NeoFSAlphabetNode, Role.P2PNotary];
            foreach (var role in roles)
            {
                var system = new TestBlockchain.TestNeoSystem(TestProtocolSettings.Default);

                var snapshot1 = system.GetTestSnapshotCache(false);
                var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot1);
                List<NotifyEventArgs> notifications = [];
                void Ev(ApplicationEngine o, NotifyEventArgs e) => notifications.Add(e);

                var ret = NativeContract.RoleManagement.Call(
                    snapshot1,
                    new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                    new Block
                    {
                        Header = (Header)RuntimeHelpers.GetUninitializedObject(typeof(Header)),
                        Transactions = []
                    },
                    "designateAsRole", Ev,
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)role) },
                    new ContractParameter(ContractParameterType.Array) { Value = publicKeys.Select(p => new ContractParameter(ContractParameterType.ByteArray) { Value = p.ToArray() }).ToList() }
                );
                snapshot1.Commit();
                Assert.HasCount(1, notifications);
                Assert.AreEqual("Designation", notifications[0].EventName);

                var snapshot2 = system.GetTestSnapshotCache(false);

                ret = NativeContract.RoleManagement.Call(
                    snapshot2,
                    "getDesignatedByRole",
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)role) },
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(1u) }
                );
                Assert.IsInstanceOfType<Array>(ret);
                Assert.HasCount(2, ret as Array);
                Assert.AreEqual(publicKeys[0].ToArray().ToHexString(), (ret as Array)[0].GetSpan().ToHexString());
                Assert.AreEqual(publicKeys[1].ToArray().ToHexString(), (ret as Array)[1].GetSpan().ToHexString());

                ret = NativeContract.RoleManagement.Call(
                    snapshot2,
                    "getDesignatedByRole",
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)role) },
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(0) }
                );
                Assert.IsInstanceOfType<Array>(ret);
                Assert.IsEmpty(ret as Array);
            }
        }

        private void ApplicationEngine_Notify(object sender, NotifyEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
