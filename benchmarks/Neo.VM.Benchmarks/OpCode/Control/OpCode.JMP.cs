// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.JMP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_JMP : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.JMP;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.Jump(VM.OpCode.JMP, loopBegin);

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript( )
        {
            throw new NotImplementedException();
        }
    }
}


//     | Method          | ItemCount | Mean     | Error    | StdDev   |
//     |---------------- |---------- |---------:|---------:|---------:|
//     | Bench_OneOpCode | 2         | 33.51 ms | 0.538 ms | 0.477 ms |
//     | Bench_OneOpCode | 32        | 33.91 ms | 0.667 ms | 0.977 ms |
//     | Bench_OneOpCode | 128       | 34.11 ms | 0.663 ms | 1.070 ms |
//     | Bench_OneOpCode | 1024      | 33.87 ms | 0.416 ms | 0.389 ms |
//     | Bench_OneOpCode | 2040      | 33.36 ms | 0.300 ms | 0.281 ms |
