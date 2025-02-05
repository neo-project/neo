// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RoleManagement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
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
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
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

            List<Role> roles = new List<Role>() { Role.StateValidator, Role.Oracle, Role.NeoFSAlphabetNode, Role.P2PNotary };
            foreach (var role in roles)
            {
                var snapshot1 = _snapshotCache.CloneCache();
                UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot1);
                List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();
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
                Assert.AreEqual(1, notifications.Count);
                Assert.AreEqual("Designation", notifications[0].EventName);
                var snapshot2 = _snapshotCache.CloneCache();
                ret = NativeContract.RoleManagement.Call(
                    snapshot2,
                    "getDesignatedByRole",
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)role) },
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(1u) }
                );
                Assert.IsInstanceOfType(ret, typeof(VM.Types.Array));
                Assert.AreEqual(2, (ret as VM.Types.Array).Count);
                Assert.AreEqual(publicKeys[0].ToArray().ToHexString(), (ret as VM.Types.Array)[0].GetSpan().ToHexString());
                Assert.AreEqual(publicKeys[1].ToArray().ToHexString(), (ret as VM.Types.Array)[1].GetSpan().ToHexString());

                ret = NativeContract.RoleManagement.Call(
                    snapshot2,
                    "getDesignatedByRole",
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)role) },
                    new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger(0) }
                );
                Assert.IsInstanceOfType(ret, typeof(VM.Types.Array));
                Assert.AreEqual(0, (ret as VM.Types.Array).Count);
            }
        }

        private void ApplicationEngine_Notify(object sender, NotifyEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
