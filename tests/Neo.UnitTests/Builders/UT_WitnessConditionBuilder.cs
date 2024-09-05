// Copyright (C) 2015-2024 The Neo Project.
//
// UT_WitnessConditionBuilder.cs file belongs to the neo project and is free
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
using Neo.Network.P2P.Payloads.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.UnitTests.Builders
{
    [TestClass]
    public class UT_WitnessConditionBuilder
    {
        [TestMethod]
        public void TestAndCondition()
        {
            var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
            var expectedContractHash = UInt160.Zero;
            var condition = WitnessConditionBuilder.Create()
                .And()
                .CalledByContract(expectedContractHash)
                .CalledByGroup(expectedPublicKey)
                .Build();

            var actual = condition as AndCondition;

            Assert.IsNotNull(actual);
            Assert.IsInstanceOfType<AndCondition>(condition);
            Assert.AreEqual(2, actual.Expressions.Length);
            Assert.IsInstanceOfType<CalledByContractCondition>(actual.Expressions[0]);
            Assert.IsInstanceOfType<CalledByGroupCondition>(actual.Expressions[1]);
            Assert.AreEqual(expectedContractHash, (actual.Expressions[0] as CalledByContractCondition).Hash);
            Assert.AreEqual(expectedPublicKey, (actual.Expressions[1] as CalledByGroupCondition).Group);
        }

        [TestMethod]
        public void TestBoolean()
        {
            var condition = WitnessConditionBuilder.Create()
                .Boolean(true)
                .Build();

            var actual = condition as BooleanCondition;

            Assert.IsNotNull(actual);
            Assert.IsInstanceOfType<BooleanCondition>(condition);
            Assert.IsTrue(actual.Expression);
        }

        [TestMethod]
        public void TestCalledByContract()
        {
            var expectedContractHash = UInt160.Zero;
            var condition = WitnessConditionBuilder.Create()
                .CalledByContract(expectedContractHash)
                .Build();

            var actual = condition as CalledByContractCondition;

            Assert.IsNotNull(actual);
            Assert.IsInstanceOfType<CalledByContractCondition>(condition);
            Assert.AreEqual(expectedContractHash, actual.Hash);
        }

        [TestMethod]
        public void TestCalledByEntry()
        {
            var condition = WitnessConditionBuilder.Create()
                .CalledByEntry()
                .Build();

            var actual = condition as CalledByEntryCondition;

            Assert.IsNotNull(actual);
            Assert.IsInstanceOfType<CalledByEntryCondition>(condition);
        }

        [TestMethod]
        public void TestCalledByGroup()
        {
            var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
            var condition = WitnessConditionBuilder.Create()
                .CalledByGroup(expectedPublicKey)
                .Build();

            var actual = condition as CalledByGroupCondition;

            Assert.IsNotNull(actual);
            Assert.IsInstanceOfType<CalledByGroupCondition>(condition);
            Assert.AreEqual(expectedPublicKey, actual.Group);
        }

        [TestMethod]
        public void TestGroup()
        {
            var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
            var condition = WitnessConditionBuilder.Create()
                .Group(expectedPublicKey)
                .Build();

            var actual = condition as GroupCondition;

            Assert.IsNotNull(actual);
            Assert.IsInstanceOfType<GroupCondition>(condition);
            Assert.AreEqual(expectedPublicKey, actual.Group);
        }

        [TestMethod]
        public void TestScriptHash()
        {
            var expectedContractHash = UInt160.Zero;
            var condition = WitnessConditionBuilder.Create()
                .ScriptHash(expectedContractHash)
                .Build();

            var actual = condition as ScriptHashCondition;

            Assert.IsNotNull(actual);
            Assert.IsInstanceOfType<ScriptHashCondition>(condition);
            Assert.AreEqual(expectedContractHash, actual.Hash);
        }

        [TestMethod]
        public void TestNotConditionWithAndCondition()
        {
            var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
            var expectedContractHash = UInt160.Zero;
            var condition = WitnessConditionBuilder.Create()
                .Not()
                .And()
                .CalledByContract(expectedContractHash)
                .CalledByGroup(expectedPublicKey)
                .Build();

            var actual = condition as NotCondition;
            var actualAndCondition = actual.Expression as AndCondition;

            Assert.IsNotNull(actual);
            Assert.IsInstanceOfType<NotCondition>(condition);
            Assert.IsInstanceOfType<AndCondition>(actual.Expression);
            Assert.AreEqual(2, actualAndCondition.Expressions.Length);
            Assert.IsInstanceOfType<CalledByContractCondition>(actualAndCondition.Expressions[0]);
            Assert.IsInstanceOfType<CalledByGroupCondition>(actualAndCondition.Expressions[1]);
            Assert.AreEqual(expectedContractHash, (actualAndCondition.Expressions[0] as CalledByContractCondition).Hash);
            Assert.AreEqual(expectedPublicKey, (actualAndCondition.Expressions[1] as CalledByGroupCondition).Group);
        }

        [TestMethod]
        public void TestNotConditionWithOrCondition()
        {
            var expectedPublicKey = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
            var expectedContractHash = UInt160.Zero;
            var condition = WitnessConditionBuilder.Create()
                .Not()
                .Or()
                .CalledByContract(expectedContractHash)
                .CalledByGroup(expectedPublicKey)
                .Build();

            var actual = condition as NotCondition;
            var actualOrCondition = actual.Expression as OrCondition;

            Assert.IsNotNull(actual);
            Assert.IsInstanceOfType<NotCondition>(condition);
            Assert.IsInstanceOfType<OrCondition>(actual.Expression);
            Assert.AreEqual(2, actualOrCondition.Expressions.Length);
            Assert.IsInstanceOfType<CalledByContractCondition>(actualOrCondition.Expressions[0]);
            Assert.IsInstanceOfType<CalledByGroupCondition>(actualOrCondition.Expressions[1]);
            Assert.AreEqual(expectedContractHash, (actualOrCondition.Expressions[0] as CalledByContractCondition).Hash);
            Assert.AreEqual(expectedPublicKey, (actualOrCondition.Expressions[1] as CalledByGroupCondition).Group);
        }
    }
}
