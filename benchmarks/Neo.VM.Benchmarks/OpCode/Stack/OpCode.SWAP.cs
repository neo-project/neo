// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SWAP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_SWAP : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.SWAP;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);

            // var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(Opcode);
            // builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            throw new NotImplementedException();
        }
    }
}


// | Method          | ItemCount | Mean     | Error     | StdDev    | Median   |
//     |---------------- |---------- |---------:|----------:|----------:|---------:|
//     | Bench_OneOpCode | 2         | 2.094 us | 0.0524 us | 0.1417 us | 2.100 us |
//     | Bench_OneOpCode | 32        | 2.897 us | 0.2905 us | 0.8565 us | 2.400 us |
//     | Bench_OneOpCode | 128       | 2.085 us | 0.0453 us | 0.1178 us | 2.100 us |
//     | Bench_OneOpCode | 1024      | 2.065 us | 0.0450 us | 0.1060 us | 2.000 us |
//     | Bench_OneOpCode | 2040      | 2.096 us | 0.0457 us | 0.1195 us | 2.100 us |


// One gas << 1

// | Method          | ItemCount | Mean     | Error    | StdDev   |
//     |---------------- |---------- |---------:|---------:|---------:|
//     | Bench_OneOpCode | 1         | 44.14 ms | 0.870 ms | 1.132 ms |
//     | Bench_OneOpCode | 2         | 44.19 ms | 0.782 ms | 1.045 ms |
//     | Bench_OneOpCode | 32        | 44.29 ms | 0.859 ms | 1.087 ms |
//     | Bench_OneOpCode | 128       | 43.59 ms | 0.755 ms | 0.706 ms |
//     | Bench_OneOpCode | 1024      | 43.68 ms | 0.597 ms | 0.559 ms |
//     | Bench_OneOpCode | 2040      | 43.54 ms | 0.590 ms | 0.552 ms |
