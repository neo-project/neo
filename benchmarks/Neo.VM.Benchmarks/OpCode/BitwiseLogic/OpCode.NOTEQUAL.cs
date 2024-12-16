// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NOTEQUAL.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NOTEQUAL : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.NOTEQUAL;


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
/// | Bench_OneOpCode | 4         |       2.216 us |     0.0901 us |     0.2601 us |       2.100 us |
/// | Bench_OneGAS    | 4         | 154,412.187 us | 2,734.2890 us | 2,557.6556 us | 154,055.800 us |
/// | Bench_OneOpCode | 8         |       2.274 us |     0.1095 us |     0.3227 us |       2.200 us |
/// | Bench_OneGAS    | 8         | 154,136.400 us | 1,435.3578 us | 1,342.6346 us | 153,929.900 us |
/// | Bench_OneOpCode | 16        |       2.135 us |     0.0913 us |     0.2664 us |       2.100 us |
/// | Bench_OneGAS    | 16        | 155,247.800 us | 1,584.7784 us | 1,482.4027 us | 154,921.000 us |
/// | Bench_OneOpCode | 32        |       2.224 us |     0.0946 us |     0.2759 us |       2.250 us |
/// | Bench_OneGAS    | 32        | 155,222.600 us | 2,294.2557 us | 2,454.8279 us | 154,085.850 us |
/// | Bench_OneOpCode | 64        |       2.142 us |     0.1034 us |     0.3015 us |       2.100 us |
/// | Bench_OneGAS    | 64        | 154,014.447 us | 1,593.2767 us | 1,490.3520 us | 154,235.800 us |
/// | Bench_OneOpCode | 128       |       1.889 us |     0.0487 us |     0.1333 us |       1.900 us |
/// | Bench_OneGAS    | 128       | 155,828.962 us | 3,027.3486 us | 2,973.2611 us | 154,837.700 us |
/// | Bench_OneOpCode | 256       |       1.858 us |     0.0367 us |     0.0947 us |       1.800 us |
/// | Bench_OneGAS    | 256       | 154,102.940 us | 1,713.3071 us | 1,602.6285 us | 154,175.300 us |
/// | Bench_OneOpCode | 512       |       1.849 us |     0.0485 us |     0.1351 us |       1.800 us |
/// | Bench_OneGAS    | 512       | 155,169.969 us | 2,208.3047 us | 1,844.0345 us | 155,369.100 us |
/// | Bench_OneOpCode | 1024      |       2.003 us |     0.1161 us |     0.3331 us |       1.900 us |
/// | Bench_OneGAS    | 1024      | 153,966.993 us |   808.9869 us |   756.7269 us | 153,986.900 us |
/// | Bench_OneOpCode | 2040      |       1.898 us |     0.0809 us |     0.2228 us |       1.900 us |
/// | Bench_OneGAS    | 2040      | 154,138.725 us | 2,846.7121 us | 2,795.8519 us | 153,393.600 us |
