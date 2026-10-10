// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine.Culture.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
        [TestMethod]
        public void TestPickItemCatchMessageWithHuyao()
        {
            const uint GorgonEnable = 10u;
            const uint HuyaoEnable = 20u;
            var settings = ProtocolSettings.Default with
            {
                Hardforks = ProtocolSettings.Default.Hardforks
                    .SetItem(Hardfork.HF_Gorgon, GorgonEnable)
                    .SetItem(Hardfork.HF_Huyao, HuyaoEnable)
            };
            var negativeKeyScript = BuildMissingMapKeyCatchScript(BigInteger.MinusOne * 5);
            var positiveKeyScript = BuildMissingMapKeyCatchScript(new BigInteger(5));

            Assert.AreEqual(
                "Key \u22125 not found in Map.",
                ExecuteCaughtMessage(negativeKeyScript, settings, HuyaoEnable - 1, "sv-SE"));

            foreach (var culture in new[] { "en-US", "sv-SE", "nb-NO", "lt-LT" })
            {
                Assert.AreEqual(
                    "Key -5 not found in Map.",
                    ExecuteCaughtMessage(negativeKeyScript, settings, HuyaoEnable, culture));
            }

            var positiveEnUs = ExecuteCaughtMessage(positiveKeyScript, settings, HuyaoEnable, "en-US");
            var positiveSvSe = ExecuteCaughtMessage(positiveKeyScript, settings, HuyaoEnable, "sv-SE");
            Assert.AreEqual("Key 5 not found in Map.", positiveEnUs);
            Assert.AreEqual(positiveEnUs, positiveSvSe);
        }

        [TestMethod]
        public void TestSetItemCatchMessageWithHuyao()
        {
            const uint GorgonEnable = 10u;
            const uint HuyaoEnable = 20u;
            var settings = ProtocolSettings.Default with
            {
                Hardforks = ProtocolSettings.Default.Hardforks
                    .SetItem(Hardfork.HF_Gorgon, GorgonEnable)
                    .SetItem(Hardfork.HF_Huyao, HuyaoEnable)
            };
            var script = BuildOutOfRangeSetItemCatchScript(BigInteger.MinusOne * 5);

            Assert.AreEqual(
                "The index of VMArray is out of range, \u22125/[0, 0).",
                ExecuteCaughtMessage(script, settings, HuyaoEnable - 1, "sv-SE"));

            var enUs = ExecuteCaughtMessage(script, settings, HuyaoEnable, "en-US");
            var svSe = ExecuteCaughtMessage(script, settings, HuyaoEnable, "sv-SE");
            Assert.AreEqual("The index of VMArray is out of range, -5/[0, 0).", enUs);
            Assert.AreEqual(enUs, svSe);
        }

        private static byte[] BuildMissingMapKeyCatchScript(BigInteger key)
        {
            return BuildCatchScript(script =>
            {
                script.Emit(OpCode.NEWMAP);
                script.EmitPush(key);
                script.Emit(OpCode.PICKITEM);
            });
        }

        private static byte[] BuildOutOfRangeSetItemCatchScript(BigInteger key)
        {
            return BuildCatchScript(script =>
            {
                script.Emit(OpCode.NEWARRAY0);
                script.EmitPush(key);
                script.EmitPush(1);
                script.Emit(OpCode.SETITEM);
            });
        }

        private static byte[] BuildCatchScript(Action<ScriptBuilder> emitBody)
        {
            using var body = new ScriptBuilder();
            emitBody(body);
            var bodyLength = body.ToArray().Length;
            var catchOffset = 3 + bodyLength + 2;
            var endAddress = catchOffset + 2;
            var firstEndTryOffset = endAddress - (3 + bodyLength);
            var secondEndTryOffset = endAddress - catchOffset;

            using var script = new ScriptBuilder();
            script.Emit(OpCode.TRY, [(byte)(sbyte)catchOffset, 0]);
            emitBody(script);
            script.Emit(OpCode.ENDTRY, [(byte)(sbyte)firstEndTryOffset]);
            script.Emit(OpCode.ENDTRY, [(byte)(sbyte)secondEndTryOffset]);
            script.Emit(OpCode.RET);
            return script.ToArray();
        }

        private static string ExecuteCaughtMessage(byte[] script, ProtocolSettings settings, uint index, string culture)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                using var engine = Execute(script, settings, index);

                Assert.AreEqual(VMState.HALT, engine.State);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.AreEqual(culture, CultureInfo.CurrentCulture.Name);
                return Encoding.UTF8.GetString(engine.ResultStack.Peek().GetSpan());
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
