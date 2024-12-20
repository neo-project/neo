// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.OR.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_OR : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.OR;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(int.MaxValue);
            builder.Push(int.MaxValue);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.Push(int.MaxValue);
            builder.Push(int.MaxValue);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error         | StdDev        | Median         |
/// |---------------- |---------- |---------------:|--------------:|--------------:|---------------:|
/// | Bench_OneOpCode | 4         |       2.050 us |     0.0913 us |     0.2514 us |       2.100 us |
/// | Bench_OneGAS    | 4         | 420,278.285 us | 3,921.5817 us | 3,274.6983 us | 419,672.100 us |
/// | Bench_OneOpCode | 8         |       2.136 us |     0.0528 us |     0.1471 us |       2.100 us |
/// | Bench_OneGAS    | 8         | 421,800.950 us | 4,426.1796 us | 3,923.6929 us | 421,130.600 us |
/// | Bench_OneOpCode | 16        |       2.066 us |     0.0574 us |     0.1582 us |       2.000 us |
/// | Bench_OneGAS    | 16        | 420,929.267 us | 4,495.9200 us | 4,205.4863 us | 420,361.100 us |
/// | Bench_OneOpCode | 32        |       2.164 us |     0.0842 us |     0.2389 us |       2.150 us |
/// | Bench_OneGAS    | 32        | 427,815.107 us | 3,324.1472 us | 3,109.4094 us | 428,440.500 us |
/// | Bench_OneOpCode | 64        |       2.175 us |     0.0473 us |     0.1143 us |       2.200 us |
/// | Bench_OneGAS    | 64        | 419,902.293 us | 6,780.6137 us | 6,342.5902 us | 419,130.000 us |
/// | Bench_OneOpCode | 128       |       2.198 us |     0.0716 us |     0.1899 us |       2.200 us |
/// | Bench_OneGAS    | 128       | 420,673.513 us | 4,262.1126 us | 3,986.7828 us | 422,638.800 us |
/// | Bench_OneOpCode | 256       |       2.000 us |     0.0000 us |     0.0000 us |       2.000 us |
/// | Bench_OneGAS    | 256       | 418,585.087 us | 4,704.3546 us | 4,400.4562 us | 419,285.700 us |
/// | Bench_OneOpCode | 512       |       2.203 us |     0.0531 us |     0.1507 us |       2.200 us |
/// | Bench_OneGAS    | 512       | 418,828.300 us | 4,403.9299 us | 3,903.9692 us | 419,073.150 us |
/// | Bench_OneOpCode | 1024      |       2.129 us |     0.0756 us |     0.2057 us |       2.100 us |
/// | Bench_OneGAS    | 1024      | 416,814.436 us | 3,277.8806 us | 2,905.7558 us | 416,336.750 us |
/// | Bench_OneOpCode | 2040      |       2.573 us |     0.1889 us |     0.5480 us |       2.300 us |
/// | Bench_OneGAS    | 2040      | 421,709.120 us | 5,007.3189 us | 4,683.8492 us | 420,733.000 us |
