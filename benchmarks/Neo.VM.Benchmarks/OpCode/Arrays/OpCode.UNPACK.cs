// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.UNPACK.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_UNPACK : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.UNPACK;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            // builder.Push(0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            throw new NotImplementedException();
        }
    }

    // 0
    // | Method          | ItemCount | Mean      | Error     | StdDev    | Median    |
    //     |---------------- |---------- |----------:|----------:|----------:|----------:|
    //     | Bench_OneOpCode | 1         |  2.237 us | 0.0537 us | 0.1480 us |  2.200 us |
    //     | Bench_OneOpCode | 32        |  2.792 us | 0.0607 us | 0.1642 us |  2.700 us |
    //     | Bench_OneOpCode | 128       |  4.751 us | 0.1897 us | 0.5194 us |  4.700 us |
    //     | Bench_OneOpCode | 1024      | 66.738 us | 1.3316 us | 3.2160 us | 66.400 us |
    //     | Bench_OneOpCode | 2040      | 97.254 us | 1.7441 us | 1.4564 us | 97.400 us |


    // ushort.max*2
    // | Method          | ItemCount | Mean       | Error       | StdDev      | Median     |
    //     |---------------- |---------- |-----------:|------------:|------------:|-----------:|
    //     | Bench_OneOpCode | 1         |   2.423 us |   0.0754 us |   0.1932 us |   2.400 us |
    //     | Bench_OneOpCode | 32        |   5.960 us |   0.4481 us |   1.2493 us |   5.800 us |
    //     | Bench_OneOpCode | 128       |  10.803 us |   0.7475 us |   2.1922 us |  10.000 us |
    //     | Bench_OneOpCode | 1024      | 190.762 us |  12.6335 us |  34.3703 us | 191.250 us |
    //     | Bench_OneOpCode | 2040      | 496.604 us | 105.7599 us | 308.5065 us | 325.600 us |
}
