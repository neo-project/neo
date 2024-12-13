// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.KEYS.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_KEYS : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.KEYS;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();

            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);

            builder.Push(VM.OpCode.LDLOC0); //Value
            builder.Push(VM.OpCode.LDLOC0); //Key

            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);


            builder.Push(ItemCount); //Size
            builder.AddInstruction(VM.OpCode.PACKMAP);

            builder.AddInstruction(Opcode); //OpCode.Keys
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();

            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);

            builder.Push(VM.OpCode.LDLOC0); //Value
            builder.Push(VM.OpCode.LDLOC0); //Key

            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);

            builder.Push(ItemCount); //Size
            builder.AddInstruction(VM.OpCode.PACKMAP);

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) }; // now deadloop until you reach 1 GAS
            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(Opcode); //OpCode.Keys
            builder.AddInstruction(VM.OpCode.DROP); // Drop the haskey result to clear the stack
            builder.Jump(VM.OpCode.JMP, loopBegin); // loop

            return builder.ToArray();
        }
    }
}

/// | Method | ItemCount | Mean | Error | StdDev | Median |
/// | ---------------- | ---------- | ---------------:| --------------:| --------------:| ---------------:|
/// | Bench_OneOpCode | 4 | 6.076 us | 0.4348 us | 1.2751 us | 5.500 us |
/// | Bench_OneGAS | 4 | 311,189.871 us | 5,036.3181 us | 4,464.5648 us | 311,286.500 us |
/// | Bench_OneOpCode | 8 | 5.319 us | 0.1088 us | 0.1818 us | 5.300 us |
/// | Bench_OneGAS | 8 | 317,724.307 us | 3,762.3339 us | 3,335.2110 us | 318,146.950 us |
/// | Bench_OneOpCode | 16 | 6.031 us | 0.3499 us | 1.0150 us | 5.600 us |
/// | Bench_OneGAS | 16 | 305,824.240 us | 5,264.4883 us | 4,924.4056 us | 304,434.000 us |
/// | Bench_OneOpCode | 32 | 6.469 us | 0.4033 us | 1.1828 us | 6.100 us |
/// | Bench_OneGAS | 32 | 318,095.000 us | 2,928.8930 us | 2,739.6883 us | 318,514.600 us |
/// | Bench_OneOpCode | 64 | 6.109 us | 0.3671 us | 1.0766 us | 5.700 us |
/// | Bench_OneGAS | 64 | 315,642.073 us | 4,854.5661 us | 4,540.9642 us | 314,314.000 us |
/// | Bench_OneOpCode | 128 | 6.690 us | 0.3704 us | 1.0804 us | 6.400 us |
/// | Bench_OneGAS | 128 | 318,420.779 us | 4,122.0329 us | 3,654.0748 us | 318,497.100 us |
/// | Bench_OneOpCode | 256 | 6.869 us | 0.3237 us | 0.9340 us | 6.500 us |
/// | Bench_OneGAS | 256 | 317,932.773 us | 5,932.4303 us | 5,549.1990 us | 316,874.000 us |
/// | Bench_OneOpCode | 512 | 7.047 us | 0.4292 us | 1.2655 us | 6.600 us |
/// | Bench_OneGAS | 512 | 309,127.043 us | 2,595.8056 us | 2,301.1141 us | 309,353.300 us |
/// | Bench_OneOpCode | 1024 | NA | NA | NA | NA |
/// | Bench_OneGAS | 1024 | NA | NA | NA | NA |
/// | Bench_OneOpCode | 2040 | NA | NA | NA | NA |
/// | Bench_OneGAS | 2040 | NA | NA | NA | NA |
