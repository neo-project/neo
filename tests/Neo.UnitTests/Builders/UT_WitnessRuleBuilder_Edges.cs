// Copyright (C) 2015-2026 The Neo Project.
//
// UT_WitnessRuleBuilder_Edges.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Builders;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads.Conditions;
using System;

namespace Neo.UnitTests.Builders
{
    /// <summary>
    /// Edge cases not covered by UT_WitnessRuleBuilder happy-path tests.
    /// </summary>
    [TestClass]
    public class UT_WitnessRuleBuilder_Edges
    {
        [TestMethod]
        public void Build_WithoutCondition_Throws()
        {
            var builder = WitnessRuleBuilder.Create(WitnessRuleAction.Deny);
            Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());
        }

        [TestMethod]
        public void Build_Deny_WithBooleanCondition()
        {
            var rule = WitnessRuleBuilder.Create(WitnessRuleAction.Deny)
                .AddCondition(c => c.Boolean(false))
                .Build();

            Assert.AreEqual(WitnessRuleAction.Deny, rule.Action);
            Assert.IsInstanceOfType<BooleanCondition>(rule.Condition);
            Assert.IsFalse(((BooleanCondition)rule.Condition).Expression);
        }

        [TestMethod]
        public void AddCondition_OverwritesPreviousCondition()
        {
            var rule = WitnessRuleBuilder.Create(WitnessRuleAction.Allow)
                .AddCondition(c => c.Boolean(true))
                .AddCondition(c => c.ScriptHash(UInt160.Zero))
                .Build();

            Assert.IsInstanceOfType<ScriptHashCondition>(rule.Condition);
            Assert.AreEqual(UInt160.Zero, ((ScriptHashCondition)rule.Condition).Hash);
        }
    }
}
