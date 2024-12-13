// Copyright (C) 2015-2024 The Neo Project.
//
// UT_WitnessRule.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_WitnessRule
    {
        [TestMethod]
        public void Test_IEquatable()
        {
            var expected = new WitnessRule
            {
                Action = WitnessRuleAction.Allow,
                Condition = new BooleanCondition
                {
                    Expression = true,
                }
            };

            var actual = new WitnessRule
            {
                Action = WitnessRuleAction.Allow,
                Condition = new BooleanCondition
                {
                    Expression = true,
                }
            };

            var notEqual = new WitnessRule
            {
                Action = WitnessRuleAction.Deny,
                Condition = new BooleanCondition
                {
                    Expression = false,
                }
            };

            Assert.IsTrue(expected.Equals(expected));

            Assert.AreEqual(expected, actual);
            Assert.IsTrue(expected == actual);
            Assert.IsTrue(expected.Equals(actual));

            Assert.AreNotEqual(expected, notEqual);
            Assert.IsTrue(expected != notEqual);
            Assert.IsFalse(expected.Equals(notEqual));

            Assert.IsFalse(expected == null);
            Assert.IsFalse(null == expected);
            Assert.AreNotEqual(expected, null);
            Assert.IsFalse(expected.Equals(null));
        }
    }
}
