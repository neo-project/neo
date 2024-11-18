// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.DEPTH.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_DEPTH : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.DEPTH;

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
            builder.AddInstruction(VM.OpCode.DEPTH);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            throw new NotImplementedException();
        }
    }
}

//     | Method          | ItemCount | Mean     | Error     | StdDev    | Median   |
//     |---------------- |---------- |---------:|----------:|----------:|---------:|
//     | Bench_OneOpCode | 2         | 1.562 us | 0.0350 us | 0.0783 us | 1.600 us |
//     | Bench_OneOpCode | 32        | 1.552 us | 0.0346 us | 0.0823 us | 1.600 us |
//     | Bench_OneOpCode | 128       | 1.579 us | 0.0353 us | 0.0899 us | 1.600 us |
//     | Bench_OneOpCode | 1024      | 2.163 us | 0.2354 us | 0.6941 us | 1.700 us |
//     | Bench_OneOpCode | 2040      | 1.956 us | 0.2022 us | 0.5930 us | 1.600 us |


//     | Method          | ItemCount | Mean     | Error     | StdDev    |
//     |---------------- |---------- |---------:|----------:|----------:|
//     | Bench_OneOpCode | 2         | 1.752 us | 0.0364 us | 0.0735 us |
//     | Bench_OneOpCode | 32        | 2.308 us | 0.1270 us | 0.3643 us |
//     | Bench_OneOpCode | 128       | 2.969 us | 0.2071 us | 0.5976 us |
//     | Bench_OneOpCode | 1024      | 3.330 us | 0.3974 us | 1.1528 us |
//     | Bench_OneOpCode | 2040      | 2.985 us | 0.3049 us | 0.8698 us |
