// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.LDSFLD.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_LDSFLD : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.LDLOC0;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}


// | Method          | ItemCount | Mean     | Error    | StdDev   |
//     |---------------- |---------- |---------:|---------:|---------:|
//     | Bench_OneOpCode | 1         | 37.30 ms | 0.480 ms | 0.449 ms |
//     | Bench_OneOpCode | 2         | 37.91 ms | 0.749 ms | 0.736 ms |
//     | Bench_OneOpCode | 32        | 36.77 ms | 0.475 ms | 0.488 ms |
//     | Bench_OneOpCode | 128       | 37.77 ms | 0.683 ms | 0.864 ms |
//     | Bench_OneOpCode | 1024      | 37.33 ms | 0.657 ms | 0.582 ms |
//     | Bench_OneOpCode | 2040      | 37.64 ms | 0.666 ms | 0.623 ms |
