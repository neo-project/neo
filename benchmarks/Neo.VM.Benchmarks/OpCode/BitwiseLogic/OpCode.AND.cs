// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.AND.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_AND : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.AND;


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
/// | Bench_OneOpCode | 4         |       2.058 us |     0.0447 us |     0.0708 us |       2.100 us |
/// | Bench_OneGAS    | 4         | 422,860.271 us | 3,810.1737 us | 3,377.6197 us | 423,807.850 us |
/// | Bench_OneOpCode | 8         |       2.036 us |     0.0419 us |     0.0995 us |       2.000 us |
/// | Bench_OneGAS    | 8         | 417,814.787 us | 6,392.6885 us | 5,979.7248 us | 417,140.000 us |
/// | Bench_OneOpCode | 16        |       2.061 us |     0.0449 us |     0.1102 us |       2.000 us |
/// | Bench_OneGAS    | 16        | 413,108.227 us | 2,641.0572 us | 2,470.4466 us | 412,631.500 us |
/// | Bench_OneOpCode | 32        |       2.270 us |     0.0587 us |     0.1608 us |       2.200 us |
/// | Bench_OneGAS    | 32        | 415,815.131 us | 3,968.5913 us | 3,313.9535 us | 416,050.300 us |
/// | Bench_OneOpCode | 64        |       2.107 us |     0.0501 us |     0.1388 us |       2.100 us |
/// | Bench_OneGAS    | 64        | 414,919.493 us | 3,206.5042 us | 2,999.3660 us | 414,408.900 us |
/// | Bench_OneOpCode | 128       |       2.100 us |     0.0572 us |     0.1576 us |       2.100 us |
/// | Bench_OneGAS    | 128       | 419,293.240 us | 3,515.8178 us | 3,288.6981 us | 418,878.100 us |
/// | Bench_OneOpCode | 256       |       2.482 us |     0.1914 us |     0.5461 us |       2.200 us |
/// | Bench_OneGAS    | 256       | 414,890.173 us | 3,955.3869 us | 3,699.8714 us | 413,567.500 us |
/// | Bench_OneOpCode | 512       |       2.146 us |     0.0468 us |     0.1138 us |       2.100 us |
/// | Bench_OneGAS    | 512       | 414,750.280 us | 4,819.3798 us | 4,508.0509 us | 415,181.300 us |
/// | Bench_OneOpCode | 1024      |       2.052 us |     0.0439 us |     0.0586 us |       2.000 us |
/// | Bench_OneGAS    | 1024      | 422,244.236 us | 3,807.1363 us | 3,374.9272 us | 422,194.150 us |
/// | Bench_OneOpCode | 2040      |       2.165 us |     0.0441 us |     0.0859 us |       2.150 us |
/// | Bench_OneGAS    | 2040      | 426,189.143 us | 5,197.4558 us | 4,607.4092 us | 425,128.600 us |
