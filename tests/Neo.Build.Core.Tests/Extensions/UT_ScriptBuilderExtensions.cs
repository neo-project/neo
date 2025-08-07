// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ScriptBuilderExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Attributes;
using Neo.Build.Core.Extensions;
using Neo.Extensions;
using Neo.VM;
using System.Numerics;

namespace Neo.Build.Core.Tests.Extensions
{
    [TestClass]
    public class UT_ScriptBuilderExtensions
    {
        [ContractScriptHash("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5")]
        private interface INeoToken
        {
            public BigInteger BalanceOf(UInt160 owner);
        }

        [TestMethod]
        public void TestEmitContractCall()
        {
            UInt160 expectedScriptHash = "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5";
            using var expectedScriptBuilder = new ScriptBuilder()
                .EmitDynamicCall(expectedScriptHash, "balanceOf", UInt160.Zero);
            var expectedRawData = expectedScriptBuilder.ToArray();

            using var actualScriptBuilder = new ScriptBuilder()
                .EmitContractCall<INeoToken>(n => n.BalanceOf(UInt160.Zero));
            var actualRawData = actualScriptBuilder.ToArray();

            CollectionAssert.AreEqual(expectedRawData, actualRawData);
        }
    }
}
