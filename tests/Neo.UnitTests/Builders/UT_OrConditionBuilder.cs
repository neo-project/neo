// Copyright (C) 2015-2026 The Neo Project.
//
// UT_OrConditionBuilder.cs file belongs to the neo project and is free
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
    public class UT_OrConditionBuilder
    {
        private static readonly ECPoint s_point = ECPoint.Parse(
            "021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);

        [TestMethod]
        public void CreateEmpty_BuildsEmptyExpressions()
        {
            var condition = OrConditionBuilder.CreateEmpty().Build();
            Assert.IsNotNull(condition);
            Assert.IsEmpty(condition.Expressions);
        }

        [TestMethod]
        public void Boolean_CalledByEntry_ScriptHash_Group_CalledByContract_CalledByGroup()
        {
            var hash = UInt160.Zero;
            var condition = OrConditionBuilder.CreateEmpty()
                .Boolean(false)
                .CalledByEntry()
                .ScriptHash(hash)
                .Group(s_point)
                .CalledByContract(hash)
                .CalledByGroup(s_point)
                .Build();

            Assert.HasCount(6, condition.Expressions);
            Assert.IsInstanceOfType<BooleanCondition>(condition.Expressions[0]);
            Assert.IsFalse(((BooleanCondition)condition.Expressions[0]).Expression);
            Assert.IsInstanceOfType<CalledByEntryCondition>(condition.Expressions[1]);
            Assert.IsInstanceOfType<ScriptHashCondition>(condition.Expressions[2]);
            Assert.IsInstanceOfType<GroupCondition>(condition.Expressions[3]);
            Assert.IsInstanceOfType<CalledByContractCondition>(condition.Expressions[4]);
            Assert.IsInstanceOfType<CalledByGroupCondition>(condition.Expressions[5]);
            Assert.AreEqual(s_point, ((CalledByGroupCondition)condition.Expressions[5]).Group);
        }

        [TestMethod]
        public void Nested_Or_And_Or()
        {
            var hash = UInt160.Zero;
            var condition = OrConditionBuilder.CreateEmpty()
                .And(and =>
                {
                    and.Boolean(true);
                    and.ScriptHash(hash);
                })
                .Or(or =>
                {
                    or.Boolean(false);
                    or.Group(s_point);
                })
                .Build();

            Assert.HasCount(2, condition.Expressions);
            Assert.IsInstanceOfType<AndCondition>(condition.Expressions[0]);
            Assert.IsInstanceOfType<OrCondition>(condition.Expressions[1]);

            var nestedAnd = (AndCondition)condition.Expressions[0];
            Assert.HasCount(2, nestedAnd.Expressions);
            Assert.IsTrue(((BooleanCondition)nestedAnd.Expressions[0]).Expression);
            Assert.AreEqual(hash, ((ScriptHashCondition)nestedAnd.Expressions[1]).Hash);

            var nestedOr = (OrCondition)condition.Expressions[1];
            Assert.HasCount(2, nestedOr.Expressions);
            Assert.IsFalse(((BooleanCondition)nestedOr.Expressions[0]).Expression);
            Assert.AreEqual(s_point, ((GroupCondition)nestedOr.Expressions[1]).Group);
        }
    }
}
