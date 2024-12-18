// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.CONVERT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_CONVERT : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.CONVERT;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.CONVERT,
                _operand = [(byte)StackItemType.ByteString]
            });
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.CONVERT,
                _operand = [(byte)StackItemType.ByteString]
            });
            builder.AddInstruction(VM.OpCode.DROP);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean         | Error       | StdDev        | Median       |
/// |---------------- |---------- |-------------:|------------:|--------------:|-------------:|
/// | Bench_OneOpCode | 4         |     2.329 us |   0.1031 us |     0.2943 us |     2.200 us |
/// | Bench_OneGAS    | 4         | 2,775.112 us |  55.1581 us |   102.2391 us | 2,770.400 us |
/// | Bench_OneOpCode | 8         |     2.547 us |   0.1696 us |     0.4974 us |     2.300 us |
/// | Bench_OneGAS    | 8         | 3,381.386 us | 197.5204 us |   579.2933 us | 3,095.200 us |
/// | Bench_OneOpCode | 16        |     2.215 us |   0.0452 us |     0.1198 us |     2.200 us |
/// | Bench_OneGAS    | 16        | 2,717.893 us |  53.8432 us |    97.0904 us | 2,679.900 us |
/// | Bench_OneOpCode | 32        |     2.379 us |   0.0511 us |     0.1381 us |     2.300 us |
/// | Bench_OneGAS    | 32        | 2,788.332 us |  55.4634 us |    68.1141 us | 2,776.250 us |
/// | Bench_OneOpCode | 64        |     2.459 us |   0.1678 us |     0.4787 us |     2.300 us |
/// | Bench_OneGAS    | 64        | 2,883.424 us |  56.2233 us |    89.1761 us | 2,873.100 us |
/// | Bench_OneOpCode | 128       |     2.367 us |   0.1179 us |     0.3365 us |     2.200 us |
/// | Bench_OneGAS    | 128       | 3,168.616 us |  62.9385 us |   106.8743 us | 3,141.500 us |
/// | Bench_OneOpCode | 256       |     2.642 us |   0.1741 us |     0.5051 us |     2.400 us |
/// | Bench_OneGAS    | 256       | 3,586.837 us |  69.4336 us |   119.7695 us | 3,561.950 us |
/// | Bench_OneOpCode | 512       |     2.265 us |   0.1015 us |     0.2813 us |     2.200 us |
/// | Bench_OneGAS    | 512       | 4,098.141 us |  80.4067 us |   125.1835 us | 4,085.950 us |
/// | Bench_OneOpCode | 1024      |     2.173 us |   0.0473 us |     0.1287 us |     2.200 us |
/// | Bench_OneGAS    | 1024      | 5,221.795 us | 415.9487 us | 1,226.4343 us | 4,968.350 us |
/// | Bench_OneOpCode | 2040      |     2.508 us |   0.1573 us |     0.4539 us |     2.300 us |
/// | Bench_OneGAS    | 2040      | 6,185.417 us | 335.8648 us |   990.3052 us | 6,398.050 us |
