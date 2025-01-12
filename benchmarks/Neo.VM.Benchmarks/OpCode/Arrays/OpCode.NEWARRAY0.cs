// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWARRAY0.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NEWARRAY0 : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.NEWARRAY0;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error         | StdDev        | Median         |
/// |---------------- |---------- |---------------:|--------------:|--------------:|---------------:|
/// | Bench_OneOpCode | 4         |       1.776 us |     0.0447 us |     0.1232 us |       1.800 us |
/// | Bench_OneGAS    | 4         | 150,179.240 us | 2,037.6636 us | 1,906.0318 us | 149,865.900 us |
/// | Bench_OneOpCode | 8         |       1.641 us |     0.0518 us |     0.1409 us |       1.600 us |
/// | Bench_OneGAS    | 8         | 153,327.195 us | 3,012.8685 us | 3,348.7953 us | 152,689.500 us |
/// | Bench_OneOpCode | 16        |       1.778 us |     0.0652 us |     0.1808 us |       1.700 us |
/// | Bench_OneGAS    | 16        | 150,926.820 us | 2,555.3623 us | 2,390.2875 us | 151,110.600 us |
/// | Bench_OneOpCode | 32        |       1.916 us |     0.0908 us |     0.2607 us |       1.900 us |
/// | Bench_OneGAS    | 32        | 151,309.100 us | 1,986.7178 us | 1,761.1736 us | 150,816.400 us |
/// | Bench_OneOpCode | 64        |       1.935 us |     0.0940 us |     0.2756 us |       1.900 us |
/// | Bench_OneGAS    | 64        | 156,382.982 us | 3,034.8426 us | 3,116.5599 us | 157,073.800 us |
/// | Bench_OneOpCode | 128       |       1.855 us |     0.0904 us |     0.2580 us |       1.800 us |
/// | Bench_OneGAS    | 128       | 154,472.860 us | 2,012.5415 us | 1,882.5325 us | 154,236.200 us |
/// | Bench_OneOpCode | 256       |       1.854 us |     0.0864 us |     0.2479 us |       1.800 us |
/// | Bench_OneGAS    | 256       | 152,500.707 us | 2,221.9784 us | 2,078.4400 us | 152,737.700 us |
/// | Bench_OneOpCode | 512       |       1.669 us |     0.0496 us |     0.1348 us |       1.600 us |
/// | Bench_OneGAS    | 512       | 150,475.193 us | 2,590.9742 us | 2,423.5989 us | 150,842.400 us |
/// | Bench_OneOpCode | 1024      |       1.820 us |     0.0800 us |     0.2295 us |       1.700 us |
/// | Bench_OneGAS    | 1024      | 152,889.853 us | 1,427.9322 us | 1,335.6887 us | 152,285.300 us |
/// | Bench_OneOpCode | 2040      |       1.809 us |     0.0958 us |     0.2765 us |       1.700 us |
/// | Bench_OneGAS    | 2040      | 153,882.207 us | 1,766.5362 us | 1,565.9883 us | 154,388.050 us |
