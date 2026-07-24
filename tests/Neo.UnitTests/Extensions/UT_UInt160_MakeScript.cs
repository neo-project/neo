// Copyright (C) 2015-2026 The Neo Project.
//
// UT_UInt160_MakeScript.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.VM;
using System;
using System.Numerics;

namespace Neo.UnitTests.Extensions
{
    [TestClass]
    public class UT_UInt160_MakeScript
    {
        [TestMethod]
        public void MakeScript_MatchesEmitDynamicCall()
        {
            var hash = UInt160.Parse("0x0000000000000000000000000000000000000001");
            using var sb = new ScriptBuilder();
            sb.EmitDynamicCall(hash, "balanceOf", hash);
            var expected = sb.ToArray();

            var actual = hash.MakeScript("balanceOf", hash);
            Assert.IsTrue(expected.AsSpan().SequenceEqual(actual));
        }

        [TestMethod]
        public void MakeScript_WithNoArgs_IsNonEmpty()
        {
            var script = UInt160.Zero.MakeScript("symbol");
            Assert.IsTrue(script.Length > 0);
        }

        [TestMethod]
        public void MakeScript_WithIntegerArg()
        {
            var script = UInt160.Zero.MakeScript("method", (BigInteger)42);
            Assert.IsTrue(script.Length > 5);
        }
    }
}
