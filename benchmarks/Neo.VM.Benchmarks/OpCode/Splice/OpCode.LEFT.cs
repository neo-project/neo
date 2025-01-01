// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.LEFT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using Newtonsoft.Json.Linq;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_LEFT : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.LEFT;

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

/// | Method          | ItemCount | Mean         | Error         | StdDev        | Median       |
/// |---------------- |---------- |-------------:|--------------:|--------------:|-------------:|
/// | Bench_OneOpCode | 4         |     3.087 us |     0.1388 us |     0.4006 us |     3.050 us |
/// | Bench_OneGAS    | 4         | 7,766.011 us | 2,079.9514 us | 6,132.7840 us | 3,097.750 us |
/// | Bench_OneOpCode | 8         |     2.653 us |     0.0784 us |     0.2160 us |     2.600 us |
/// | Bench_OneGAS    | 8         | 6,683.943 us | 1,593.2528 us | 4,672.7350 us | 6,109.400 us |
/// | Bench_OneOpCode | 16        |     2.819 us |     0.1408 us |     0.3995 us |     2.700 us |
/// | Bench_OneGAS    | 16        | 6,860.838 us | 1,901.0701 us | 5,575.5100 us | 2,946.500 us |
/// | Bench_OneOpCode | 32        |     2.898 us |     0.1206 us |     0.3479 us |     2.750 us |
/// | Bench_OneGAS    | 32        | 6,020.279 us | 1,359.3513 us | 3,900.2339 us | 5,853.100 us |
/// | Bench_OneOpCode | 64        |     2.774 us |     0.1164 us |     0.3321 us |     2.650 us |
/// | Bench_OneGAS    | 64        | 6,689.094 us | 1,831.2251 us | 5,312.7164 us | 3,042.500 us |
/// | Bench_OneOpCode | 128       |     2.755 us |     0.1277 us |     0.3602 us |     2.600 us |
/// | Bench_OneGAS    | 128       | 6,686.291 us | 1,847.7189 us | 5,360.5680 us | 3,440.800 us |
/// | Bench_OneOpCode | 256       |     2.896 us |     0.1728 us |     0.4957 us |     2.700 us |
/// | Bench_OneGAS    | 256       | 7,457.618 us | 1,887.6292 us | 5,476.3549 us | 3,472.700 us |
/// | Bench_OneOpCode | 512       |     2.913 us |     0.1887 us |     0.5505 us |     2.700 us |
/// | Bench_OneGAS    | 512       | 7,019.254 us | 1,985.5425 us | 5,823.2528 us | 3,587.800 us |
/// | Bench_OneOpCode | 1024      |     3.233 us |     0.1838 us |     0.5303 us |     3.100 us |
/// | Bench_OneGAS    | 1024      | 7,561.639 us | 1,878.0069 us | 5,448.4390 us | 4,233.400 us |
/// | Bench_OneOpCode | 2040      |     3.010 us |     0.1309 us |     0.3713 us |     2.900 us |
/// | Bench_OneGAS    | 2040      | 4,485.884 us |   123.0046 us |   328.3241 us | 4,448.600 us |
