// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.LDSFLD.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_LDSFLD : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.LDLOC0;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}


//     | Method          | ItemCount | Mean             | Error           | StdDev          | Median           |
//     |---------------- |---------- |-----------------:|----------------:|----------------:|-----------------:|
//     | Bench_OneOpCode | 2         |       1,133.8 ns |        26.64 ns |        65.34 ns |       1,100.0 ns |
//     | Bench_OneGAS    | 2         | 363,704,866.7 ns | 2,621,811.75 ns | 2,046,938.16 ns | 363,888,650.0 ns |
//     | Bench_OneOpCode | 32        |       1,301.0 ns |       127.96 ns |       371.23 ns |       1,100.0 ns |
//     | Bench_OneGAS    | 32        | 365,834,392.9 ns | 4,653,715.49 ns | 4,125,397.56 ns | 365,476,500.0 ns |
//     | Bench_OneOpCode | 128       |       1,013.6 ns |        26.68 ns |        70.27 ns |       1,000.0 ns |
//     | Bench_OneGAS    | 128       | 366,044,500.0 ns | 6,067,918.98 ns | 5,675,935.18 ns | 365,388,400.0 ns |
//     | Bench_OneOpCode | 1024      |         985.2 ns |        34.54 ns |        90.98 ns |       1,000.0 ns |
//     | Bench_OneGAS    | 1024      | 361,353,723.1 ns | 2,865,412.98 ns | 2,392,749.61 ns | 361,204,100.0 ns |
//     | Bench_OneOpCode | 2040      |       1,015.5 ns |        27.41 ns |        73.62 ns |       1,000.0 ns |
//     | Bench_OneGAS    | 2040      | 359,853,366.7 ns | 2,829,191.84 ns | 2,208,846.89 ns | 358,942,750.0 ns |
