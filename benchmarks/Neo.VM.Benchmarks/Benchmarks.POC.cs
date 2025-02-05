// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.POC.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using System.Diagnostics;

namespace Neo.VM.Benchmark
{
    public class Benchmarks_PoCs
    {
        [Benchmark]
        public void NeoIssue2528()
        {
            // https://github.com/neo-project/neo/issues/2528
            // L01: INITSLOT 1, 0
            // L02: NEWARRAY0
            // L03: DUP
            // L04: DUP
            // L05: PUSHINT16 2043
            // L06: STLOC 0
            // L07: PUSH1
            // L08: PACK
            // L09: LDLOC 0
            // L10: DEC
            // L11: STLOC 0
            // L12: LDLOC 0
            // L13: JMPIF_L L07
            // L14: PUSH1
            // L15: PACK
            // L16: APPEND
            // L17: PUSHINT32 38000
            // L18: STLOC 0
            // L19: PUSH0
            // L20: PICKITEM
            // L21: LDLOC 0
            // L22: DEC
            // L23: STLOC 0
            // L24: LDLOC 0
            // L25: JMPIF_L L19
            // L26: DROP
            Run("VwEAwkpKAfsHdwARwG8AnXcAbwAl9////xHAzwJwlAAAdwAQzm8AnXcAbwAl9////0U=");
        }

        [Benchmark]
        public void NeoVMIssue418()
        {
            // https://github.com/neo-project/neo-vm/issues/418
            // L00: NEWARRAY0
            // L01: PUSH0
            // L02: PICK
            // L03: PUSH1
            // L04: PACK
            // L05: PUSH1
            // L06: PICK
            // L07: PUSH1
            // L08: PACK
            // L09: INITSSLOT 1
            // L10: PUSHINT16 510
            // L11: DEC
            // L12: STSFLD0
            // L13: PUSH1
            // L14: PICK
            // L15: PUSH1
            // L16: PICK
            // L17: PUSH2
            // L18: PACK
            // L19: REVERSE3
            // L20: PUSH2
            // L21: PACK
            // L22: LDSFLD0
            // L23: DUP
            // L24: JMPIF L11
            // L25: DROP
            // L26: ROT
            // L27: DROP
            Run("whBNEcARTRHAVgEB/gGdYBFNEU0SwFMSwFhKJPNFUUU=");
        }

        [Benchmark]
        public void NeoIssue2723()
        {
            // L00: INITSSLOT 1
            // L01: PUSHINT32 130000
            // L02: STSFLD 0
            // L03: PUSHINT32 1048576
            // L04: NEWBUFFER
            // L05: DROP
            // L06: LDSFLD 0
            // L07: DEC
            // L08: DUP
            // L09: STSFLD 0
            // L10: JMPIF L03
            Run("VgEC0PsBAGcAAgAAEACIRV8AnUpnACTz");
        }

        // Below are PoCs from issue https://github.com/neo-project/neo/issues/2723 by @dusmart
        [Benchmark]
        public void PoC_NewBuffer()
        {
            // INITSLOT 0100
            // PUSHINT32 23000000
            // STLOC 00
            // PUSHINT32 1048576
            // NEWBUFFER
            // DROP
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f2ffffff
            // CLEAR
            // RET
            Run("VwEAAsDzXgF3AAIAABAAiEVvAJ13AG8AJfL///9JQA==");
        }

        [Benchmark]
        public void PoC_Cat()
        {
            // INITSLOT 0100
            // PUSHINT32 1048575
            // NEWBUFFER
            // PUSH1
            // NEWBUFFER
            // PUSHINT32 133333337
            // STLOC 00
            // OVER
            // OVER
            // CAT
            // DROP
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f5ffffff
            // CLEAR
            // RET
            Run("VwEAAv//DwCIEYgCWYHyB3cAS0uLRW8AnXcAbwAl9f///0lA");
        }

        [Benchmark]
        public void PoC_Left()
        {
            // INITSLOT 0100
            // PUSHINT32 1048576
            // NEWBUFFER
            // PUSHINT32 133333337
            // STLOC 00
            // DUP
            // PUSHINT32 1048576
            // LEFT
            // DROP
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f1ffffff
            // CLEAR
            // RET
            Run("VwEAAgAAEACIAlmB8gd3AEoCAAAQAI1FbwCddwBvACXx////SUA=");
        }

        [Benchmark]
        public void PoC_Right()
        {
            // INITSLOT 0100
            // PUSHINT32 1048576
            // NEWBUFFER
            // PUSHINT32 133333337
            // STLOC 00
            // DUP
            // PUSHINT32 1048576
            // RIGHT
            // DROP
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f1ffffff
            // CLEAR
            // RET
            Run("VwEAAgAAEACIAlmB8gd3AEoCAAAQAI5FbwCddwBvACXx////SUA=");
        }

        [Benchmark]
        public void PoC_ReverseN()
        {
            // INITSLOT 0100
            // PUSHINT16 2040
            // STLOC 00
            // PUSHDATA1 aaabbbbbbbbbcccccccdddddddeeeeeeefffffff
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L cfffffff
            // PUSHINT32 23000000
            // STLOC 00
            // PUSHINT16 2040
            // REVERSEN
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f5ffffff
            // CLEAR
            // RET
            Run("VwEAAfgHdwAMKGFhYWJiYmJiYmJiYmNjY2NjY2NkZGRkZGRkZWVlZWVlZWZmZmZmZmZvAJ13AG8AJc////8CwPNeAXcAAfgHVW8AnXcAbwAl9f///0lA");
        }

        [Benchmark]
        public void PoC_Substr()
        {
            // INITSLOT 0100
            // PUSHINT32 1048576
            // NEWBUFFER
            // PUSHINT32 133333337
            // STLOC 00
            // DUP
            // PUSH0
            // PUSHINT32 1048576
            // SUBSTR
            // DROP
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f0ffffff
            // CLEAR
            // RET
            Run("VwEAAgAAEACIAlmB8gd3AEoQAgAAEACMRW8AnXcAbwAl8P///0lA");
        }

        [Benchmark]
        public void PoC_NewArray()
        {
            // INITSLOT 0100
            // PUSHINT32 1333333337
            // STLOC 00
            // PUSHINT16 2040
            // NEWARRAY
            // DROP
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f4ffffff
            // RET
            Run("VwEAAlkNeU93AAH4B8NFbwCddwBvACX0////QA==");
        }

        [Benchmark]
        public void PoC_NewStruct()
        {
            // INITSLOT 0100
            // PUSHINT32 1333333337
            // STLOC 00
            // PUSHINT16 2040
            // NEWSTRUCT
            // DROP
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f4ffffff
            // RET
            Run("VwEAAlkNeU93AAH4B8ZFbwCddwBvACX0////QA==");
        }

        [Benchmark]
        public void PoC_Roll()
        {
            // INITSLOT 0100
            // PUSHINT16 2040
            // STLOC 00
            // PUSHDATA1 aaabbbbbbbbbcccccccdddddddeeeeeeefffffff
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L cfffffff
            // PUSHINT32 23000000
            // STLOC 00
            // PUSHINT16 2039
            // ROLL
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f5ffffff
            // CLEAR
            // RET
            Run("VwEAAfgHdwAMKGFhYWJiYmJiYmJiYmNjY2NjY2NkZGRkZGRkZWVlZWVlZWZmZmZmZmZvAJ13AG8AJc////8CwPNeAXcAAfcHUm8AnXcAbwAl9f///0lA");
        }

        [Benchmark]
        public void PoC_XDrop()
        {
            // INITSLOT 0100
            // PUSHINT16 2040
            // STLOC 00
            // PUSHDATA1 aaabbbbbbbbbcccccccdddddddeeeeeeefffffff
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L cfffffff
            // PUSHINT32 23000000
            // STLOC 00
            // PUSHINT16 2039
            // XDROP
            // DUP
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f4ffffff
            // CLEAR
            // RET
            Run("VwEAAfgHdwAMKGFhYWJiYmJiYmJiYmNjY2NjY2NkZGRkZGRkZWVlZWVlZWZmZmZmZmZvAJ13AG8AJc////8CwPNeAXcAAfcHSEpvAJ13AG8AJfT///9JQA==");
        }

        [Benchmark]
        public void PoC_MemCpy()
        {
            // INITSLOT 0100
            // PUSHINT32 1048576
            // NEWBUFFER
            // PUSHINT32 1048576
            // NEWBUFFER
            // PUSHINT32 133333337
            // STLOC 00
            // OVER
            // PUSH0
            // PUSH2
            // PICK
            // PUSH0
            // PUSHINT32 1048576
            // MEMCPY
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L eeffffff
            // CLEAR
            // RET
            Run("VwEAAgAAEACIAgAAEACIAlmB8gd3AEsQEk0QAgAAEACJbwCddwBvACXu////SUA=");
        }

        [Benchmark]
        public void PoC_Unpack()
        {
            // INITSLOT 0200
            // PUSHINT16 1010
            // NEWARRAY
            // STLOC 01
            // PUSHINT32 1333333337
            // STLOC 00
            // LDLOC 01
            // UNPACK
            // CLEAR
            // LDLOC 00
            // DEC
            // STLOC 00
            // LDLOC 00
            // JMPIF_L f5ffffff
            // RET
            Run("VwIAAfIDw3cBAlkNeU93AG8BwUlvAJ13AG8AJfX///9A");
        }

        [Benchmark]
        public void PoC_GetScriptContainer()
        {
            // SYSCALL System.Runtime.GetScriptContainer
            // DROP
            // JMP fa
            Run("QS1RCDBFIvo=");
        }

        private static void Run(string poc)
        {
            byte[] script = Convert.FromBase64String(poc);
            using ExecutionEngine engine = new();
            engine.LoadScript(script);
            engine.Execute();

            Debug.Assert(engine.State == VMState.HALT);
        }
    }
}
