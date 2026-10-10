// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SmartAccountCanonicalJson.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.SmartContract.Native;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_SmartAccountCanonicalJson
    {
        private static string Encode(JToken value) => Encoding.UTF8.GetString(SmartAccountCanonicalJson.Serialize(value));

        [TestMethod]
        public void SortsRawUtf16NamesRecursivelyButPreservesArraysAndUnicode()
        {
            var value = new JObject
            {
                ["\ufffd"] = 1,
                ["\ud83d\ude00"] = 2,
                ["\u20ac"] = 3,
                ["1"] = new JArray(new JObject { ["z"] = false, ["a"] = true }, null, "e\u0301", "\u00e9"),
                ["\r"] = "<>&/\u2028\u2029"
            };
            Assert.AreEqual("{\"\\r\":\"<>&/\u2028\u2029\",\"1\":[{\"a\":true,\"z\":false},null,\"e\u0301\",\"é\"],\"€\":3,\"😀\":2,\"�\":1}", Encode(value));
            Assert.AreEqual("\ufffd", value.Properties.Keys.First());
            Assert.AreEqual("{}", Encode(new JObject()));
            Assert.AreEqual("[]", Encode(new JArray()));
        }

        [TestMethod]
        public void EscapesAllControlCharactersAndNothingElse()
        {
            string input = new(Enumerable.Range(0, 32).Select(i => (char)i).ToArray());
            string expected = "\"\\u0000\\u0001\\u0002\\u0003\\u0004\\u0005\\u0006\\u0007\\b\\t\\n\\u000b\\f\\r\\u000e\\u000f\\u0010\\u0011\\u0012\\u0013\\u0014\\u0015\\u0016\\u0017\\u0018\\u0019\\u001a\\u001b\\u001c\\u001d\\u001e\\u001f\"";
            Assert.AreEqual(expected, Encode(new JString(input)));
            Assert.AreEqual("\"\\\"\\\\/\"", Encode(new JString("\"\\/")));
        }

        [TestMethod]
        public void RejectsMalformedUnicodeInKeysAndValues()
        {
            foreach (var input in new[] { "\ud800", "\udfff", "a\ud800b", "\ud800\ud800", "\udc00\ud800" })
            {
                Assert.ThrowsExactly<FormatException>(() => Encode(new JString(input)));
                Assert.ThrowsExactly<FormatException>(() => Encode(new JObject { [input] = true }));
            }
        }

        [TestMethod]
        public void RejectsCyclesDepthAndCanonicalSizeOverflowButAllowsRepeatedTrees()
        {
            var cycle = new JArray(); cycle.Add(cycle);
            Assert.ThrowsExactly<FormatException>(() => Encode(cycle));
            var objCycle = new JObject(); objCycle["self"] = objCycle;
            Assert.ThrowsExactly<FormatException>(() => Encode(objCycle));
            JToken nested = new JArray();
            for (int i = 1; i < 64; i++) nested = new JArray(nested);
            Assert.IsTrue(Encode(nested).Length > 0);
            Assert.ThrowsExactly<FormatException>(() => Encode(new JArray(nested)));
            var common = new JObject { ["x"] = 1 };
            Assert.AreEqual("[{\"x\":1},{\"x\":1}]", Encode(new JArray(common, common)));
            Assert.AreEqual(65535, SmartAccountCanonicalJson.Serialize(new JString(new string('a', 65533))).Length);
            Assert.ThrowsExactly<FormatException>(() => Encode(new JString(new string('a', 65534))));
            Assert.ThrowsExactly<FormatException>(() => Encode(new JString(new string('界', 21845))));
        }

        [TestMethod]
        public void MatchesIndependentBinary64VectorsWithoutCultureDependence()
        {
            var vectors = (JArray)JToken.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
                "SmartContract", "Native", "TestFile", "smartaccount-jcs-numbers.json")));
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                foreach (var row in vectors)
                {
                    string bits = row[0].GetString();
                    double value = BitConverter.UInt64BitsToDouble(ulong.Parse(bits, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    Assert.AreEqual(row[1].GetString(), Encode(new JNumber(value)), bits);
                }
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [TestMethod]
        public void RefusesNonFiniteNumbersAndSerializesLiterals()
        {
            foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                Assert.ThrowsExactly<FormatException>(() => SmartAccountCanonicalJson.FormatNumber(value));
            Assert.AreEqual("null", Encode(null));
            Assert.AreEqual("true", Encode(new JBoolean(true)));
            Assert.AreEqual("false", Encode(new JBoolean(false)));
        }
    }
}
