// Copyright (C) 2015-2025 The Neo Project.
//
// UT_WitnessRuleBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Builders;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads.Conditions;

namespace Neo.UnitTests.Builders
{
    [TestClass]
    public class UT_WitnessRuleBuilder
    {
        [TestMethod]
        public void TestCreate()
        {
            var builder = WitnessRuleBuilder.Create(WitnessRuleAction.Allow);

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void TestCondition()
        {
            var rule = WitnessRuleBuilder.Create(WitnessRuleAction.Allow)
                .AddCondition(wcb =>
                {
                    wcb.ScriptHash(UInt160.Zero);
                }).Build();

            Assert.IsNotNull(rule.Condition);
            Assert.AreEqual(WitnessRuleAction.Allow, rule.Action);
            Assert.IsInstanceOfType<ScriptHashCondition>(rule.Condition);
            Assert.AreEqual(UInt160.Zero, ((ScriptHashCondition)rule.Condition).Hash);
        }

        [TestMethod]
        public void TestCondition2()
        {
            var rule = WitnessRuleBuilder.Create(WitnessRuleAction.Allow)
                .AddCondition(wcb =>
                {
                    wcb.And(and =>
                    {
                        and.ScriptHash(UInt160.Zero);
                    });
                }).Build();

            Assert.IsNotNull(rule.Condition);
            Assert.AreEqual(WitnessRuleAction.Allow, rule.Action);
            Assert.IsInstanceOfType<AndCondition>(rule.Condition);
            Assert.IsInstanceOfType<ScriptHashCondition>((rule.Condition as AndCondition).Expressions[0]);
            Assert.AreEqual(UInt160.Zero, ((rule.Condition as AndCondition).Expressions[0] as ScriptHashCondition).Hash);
        }
    }
}
