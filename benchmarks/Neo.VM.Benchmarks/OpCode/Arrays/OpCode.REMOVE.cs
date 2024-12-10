// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.REMOVE.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_REMOVE : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.REMOVE;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
            // builder.Push(ushort.MaxValue * 2);
            // builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.Push(0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.Push(0);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            throw new NotImplementedException();
        }
    }

    // itemcount -1
    // | Method          | ItemCount | Mean     | Error     | StdDev    | Median   |
    //     |---------------- |---------- |---------:|----------:|----------:|---------:|
    //     | Bench_OneOpCode | 1         | 1.670 us | 0.0448 us | 0.1148 us | 1.600 us |
    //     | Bench_OneOpCode | 32        | 2.364 us | 0.1307 us | 0.3750 us | 2.400 us |
    //     | Bench_OneOpCode | 128       | 3.142 us | 0.1929 us | 0.5534 us | 3.200 us |
    //     | Bench_OneOpCode | 1024      | 3.755 us | 0.6429 us | 1.8957 us | 4.000 us |
    //     | Bench_OneOpCode | 2040      | 3.424 us | 0.5194 us | 1.5232 us | 3.600 us |

    // index 0
    // | Method          | ItemCount | Mean     | Error     | StdDev    |
    //     |---------------- |---------- |---------:|----------:|----------:|
    //     | Bench_OneOpCode | 1         | 1.615 us | 0.0460 us | 0.1237 us |
    //     | Bench_OneOpCode | 32        | 2.829 us | 0.1725 us | 0.5086 us |
    //     | Bench_OneOpCode | 128       | 3.711 us | 0.2571 us | 0.7542 us |
    //     | Bench_OneOpCode | 1024      | 3.991 us | 0.6248 us | 1.8225 us |
    //     | Bench_OneOpCode | 2040      | 4.687 us | 0.4194 us | 1.1831 us |

    // itemcount/2
    // | Method          | ItemCount | Mean     | Error     | StdDev    | Median   |
    //     |---------------- |---------- |---------:|----------:|----------:|---------:|
    //     | Bench_OneOpCode | 1         | 1.686 us | 0.0523 us | 0.1458 us | 1.600 us |
    //     | Bench_OneOpCode | 32        | 2.635 us | 0.1458 us | 0.4298 us | 2.600 us |
    //     | Bench_OneOpCode | 128       | 3.490 us | 0.2725 us | 0.7861 us | 3.500 us |
    //     | Bench_OneOpCode | 1024      | 4.392 us | 0.6678 us | 1.9375 us | 4.700 us |
    //     | Bench_OneOpCode | 2040      | 4.240 us | 0.5454 us | 1.5649 us | 4.500 us |


    // for 0

    // | Method          | ItemCount | Mean     | Error     | StdDev    | Median   |
    //     |---------------- |---------- |---------:|----------:|----------:|---------:|
    //     | Bench_OneOpCode | 1         | 1.370 us | 0.0432 us | 0.1130 us | 1.300 us |
    //     | Bench_OneOpCode | 32        | 1.453 us | 0.0255 us | 0.0644 us | 1.400 us |
    //     | Bench_OneOpCode | 128       | 2.132 us | 0.2310 us | 0.6812 us | 1.800 us |
    //     | Bench_OneOpCode | 1024      | 1.749 us | 0.0647 us | 0.1716 us | 1.700 us |
    //     | Bench_OneOpCode | 2040      | 2.468 us | 0.1875 us | 0.5528 us | 2.200 us |

    //
    // | Method          | ItemCount | Mean     | Error     | StdDev    | Median   |
    //     |---------------- |---------- |---------:|----------:|----------:|---------:|
    //     | Bench_OneOpCode | 1         | 1.542 us | 0.0410 us | 0.1095 us | 1.500 us |
    //     | Bench_OneOpCode | 32        | 1.538 us | 0.0415 us | 0.1123 us | 1.500 us |
    //     | Bench_OneOpCode | 128       | 2.103 us | 0.2342 us | 0.6904 us | 1.800 us |
    //     | Bench_OneOpCode | 1024      | 2.469 us | 0.2183 us | 0.6401 us | 2.300 us |
    //     | Bench_OneOpCode | 2040      | 2.336 us | 0.1864 us | 0.5409 us | 2.100 us |
}
