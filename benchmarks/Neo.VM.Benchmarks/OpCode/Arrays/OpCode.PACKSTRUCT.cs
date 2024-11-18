// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PACKSTRUCT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_PACKSTRUCT : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.PACKSTRUCT;

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
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            throw new NotImplementedException();
        }
    }

    // | Method          | ItemCount | Mean      | Error     | StdDev    | Median    |
    //     |---------------- |---------- |----------:|----------:|----------:|----------:|
    //     | Bench_OneOpCode | 1         |  2.895 us | 0.1234 us | 0.3314 us |  2.800 us |
    //     | Bench_OneOpCode | 32        |  4.445 us | 0.1109 us | 0.2979 us |  4.300 us |
    //     | Bench_OneOpCode | 128       |  8.186 us | 0.1654 us | 0.3107 us |  8.100 us |
    //     | Bench_OneOpCode | 1024      | 41.397 us | 0.8243 us | 1.8094 us | 41.100 us |
    //     | Bench_OneOpCode | 2040      | 59.211 us | 1.1815 us | 2.0062 us | 58.900 us |


    // ushort.max *2
    // | Method          | ItemCount | Mean       | Error      | StdDev     | Median     |
    //     |---------------- |---------- |-----------:|-----------:|-----------:|-----------:|
    //     | Bench_OneOpCode | 1         |   3.479 us |  0.1016 us |  0.2693 us |   3.450 us |
    //     | Bench_OneOpCode | 32        |   9.766 us |  0.3572 us |  1.0363 us |   9.500 us |
    //     | Bench_OneOpCode | 128       |  27.095 us |  2.5971 us |  7.6168 us |  22.300 us |
    //     | Bench_OneOpCode | 1024      |  83.929 us |  3.2253 us |  8.7746 us |  84.200 us |
    //     | Bench_OneOpCode | 2040      | 164.479 us | 15.2821 us | 42.3467 us | 146.100 us |

}
