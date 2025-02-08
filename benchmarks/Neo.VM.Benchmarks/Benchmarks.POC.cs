// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.POC.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Benchmark.OpCode;
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
            using BenchmarkEngine engine = new();
            engine.LoadScript(script);
            engine.ExecuteOneGASBenchmark();

            Debug.Assert(engine.State == VMState.HALT);
        }
    }
}

//
// | Method                 | Mean            | Error          | StdDev         |
//     |----------------------- |----------------:|---------------:|---------------:|
//     | NeoIssue2528           |    55,319.65 us |   1,105.171 us |   1,687.713 us |
//     | NeoVMIssue418          |       347.21 us |       4.985 us |       4.663 us |
//     | NeoIssue2723           |        71.05 us |       0.674 us |       0.630 us |
//     | PoC_NewBuffer          |        68.60 us |       0.691 us |       0.646 us |
//     | PoC_Cat                |        68.96 us |       0.831 us |       0.777 us |
//     | PoC_Left               |        65.76 us |       0.773 us |       0.723 us |
//     | PoC_Right              |        64.84 us |       0.679 us |       0.635 us |
//     | PoC_ReverseN           | 8,399,264.69 us | 152,328.560 us | 135,035.301 us |
//     | PoC_Substr             |        65.07 us |       1.122 us |       1.050 us |
//     | PoC_NewArray           | 2,903,663.37 us |  15,827.726 us |  14,805.265 us |
//     | PoC_NewStruct          | 3,027,978.35 us |  12,747.892 us |  11,300.674 us |
//     | PoC_Roll               | 1,274,207.78 us |   8,063.045 us |   7,542.177 us |
//     | PoC_XDrop              | 1,217,206.92 us |  22,759.560 us |  21,289.307 us |
//     | PoC_MemCpy             |        70.88 us |       1.407 us |       1.728 us |
//     | PoC_Unpack             |   384,346.69 us |   3,212.166 us |   2,847.502 us |
//     | PoC_GetScriptContainer |        69.26 us |       0.620 us |       0.580 us |


// | Method                 | Mean            | Error          | StdDev         |
//     |----------------------- |----------------:|---------------:|---------------:|
//     | NeoIssue2528           |   698,949.53 us |  13,657.042 us |  22,817.849 us |
//     | NeoVMIssue418          |       589.21 us |      11.631 us |      17.409 us |
//     | NeoIssue2723           |        70.68 us |       1.375 us |       1.739 us |
//     | PoC_NewBuffer          |        71.54 us |       1.350 us |       1.607 us |
//     | PoC_Cat                |        70.13 us |       1.387 us |       2.077 us |
//     | PoC_Left               |        69.71 us |       1.230 us |       1.151 us |
//     | PoC_Right              |        70.44 us |       1.358 us |       1.564 us |
//     | PoC_ReverseN           | 8,387,677.64 us | 157,776.848 us | 154,957.961 us |
//     | PoC_Substr             |        66.31 us |       0.834 us |       0.780 us |
//     | PoC_NewArray           | 4,081,067.00 us |  21,804.230 us |  20,395.690 us |
//     | PoC_NewStruct          | 4,073,281.73 us |  14,618.967 us |  13,674.591 us |
//     | PoC_Roll               | 1,226,264.36 us |   9,115.778 us |   8,952.913 us |
//     | PoC_XDrop              | 1,220,594.82 us |  12,695.011 us |   9,911.430 us |
//     | PoC_MemCpy             |        65.53 us |       0.737 us |       0.616 us |
//     | PoC_Unpack             |   392,349.50 us |   7,267.582 us |   7,463.272 us |
//     | PoC_GetScriptContainer |        66.83 us |       0.622 us |       0.551 us |
