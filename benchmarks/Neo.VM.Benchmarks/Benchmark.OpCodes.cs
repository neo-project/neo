// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark.OpCodes.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.Test.Types;
using System.Text;

namespace Neo.VM
{
    [MemoryDiagnoser]
    public class BenchmarkOpCodes
    {
        private const string Root = "../../../../../../../../../tests/Neo.VM.Tests/Tests/OpCodes/";
        private List<BenchmarkEngine?>? _benchmarkEngines;

        [Params("Arrays/CLEARITEMS.json",
            "Arrays/PACK.json",
            "Arrays/NEWARRAY_T.json",
            "Arrays/PICKITEM.json",
            "Arrays/PACKSTRUCT.json",
            "Arrays/SETITEM.json",
            "Arrays/NEWSTRUCT0.json",
            "Arrays/REVERSEITEMS.json",
            "Arrays/NEWMAP.json",
            "Arrays/HASKEY.json",
            "Arrays/NEWARRAY.json",
            "Arrays/KEYS.json",
            "Arrays/APPEND.json",
            "Arrays/REMOVE.json",
            "Arrays/PACKMAP.json",
            "Arrays/VALUES.json",
            "Arrays/NEWARRAY0.json",
            "Arrays/UNPACK.json",
            "Arrays/SIZE.json",
            "Arrays/NEWSTRUCT.json",
            "Stack/XDROP.json",
            "Stack/REVERSEN.json",
            "Stack/REVERSE4.json",
            "Stack/CLEAR.json",
            "Stack/REVERSE3.json",
            "Stack/ROT.json",
            "Stack/PICK.json",
            "Stack/NIP.json",
            "Stack/ROLL.json",
            "Stack/DEPTH.json",
            "Stack/SWAP.json",
            "Stack/TUCK.json",
            "Stack/OVER.json",
            "Stack/DROP.json",
            "Slot/STSFLD6.json",
            "Slot/INITSLOT.json",
            "Slot/LDLOC4.json",
            "Slot/LDARG.json",
            "Slot/STARG0.json",
            "Slot/LDSFLD0.json",
            "Slot/LDARG4.json",
            "Slot/LDARG5.json",
            "Slot/LDSFLD1.json",
            "Slot/LDLOC.json",
            "Slot/STARG1.json",
            "Slot/LDLOC5.json",
            "Slot/LDSFLD6.json",
            "Slot/LDARG2.json",
            "Slot/STSFLD0.json",
            "Slot/LDLOC2.json",
            "Slot/STARG6.json",
            "Slot/LDLOC3.json",
            "Slot/STSFLD1.json",
            "Slot/INITSSLOT.json",
            "Slot/LDARG3.json",
            "Slot/LDSFLD.json",
            "Slot/LDARG0.json",
            "Slot/LDSFLD4.json",
            "Slot/STARG4.json",
            "Slot/LDLOC0.json",
            "Slot/STSFLD2.json",
            "Slot/STSFLD3.json",
            "Slot/LDLOC1.json",
            "Slot/STARG5.json",
            "Slot/LDSFLD5.json",
            "Slot/LDARG1.json",
            "Slot/STARG2.json",
            "Slot/LDLOC6.json",
            "Slot/STARG.json",
            "Slot/STSFLD4.json",
            "Slot/LDARG6.json",
            "Slot/LDSFLD2.json",
            "Slot/LDSFLD3.json",
            "Slot/STLOC.json",
            "Slot/STSFLD.json",
            "Slot/STSFLD5.json",
            "Slot/STARG3.json",
            "Splice/SUBSTR.json",
            "Splice/CAT.json",
            "Arithmetic/NOT.json",
            "Splice/MEMCPY.json",
            "Splice/LEFT.json",
            "Splice/RIGHT.json",
            "Splice/NEWBUFFER.json",
            "Control/TRY_CATCH_FINALLY6.json",
            "Control/JMPLE_L.json",
            "Control/JMPEQ_L.json",
            "Control/JMPLE.json",
            "Control/JMPNE.json",
            "Control/TRY_CATCH_FINALLY7.json",
            "Control/ASSERTMSG.json",
            "Control/JMPGT.json",
            "Control/JMP_L.json",
            "Control/JMPIF_L.json",
            "Control/TRY_FINALLY.json",
            "Control/JMPNE_L.json",
            "Control/JMPIFNOT.json",
            "Control/ABORTMSG.json",
            "Control/CALL.json",
            "Control/CALL_L.json",
            "Control/JMPGE_L.json",
            "Control/RET.json",
            "Control/JMPGT_L.json",
            "Control/JMPEQ.json",
            "Control/SYSCALL.json",
            "Control/CALLA.json",
            "Control/ASSERT.json",
            "Control/TRY_CATCH_FINALLY2.json",
            "Control/TRY_CATCH_FINALLY3.json",
            "Control/NOP.json",
            "Control/JMPGE.json",
            "Control/TRY_CATCH_FINALLY10.json",
            "Control/JMPLT.json",
            "Control/JMPIF.json",
            "Control/ABORT.json",
            "Control/JMPIFNOT_L.json",
            "Control/TRY_CATCH.json",
            "Control/TRY_CATCH_FINALLY4.json",
            "Control/JMP.json",
            "Control/TRY_CATCH_FINALLY8.json",
            "Control/TRY_CATCH_FINALLY9.json",
            "Control/JMPLT_L.json",
            "Control/TRY_CATCH_FINALLY.json",
            "Control/TRY_CATCH_FINALLY5.json",
            "Control/THROW.json",
            "Push/PUSHA.json",
            "Push/PUSHM1_to_PUSH16.json",
            "Push/PUSHINT8_to_PUSHINT256.json",
            "Push/PUSHDATA1.json",
            "Push/PUSHDATA2.json",
            "Push/PUSHNULL.json",
            "Push/PUSHDATA4.json",
            "Arithmetic/GE.json",
            "Arithmetic/LT.json",
            "Arithmetic/MODMUL.json",
            "Arithmetic/NUMNOTEQUAL.json",
            "Arithmetic/MODPOW.json",
            "Arithmetic/LE.json",
            "Arithmetic/SHL.json",
            "Arithmetic/GT.json",
            "Arithmetic/POW.json",
            "Arithmetic/NUMEQUAL.json",
            "Arithmetic/SIGN.json",
            "Arithmetic/SQRT.json",
            "Arithmetic/SHR.json",
            "BitwiseLogic/OR.json",
            "BitwiseLogic/EQUAL.json",
            "BitwiseLogic/INVERT.json",
            "BitwiseLogic/XOR.json",
            "BitwiseLogic/NOTEQUAL.json",
            "BitwiseLogic/AND.json",
            "Types/CONVERT.json",
            "Types/ISTYPE.json",
            "Types/ISNULL.json"
            )]
        public string OpCodePath { get; set; } = string.Empty;

        [GlobalSetup]
        public void Setup()
        {
            _benchmarkEngines = LoadJson(OpCodePath, OpCode.CLEARITEMS);
        }

        [Benchmark]
        public void TestOpCode() => RunBench();

        private List<BenchmarkEngine?>? LoadJson(string path, OpCode opCode)
        {
            var realFile = Path.GetFullPath(Root + path);
            var json = File.ReadAllText(realFile, Encoding.UTF8);
            var ut = json.DeserializeJson<VMUT>();
            Assert.IsFalse(string.IsNullOrEmpty(ut.Name), "Name is required");
            if (json != ut.ToJson().Replace("\r\n", "\n"))
            {
            }
            foreach (var test in ut.Tests)
            {
                Assert.IsFalse(string.IsNullOrEmpty(test.Name), "Name is required");
                var engine = new TestEngine();
                if (test.Script.Length > 0)
                {
                    engine.LoadScript(test.Script);
                    var bench = new BenchmarkEngine(engine);
                    _benchmarkEngines?.Add(bench.ExecuteUntil(opCode));
                }
            }
            return _benchmarkEngines;
        }

        private void RunBench()
        {
            foreach (var engine in _benchmarkEngines!)
            {
                engine?.ExecuteBenchmark();
            }
        }
    }
}
