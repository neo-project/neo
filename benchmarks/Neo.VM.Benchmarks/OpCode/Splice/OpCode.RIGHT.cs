// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.RIGHT.cs file belongs to the neo project and is free
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
    public class OpCode_RIGHT : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.RIGHT;

        public const int MAX_LEN = ushort.MaxValue;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(MAX_LEN);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.CONVERT,
                _operand = [(byte)StackItemType.ByteString]
            });
            builder.Push(ItemCount);
            builder.AddInstruction(Opcode);

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(MAX_LEN);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.CONVERT,
                _operand = [(byte)StackItemType.ByteString]
            });

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.AddInstruction(VM.OpCode.DUP);
            builder.Push(ItemCount);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean          | Error         | StdDev        | Median        |
/// |---------------- |---------- |--------------:|--------------:|--------------:|--------------:|
/// | Bench_OneOpCode | 4         |      2.860 us |     0.1128 us |     0.3237 us |      2.800 us |
/// | Bench_OneGAS    | 4         |  7,371.909 us | 2,192.7840 us | 6,465.4737 us |  2,941.400 us |
/// | Bench_OneOpCode | 8         |      2.701 us |     0.0632 us |     0.1718 us |      2.700 us |
/// | Bench_OneGAS    | 8         |  8,276.648 us | 2,089.1983 us | 6,160.0488 us |  7,424.050 us |
/// | Bench_OneOpCode | 16        |      2.941 us |     0.0736 us |     0.2076 us |      2.900 us |
/// | Bench_OneGAS    | 16        |  6,213.548 us | 1,251.4915 us | 3,650.6597 us |  5,860.350 us |
/// | Bench_OneOpCode | 32        |      2.518 us |     0.0534 us |     0.1090 us |      2.500 us |
/// | Bench_OneGAS    | 32        |  7,500.268 us | 2,140.7244 us | 6,311.9747 us |  2,982.950 us |
/// | Bench_OneOpCode | 64        |      2.603 us |     0.1081 us |     0.3015 us |      2.500 us |
/// | Bench_OneGAS    | 64        |  7,123.892 us | 1,879.9781 us | 5,513.6507 us |  3,343.800 us |
/// | Bench_OneOpCode | 128       |      2.872 us |     0.1279 us |     0.3566 us |      2.800 us |
/// | Bench_OneGAS    | 128       |  6,545.854 us | 1,954.8252 us | 5,733.1642 us |  3,162.900 us |
/// | Bench_OneOpCode | 256       |      2.897 us |     0.1002 us |     0.2940 us |      2.800 us |
/// | Bench_OneGAS    | 256       | 10,724.053 us |   212.8602 us |   199.1095 us | 10,702.100 us |
/// | Bench_OneOpCode | 512       |      2.787 us |     0.0544 us |     0.1036 us |      2.800 us |
/// | Bench_OneGAS    | 512       | 10,817.380 us |   215.8381 us |   201.8951 us | 10,848.100 us |
/// | Bench_OneOpCode | 1024      |      2.920 us |     0.0776 us |     0.2190 us |      2.900 us |
/// | Bench_OneGAS    | 1024      |  8,326.519 us | 2,234.9798 us | 6,589.8888 us |  3,857.700 us |
/// | Bench_OneOpCode | 2040      |      2.862 us |     0.1498 us |     0.4298 us |      2.700 us |
/// | Bench_OneGAS    | 2040      |  7,857.649 us | 1,754.6136 us | 5,062.4618 us |  4,817.050 us |
