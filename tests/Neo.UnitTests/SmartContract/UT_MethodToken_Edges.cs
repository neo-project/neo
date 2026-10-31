// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MethodToken_Edges.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// Edges not covered by UT_MethodToken serialize tests.
    /// </summary>
    [TestClass]
    public class UT_MethodToken_Edges
    {
        [TestMethod]
        public void ToJson_IncludesAllFields()
        {
            var token = new MethodToken
            {
                Hash = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"),
                Method = "transfer",
                ParametersCount = 3,
                HasReturnValue = false,
                CallFlags = CallFlags.ReadOnly
            };

            var json = token.ToJson();
            Assert.AreEqual(token.Hash.ToString(), json["hash"].GetString());
            Assert.AreEqual("transfer", json["method"].GetString());
            Assert.AreEqual(3, (int)json["paramcount"].GetNumber());
            Assert.IsFalse(json["hasreturnvalue"].GetBoolean());
            Assert.IsNotNull(json["callflags"]);
        }

        [TestMethod]
        public void Size_MatchesSerializedLength()
        {
            var token = new MethodToken
            {
                Hash = UInt160.Zero,
                Method = "foo",
                ParametersCount = 0,
                HasReturnValue = true,
                CallFlags = CallFlags.None
            };

            Assert.AreEqual(token.ToArray().Length, token.Size);
        }

        [TestMethod]
        public void Deserialize_MethodStartingWithUnderscore_Throws()
        {
            var token = new MethodToken
            {
                Hash = UInt160.Zero,
                Method = "ok",
                ParametersCount = 0,
                HasReturnValue = false,
                CallFlags = CallFlags.None
            };
            var bytes = token.ToArray();

            // Method is a var string after the 20-byte hash; rewrite to "_bad"
            // Easier: serialize via manual construction of a bad method name
            token.Method = "_init";
            // Serialize writes the method as-is; deserialize validates
            Assert.ThrowsExactly<FormatException>(() => _ = token.ToArray().AsSerializable<MethodToken>());
        }
    }
}
