// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWSTRUCT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NEWSTRUCT : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.NEWSTRUCT;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            // builder.Push(ItemCount);
            // builder.AddInstruction(VM.OpCode.NEWSTRUCT);

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWSTRUCT);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript( )
        {
            throw new NotImplementedException();
        }
    }

    // | Method          | ItemCount | Mean      | Error     | StdDev    | Median    |
    //     |---------------- |---------- |----------:|----------:|----------:|----------:|
    //     | Bench_OneOpCode | 1         |  3.924 us | 0.2051 us | 0.5579 us |  3.700 us |
    //     | Bench_OneOpCode | 32        |  4.207 us | 0.1504 us | 0.4143 us |  4.100 us |
    //     | Bench_OneOpCode | 128       |  6.317 us | 0.2726 us | 0.7555 us |  6.100 us |
    //     | Bench_OneOpCode | 1024      | 25.456 us | 0.5446 us | 1.5713 us | 24.900 us |
    //     | Bench_OneOpCode | 2040      | 31.177 us | 0.6247 us | 0.7671 us | 31.100 us |
}


// InvocationCount=1  UnrollFactor=1
//
//                                 | Method          | ItemCount | Mean       | Error     | StdDev    | Median     |
//     |---------------- |---------- |-----------:|----------:|----------:|-----------:|
//     | Bench_OneOpCode | 1         |   5.550 ms | 0.1085 ms | 0.1690 ms |   5.496 ms |
//     | Bench_OneOpCode | 2         |   6.689 ms | 0.3256 ms | 0.9599 ms |   6.988 ms |
//     | Bench_OneOpCode | 32        |   8.699 ms | 2.3569 ms | 6.9493 ms |   4.031 ms |
//     | Bench_OneOpCode | 128       |   8.752 ms | 0.1724 ms | 0.3962 ms |   8.676 ms |
//     | Bench_OneOpCode | 1024      |  55.746 ms | 1.1068 ms | 1.8185 ms |  55.939 ms |
//     | Bench_OneOpCode | 2040      | 106.887 ms | 2.1073 ms | 3.2807 ms | 105.926 ms |
