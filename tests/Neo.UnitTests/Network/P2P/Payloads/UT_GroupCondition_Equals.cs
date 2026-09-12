// Copyright (C) 2015-2026 The Neo Project.
//
// UT_GroupCondition_Equals.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads.Conditions;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_GroupCondition_Equals
    {
        private static readonly ECPoint s_point = ECPoint.Parse(
            "03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);

        [TestMethod]
        public void GroupCondition_Equals_AfterDeserialize_UsesValueEquality()
        {
            // ECPoint has no operator==; Equals must use Group.Equals so deserialized
            // instances with equal points compare equal (not reference equality).
            var original = new GroupCondition { Group = s_point };
            var reader = new MemoryReader(original.ToArray());
            var clone = (GroupCondition)WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);

            Assert.IsTrue(original.Group.Equals(clone.Group));
            Assert.IsTrue(original.Equals(clone));
            Assert.IsTrue(original == clone);
            Assert.AreEqual(original.GetHashCode(), clone.GetHashCode());
        }

        [TestMethod]
        public void CalledByGroupCondition_Equals_AfterDeserialize_UsesValueEquality()
        {
            var original = new CalledByGroupCondition { Group = s_point };
            var reader = new MemoryReader(original.ToArray());
            var clone = (CalledByGroupCondition)WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);

            Assert.IsTrue(original.Group.Equals(clone.Group));
            Assert.IsTrue(original.Equals(clone));
            Assert.IsTrue(original == clone);
            Assert.AreEqual(original.GetHashCode(), clone.GetHashCode());
        }
    }
}
