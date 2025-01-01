// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.EQUAL.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_EQUAL : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.EQUAL;


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
/// | Bench_OneOpCode | 4         |       2.043 us |     0.0713 us |     0.1951 us |       2.000 us |
/// | Bench_OneGAS    | 4         | 157,923.073 us | 2,561.5390 us | 2,396.0652 us | 156,784.000 us |
/// | Bench_OneOpCode | 8         |       2.183 us |     0.1038 us |     0.3044 us |       2.100 us |
/// | Bench_OneGAS    | 8         | 152,716.857 us | 2,357.2834 us | 2,089.6703 us | 152,235.950 us |
/// | Bench_OneOpCode | 16        |       2.120 us |     0.1043 us |     0.3026 us |       2.000 us |
/// | Bench_OneGAS    | 16        | 155,691.847 us | 2,423.7758 us | 2,267.2014 us | 155,856.900 us |
/// | Bench_OneOpCode | 32        |       2.315 us |     0.1378 us |     0.4064 us |       2.200 us |
/// | Bench_OneGAS    | 32        | 152,824.327 us | 2,262.0217 us | 2,115.8965 us | 152,154.900 us |
/// | Bench_OneOpCode | 64        |       2.183 us |     0.0973 us |     0.2761 us |       2.100 us |
/// | Bench_OneGAS    | 64        | 152,816.293 us | 2,102.2278 us | 1,966.4252 us | 152,142.000 us |
/// | Bench_OneOpCode | 128       |       1.794 us |     0.0478 us |     0.1316 us |       1.800 us |
/// | Bench_OneGAS    | 128       | 152,486.562 us | 2,536.0156 us | 2,117.6879 us | 152,057.800 us |
/// | Bench_OneOpCode | 256       |       1.807 us |     0.0502 us |     0.1331 us |       1.800 us |
/// | Bench_OneGAS    | 256       | 155,699.120 us | 2,758.1901 us | 2,580.0128 us | 155,298.500 us |
/// | Bench_OneOpCode | 512       |       1.897 us |     0.0516 us |     0.1403 us |       1.850 us |
/// | Bench_OneGAS    | 512       | 155,937.100 us | 3,004.3563 us | 2,950.6796 us | 156,387.550 us |
/// | Bench_OneOpCode | 1024      |       1.725 us |     0.0563 us |     0.1542 us |       1.700 us |
/// | Bench_OneGAS    | 1024      | 154,823.867 us | 2,699.2542 us | 2,524.8841 us | 154,503.600 us |
/// | Bench_OneOpCode | 2040      |       1.804 us |     0.0398 us |     0.0985 us |       1.800 us |
/// | Bench_OneGAS    | 2040      | 154,626.260 us | 2,224.6052 us | 2,080.8971 us | 154,298.400 us |
