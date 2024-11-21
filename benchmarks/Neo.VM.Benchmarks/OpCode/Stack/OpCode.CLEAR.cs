// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.CLEAR.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_CLEAR : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.CLEAR;

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
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript( )
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
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
            builder.AddInstruction(Opcode);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

// | Method          | ItemCount | Mean             | Error           | StdDev          | Median           |
//     |---------------- |---------- |-----------------:|----------------:|----------------:|-----------------:|
//     | Bench_OneOpCode | 2         |         1.377 us |       0.0615 us |       0.1653 us |         1.300 us |
//     | Bench_OneGAS    | 2         | 4,231,554.676 us |  83,909.7077 us | 184,183.8425 us | 4,303,600.900 us |
//     | Bench_OneOpCode | 32        |         2.401 us |       0.1199 us |       0.3515 us |         2.400 us |
//     | Bench_OneGAS    | 32        | 2,547,750.008 us |  49,819.7387 us |  83,237.5915 us | 2,543,539.850 us |
//     | Bench_OneOpCode | 128       |         5.956 us |       0.3384 us |       0.9601 us |         5.900 us |
//     | Bench_OneGAS    | 128       | 2,639,446.005 us | 179,671.1278 us | 529,764.3742 us | 2,624,183.250 us |
//     | Bench_OneOpCode | 1024      |        14.652 us |       2.9867 us |       8.7123 us |        18.950 us |
//     | Bench_OneGAS    | 1024      | 1,805,836.310 us | 113,124.7967 us | 329,990.3417 us | 1,806,568.700 us |
//     | Bench_OneOpCode | 2040      |        18.231 us |       3.2867 us |       9.6393 us |        22.300 us |
//     | Bench_OneGAS    | 2040      | 1,855,640.206 us | 108,324.1031 us | 319,396.0621 us | 1,871,437.150 us |
