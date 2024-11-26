// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.VALUES.cs file belongs to the neo project and is free
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
    public class OpCode_VALUES : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.VALUES;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount / 2);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);

            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.Push(sbyte.MaxValue);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.CONVERT,
                _operand = [(byte)StackItemType.ByteString]
            });
            // builder.Push(0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            builder.Push(ItemCount / 2);
            builder.AddInstruction(VM.OpCode.PACKMAP);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript( )
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.Push(ItemCount / 2);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);

            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.Push(sbyte.MaxValue);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.CONVERT,
                _operand = [(byte)StackItemType.ByteString]
            });
            // builder.Push(0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            builder.Push(ItemCount / 2);
            builder.AddInstruction(VM.OpCode.PACKMAP);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.CLEAR);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }


    // 0
    // | Method          | ItemCount | Mean     | Error     | StdDev    | Median   |
    //     |---------------- |---------- |---------:|----------:|----------:|---------:|
    //     | Bench_OneOpCode | 1         | 3.472 us | 0.0975 us | 0.2686 us | 3.400 us |
    //     | Bench_OneOpCode | 32        | 4.728 us | 0.4434 us | 1.2935 us | 4.100 us |
    //     | Bench_OneOpCode | 128       | 3.622 us | 0.0749 us | 0.1050 us | 3.600 us |
    //     | Bench_OneOpCode | 1024      | 4.827 us | 0.4048 us | 1.1873 us | 4.300 us |
    //     | Bench_OneOpCode | 2040      | 4.518 us | 0.2661 us | 0.7331 us | 4.250 us |

    // ushort.max*2
    // | Method          | ItemCount | Mean      | Error     | StdDev    | Median    |
    //     |---------------- |---------- |----------:|----------:|----------:|----------:|
    //     | Bench_OneOpCode | 1         |  3.348 us | 0.1720 us | 0.4765 us |  3.100 us |
    //     | Bench_OneOpCode | 32        | 16.023 us | 1.3208 us | 3.8737 us | 14.300 us |
    //     | Bench_OneOpCode | 128       | 18.192 us | 1.3260 us | 3.9097 us | 17.500 us |
    //     | Bench_OneOpCode | 1024      | 30.912 us | 1.6036 us | 4.7031 us | 30.600 us |
    //     | Bench_OneOpCode | 2040      | 27.736 us | 1.7210 us | 5.0745 us | 27.200 us |
}
