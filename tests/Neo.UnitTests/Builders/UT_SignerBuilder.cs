// Copyright (C) 2015-2025 The Neo Project.
//
// UT_SignerBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Builders;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads.Conditions;

namespace Neo.UnitTests.Builders
{
    [TestClass]
    public class UT_SignerBuilder
    {
        [TestMethod]
        public void TestAccount()
        {
            var signer = SignerBuilder.Create(UInt160.Zero)
                .Build();

            Assert.IsNotNull(signer);
            Assert.AreEqual(UInt160.Zero, signer.Account);
        }

        [TestMethod]
        public void TestAllowContract()
        {
            var signer = SignerBuilder.Create(UInt160.Zero)
                .AllowContract(UInt160.Zero)
                .Build();

            Assert.HasCount(1, signer.AllowedContracts);
            Assert.AreEqual(UInt160.Zero, signer.AllowedContracts[0]);
        }

        [TestMethod]
        public void TestAllowGroup()
        {
            var myPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
            var signer = SignerBuilder.Create(UInt160.Zero)
                .AllowGroup(myPublicKey)
                .Build();

            Assert.HasCount(1, signer.AllowedGroups);
            Assert.AreEqual(myPublicKey, signer.AllowedGroups[0]);
        }

        [TestMethod]
        public void TestAddWitnessScope()
        {
            var signer = SignerBuilder.Create(UInt160.Zero)
                .AddWitnessScope(WitnessScope.Global)
                .Build();

            Assert.AreEqual(WitnessScope.Global, signer.Scopes);
        }

        [TestMethod]
        public void TestAddWitnessRule()
        {
            var signer = SignerBuilder.Create(UInt160.Zero)
                .AddWitnessRule(WitnessRuleAction.Allow, rb =>
                {
                    rb.AddCondition(cb =>
                    {
                        cb.ScriptHash(UInt160.Zero);
                    });
                })
                .Build();

            Assert.HasCount(1, signer.Rules);
            Assert.AreEqual(WitnessRuleAction.Allow, signer.Rules[0].Action);
            Assert.IsInstanceOfType<ScriptHashCondition>(signer.Rules[0].Condition);
        }
    }
}
