// Copyright (C) 2015-2026 The Neo Project.
//
// UT_WitnessCondition_Serialize.cs file belongs to the neo project and is free
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
using Neo.Json;
using Neo.Network.P2P.Payloads.Conditions;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    /// <summary>
    /// Serialize / JSON / StackItem coverage for leaf witness conditions.
    /// Kept in a dedicated file so coverage PRs do not touch UT_WitnessCondition.cs.
    /// </summary>
    [TestClass]
    public class UT_WitnessCondition_Serialize
    {
        private static readonly ECPoint s_point = ECPoint.Parse(
            "03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);

        [TestMethod]
        public void NotCondition_RoundTrip_Serialize_Json_StackItem_HashCode()
        {
            var original = new NotCondition
            {
                Expression = new BooleanCondition { Expression = true }
            };

            Assert.AreEqual(WitnessConditionType.Not, original.Type);
            Assert.AreEqual(1 + original.Expression.Size, original.Size);

            var bytes = original.ToArray();
            var reader = new MemoryReader(bytes);
            var clone = (NotCondition)WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            Assert.AreEqual(original, clone);
            Assert.AreEqual(original.GetHashCode(), clone.GetHashCode());

            var json = original.ToJson();
            Assert.AreEqual("Not", json["type"]!.GetString());
            var fromJson = (NotCondition)WitnessCondition.FromJson(json, WitnessCondition.MaxNestingDepth);
            Assert.AreEqual(original, fromJson);

            var stack = (Array)original.ToStackItem();
            Assert.IsTrue(stack.Count >= 2);

            Assert.IsTrue(original == clone);
            Assert.IsFalse(original != clone);
            Assert.IsFalse(original.Equals((object)null));
            Assert.IsFalse(original.Equals((object)"x"));
        }

        [TestMethod]
        public void ScriptHashCondition_RoundTrip_Serialize_Json_StackItem()
        {
            var hash = UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf");
            var original = new ScriptHashCondition { Hash = hash };

            Assert.AreEqual(WitnessConditionType.ScriptHash, original.Type);
            Assert.AreEqual(1 + UInt160.Length, original.Size);

            var bytes = original.ToArray();
            var reader = new MemoryReader(bytes);
            var clone = (ScriptHashCondition)WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            Assert.AreEqual(original, clone);
            Assert.AreEqual(original.GetHashCode(), clone.GetHashCode());

            var json = original.ToJson();
            Assert.AreEqual(hash.ToString(), json["hash"].GetString());
            var fromJson = (ScriptHashCondition)WitnessCondition.FromJson(json, WitnessCondition.MaxNestingDepth);
            Assert.AreEqual(hash, fromJson.Hash);

            var stack = (Array)original.ToStackItem();
            Assert.AreEqual(hash, new UInt160(stack[stack.Count - 1].GetSpan()));
        }

        [TestMethod]
        public void GroupCondition_RoundTrip_Serialize_Json_StackItem()
        {
            var original = new GroupCondition { Group = s_point };

            Assert.AreEqual(WitnessConditionType.Group, original.Type);
            Assert.AreEqual(1 + s_point.Size, original.Size);

            var bytes = original.ToArray();
            var reader = new MemoryReader(bytes);
            var clone = (GroupCondition)WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            Assert.AreEqual(original.Type, clone.Type);
            Assert.IsTrue(original.Group.Equals(clone.Group));

            var json = original.ToJson();
            Assert.AreEqual(s_point.ToString(), json["group"].GetString());
            var fromJson = (GroupCondition)WitnessCondition.FromJson(json, WitnessCondition.MaxNestingDepth);
            Assert.IsTrue(s_point.Equals(fromJson.Group));

            var stack = (Array)original.ToStackItem();
            Assert.IsTrue(stack.Count >= 2);
        }

        [TestMethod]
        public void CalledByGroupCondition_RoundTrip_Serialize_Json_StackItem()
        {
            var original = new CalledByGroupCondition { Group = s_point };

            Assert.AreEqual(WitnessConditionType.CalledByGroup, original.Type);
            Assert.AreEqual(1 + s_point.Size, original.Size);

            var bytes = original.ToArray();
            var reader = new MemoryReader(bytes);
            var clone = (CalledByGroupCondition)WitnessCondition.DeserializeFrom(ref reader, WitnessCondition.MaxNestingDepth);
            Assert.AreEqual(original.Type, clone.Type);
            Assert.IsTrue(original.Group.Equals(clone.Group));

            var json = original.ToJson();
            Assert.AreEqual(s_point.ToString(), json["group"].GetString());
            var fromJson = (CalledByGroupCondition)WitnessCondition.FromJson(json, WitnessCondition.MaxNestingDepth);
            Assert.IsTrue(s_point.Equals(fromJson.Group));

            var stack = (Array)original.ToStackItem();
            Assert.IsTrue(stack.Count >= 2);
        }

        [TestMethod]
        public void NotCondition_ObjectEquals_And_Operators_WithNull()
        {
            var a = new NotCondition { Expression = new BooleanCondition { Expression = false } };
            var b = new NotCondition { Expression = new BooleanCondition { Expression = false } };
            var c = new NotCondition { Expression = new BooleanCondition { Expression = true } };

            Assert.IsTrue(a.Equals((object)b));
            Assert.IsFalse(a.Equals((object)c));
            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
        }
    }
}
