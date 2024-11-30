// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.XDROP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_XDROP : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.XDROP;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

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
            builder.Jump(VM.OpCode.JMP, loopBegin);

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript( )
        {
            throw new NotImplementedException();
        }
    }
}

// | Method          | ItemCount | Mean     | Error    | StdDev   | Median   |
//     |---------------- |---------- |---------:|---------:|---------:|---------:|
//     | Bench_OneOpCode | 2         | 16.58 us | 1.035 us | 3.004 us | 15.50 us |
//     | Bench_OneOpCode | 32        | 19.00 us | 0.758 us | 2.235 us | 18.55 us |
//     | Bench_OneOpCode | 128       | 22.46 us | 1.014 us | 2.911 us | 21.90 us |
//     | Bench_OneOpCode | 1024      | 40.45 us | 2.095 us | 6.144 us | 39.00 us |
//     | Bench_OneOpCode | 2040      | 39.45 us | 2.179 us | 6.356 us | 38.55 us |

//     | Method          | ItemCount | Mean     | Error    | StdDev   | Median   |
//     |---------------- |---------- |---------:|---------:|---------:|---------:|
//     | Bench_OneOpCode | 2         | 13.18 us | 0.419 us | 1.141 us | 12.90 us |
//     | Bench_OneOpCode | 32        | 13.19 us | 0.522 us | 1.394 us | 12.80 us |
//     | Bench_OneOpCode | 128       | 13.21 us | 0.427 us | 1.147 us | 13.00 us |
//     | Bench_OneOpCode | 1024      | 16.26 us | 0.944 us | 2.768 us | 15.10 us |
//     | Bench_OneOpCode | 2040      | 16.95 us | 0.822 us | 2.424 us | 16.00 us |
