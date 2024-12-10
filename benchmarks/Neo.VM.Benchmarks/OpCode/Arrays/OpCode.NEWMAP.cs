// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWMAP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NEWMAP : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.NEWMAP;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            // builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWMAP);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            // builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWMAP);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }

    // | Method          | ItemCount | Mean           | Error         | StdDev        | Median         |
    // |---------------- |---------- |---------------:|--------------:|--------------:|---------------:|
    // | Bench_OneOpCode | 2         |       1.700 us |     0.0376 us |     0.0809 us |       1.700 us |
    // | Bench_OneGAS    | 2         | 540,295.956 us | 9,106.0668 us | 8,943.3752 us | 538,427.100 us |
    // | Bench_OneOpCode | 32        |       1.731 us |     0.0385 us |     0.0972 us |       1.700 us |
    // | Bench_OneGAS    | 32        | 529,350.753 us | 9,432.3071 us | 8,822.9859 us | 531,064.500 us |
    // | Bench_OneOpCode | 128       |       1.685 us |     0.0375 us |     0.0968 us |       1.700 us |
    // | Bench_OneGAS    | 128       | 522,079.015 us | 5,165.5249 us | 4,313.4473 us | 521,718.500 us |
    // | Bench_OneOpCode | 1024      |       1.725 us |     0.0383 us |     0.0879 us |       1.700 us |
    // | Bench_OneGAS    | 1024      | 529,468.479 us | 8,473.8522 us | 7,511.8493 us | 529,274.150 us |
    // | Bench_OneOpCode | 2040      |       2.118 us |     0.1963 us |     0.5756 us |       1.900 us |
    // | Bench_OneGAS    | 2040      | 525,997.760 us | 6,679.5520 us | 6,248.0571 us | 527,017.900 us |

}
