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

/// | Method          | ItemCount | Mean           | Error         | StdDev        | Median         |
/// |---------------- |---------- |---------------:|--------------:|--------------:|---------------:|
/// | Bench_OneOpCode | 4         |       5.770 us |     0.1180 us |     0.3002 us |       5.700 us |
/// | Bench_OneGAS    | 4         | 321,068.540 us | 5,352.9497 us | 5,007.1525 us | 319,411.700 us |
/// | Bench_OneOpCode | 8         |       5.795 us |     0.1193 us |     0.3142 us |       5.800 us |
/// | Bench_OneGAS    | 8         | 319,080.907 us | 5,478.2962 us | 4,856.3669 us | 317,455.100 us |
/// | Bench_OneOpCode | 16        |       5.776 us |     0.1187 us |     0.2728 us |       5.700 us |
/// | Bench_OneGAS    | 16        | 312,350.647 us | 4,151.8398 us | 3,883.6335 us | 311,264.300 us |
/// | Bench_OneOpCode | 32        |       6.224 us |     0.3306 us |     0.9746 us |       5.800 us |
/// | Bench_OneGAS    | 32        | 316,735.927 us | 2,684.2112 us | 2,510.8128 us | 317,492.800 us |
/// | Bench_OneOpCode | 64        |       6.163 us |     0.2396 us |     0.6876 us |       5.900 us |
/// | Bench_OneGAS    | 64        | 316,759.207 us | 3,102.5177 us | 2,902.0970 us | 316,835.500 us |
/// | Bench_OneOpCode | 128       |       5.588 us |     0.2422 us |     0.6547 us |       5.600 us |
/// | Bench_OneGAS    | 128       | 315,794.640 us | 2,477.8688 us | 2,317.8000 us | 315,204.400 us |
/// | Bench_OneOpCode | 256       |       6.369 us |     0.3181 us |     0.9228 us |       6.000 us |
/// | Bench_OneGAS    | 256       | 313,106.920 us | 2,772.6315 us | 2,593.5212 us | 313,483.200 us |
/// | Bench_OneOpCode | 512       |       6.661 us |     0.2829 us |     0.7978 us |       6.400 us |
/// | Bench_OneGAS    | 512       | 314,332.029 us | 1,938.6764 us | 1,718.5861 us | 314,657.850 us |
/// | Bench_OneOpCode | 1024      |             NA |            NA |            NA |             NA |
/// | Bench_OneGAS    | 1024      |             NA |            NA |            NA |             NA |
/// | Bench_OneOpCode | 2040      |             NA |            NA |            NA |             NA |
/// | Bench_OneGAS    | 2040      |             NA |            NA |            NA |             NA |
