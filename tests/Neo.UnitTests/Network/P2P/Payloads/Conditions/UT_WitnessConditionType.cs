// Copyright (C) 2015-2026 The Neo Project.
//
// UT_WitnessConditionType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads.Conditions;

namespace Neo.UnitTests.Network.P2P.Payloads.Conditions
{
    [TestClass]
    public class UT_WitnessConditionType
    {
        [TestMethod]
        public void Values_MatchSpecification()
        {
            Assert.AreEqual(0x00, (byte)WitnessConditionType.Boolean);
            Assert.AreEqual(0x01, (byte)WitnessConditionType.Not);
            Assert.AreEqual(0x02, (byte)WitnessConditionType.And);
            Assert.AreEqual(0x03, (byte)WitnessConditionType.Or);
            Assert.AreEqual(0x18, (byte)WitnessConditionType.ScriptHash);
            Assert.AreEqual(0x19, (byte)WitnessConditionType.Group);
            Assert.AreEqual(0x20, (byte)WitnessConditionType.CalledByEntry);
            Assert.AreEqual(0x28, (byte)WitnessConditionType.CalledByContract);
            Assert.AreEqual(0x29, (byte)WitnessConditionType.CalledByGroup);
        }

        [TestMethod]
        public void AllValues_AreDefined()
        {
            foreach (WitnessConditionType value in System.Enum.GetValues<WitnessConditionType>())
                Assert.IsTrue(System.Enum.IsDefined(value));
        }
    }
}
