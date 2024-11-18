// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.REVERSEITEMS.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_REVERSEITEMS : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.REVERSEITEMS;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            // builder.Push(0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            builder.Push(ItemCount);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            throw new NotImplementedException();
        }
    }

    // for 0

    // | Method          | ItemCount | Mean     | Error    | StdDev   | Median   |
    //     |---------------- |---------- |---------:|---------:|---------:|---------:|
    //     | Bench_OneOpCode | 1         | 31.14 us | 1.564 us | 4.536 us | 29.30 us |
    //     | Bench_OneOpCode | 32        | 30.65 us | 1.355 us | 3.867 us | 29.30 us |
    //     | Bench_OneOpCode | 128       | 31.74 us | 1.580 us | 4.634 us | 29.80 us |
    //     | Bench_OneOpCode | 1024      | 34.25 us | 1.355 us | 3.954 us | 32.80 us |
    //     | Bench_OneOpCode | 2040      | 36.05 us | 1.337 us | 3.899 us | 35.45 us |

    // ushort.max*2
    // | Method          | ItemCount | Mean     | Error    | StdDev   | Median   |
    //     |---------------- |---------- |---------:|---------:|---------:|---------:|
    //     | Bench_OneOpCode | 1         | 30.51 us | 1.113 us | 3.104 us | 29.80 us |
    //     | Bench_OneOpCode | 32        | 36.93 us | 1.111 us | 3.224 us | 35.90 us |
    //     | Bench_OneOpCode | 128       | 42.92 us | 1.283 us | 3.659 us | 42.55 us |
    //     | Bench_OneOpCode | 1024      | 64.23 us | 2.044 us | 5.799 us | 63.40 us |
    //     | Bench_OneOpCode | 2040      | 65.20 us | 2.549 us | 7.516 us | 64.70 us |
}
