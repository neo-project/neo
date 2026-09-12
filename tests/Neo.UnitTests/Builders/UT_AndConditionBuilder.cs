// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AndConditionBuilder.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.Builders
{
    [TestClass]
    public class UT_AndConditionBuilder
    {
        private static readonly ECPoint s_point = ECPoint.Parse(
            "021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);

        [TestMethod]
        public void CreateEmpty_BuildsEmptyExpressions()
        {
            var condition = AndConditionBuilder.CreateEmpty().Build();
            Assert.IsNotNull(condition);
            Assert.IsEmpty(condition.Expressions);
        }

        [TestMethod]
        public void Boolean_CalledByEntry_ScriptHash_Group_CalledByContract()
        {
            var hash = UInt160.Zero;
            var condition = AndConditionBuilder.CreateEmpty()
                .Boolean(true)
                .CalledByEntry()
                .ScriptHash(hash)
                .Group(s_point)
                .CalledByContract(hash)
                .Build();

            Assert.HasCount(5, condition.Expressions);
            Assert.IsInstanceOfType<BooleanCondition>(condition.Expressions[0]);
            Assert.IsTrue(((BooleanCondition)condition.Expressions[0]).Expression);
            Assert.IsInstanceOfType<CalledByEntryCondition>(condition.Expressions[1]);
            Assert.IsInstanceOfType<ScriptHashCondition>(condition.Expressions[2]);
            Assert.AreEqual(hash, ((ScriptHashCondition)condition.Expressions[2]).Hash);
            Assert.IsInstanceOfType<GroupCondition>(condition.Expressions[3]);
            Assert.AreEqual(s_point, ((GroupCondition)condition.Expressions[3]).Group);
            Assert.IsInstanceOfType<CalledByContractCondition>(condition.Expressions[4]);
            Assert.AreEqual(hash, ((CalledByContractCondition)condition.Expressions[4]).Hash);
        }

        [TestMethod]
        public void Nested_And_And_Or()
        {
            var hash = UInt160.Zero;
            var condition = AndConditionBuilder.CreateEmpty()
                .And(inner =>
                {
                    inner.Boolean(false);
                    inner.CalledByContract(hash);
                })
                .Or(or =>
                {
                    or.Boolean(true);
                    or.CalledByGroup(s_point);
                })
                .Build();

            Assert.HasCount(2, condition.Expressions);
            Assert.IsInstanceOfType<AndCondition>(condition.Expressions[0]);
            Assert.IsInstanceOfType<OrCondition>(condition.Expressions[1]);

            var nestedAnd = (AndCondition)condition.Expressions[0];
            Assert.HasCount(2, nestedAnd.Expressions);
            Assert.IsFalse(((BooleanCondition)nestedAnd.Expressions[0]).Expression);

            var nestedOr = (OrCondition)condition.Expressions[1];
            Assert.HasCount(2, nestedOr.Expressions);
            Assert.IsTrue(((BooleanCondition)nestedOr.Expressions[0]).Expression);
            Assert.AreEqual(s_point, ((CalledByGroupCondition)nestedOr.Expressions[1]).Group);
        }
    }
}
