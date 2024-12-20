// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.DROP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_DROP : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.DROP;

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

        protected override byte[] CreateOneGASScript()
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
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.AddInstruction(Opcode);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean             | Error           | StdDev          | Median           |
/// |---------------- |---------- |-----------------:|----------------:|----------------:|-----------------:|
/// | Bench_OneOpCode | 4         |         2.303 us |       0.1030 us |       0.2938 us |         2.300 us |
/// | Bench_OneGAS    | 4         |   212,726.067 us |   4,110.3196 us |   4,397.9959 us |   211,436.600 us |
/// | Bench_OneOpCode | 8         |         2.775 us |       0.1186 us |       0.3402 us |         2.700 us |
/// | Bench_OneGAS    | 8         |   455,625.907 us |  13,806.3875 us |  40,708.4479 us |   441,782.700 us |
/// | Bench_OneOpCode | 16        |         3.049 us |       0.1430 us |       0.4079 us |         2.900 us |
/// | Bench_OneGAS    | 16        |   959,922.988 us |  19,161.8771 us |  37,823.6788 us |   957,742.300 us |
/// | Bench_OneOpCode | 32        |         3.959 us |       0.1917 us |       0.5438 us |         3.900 us |
/// | Bench_OneGAS    | 32        | 2,437,348.380 us |  48,054.4764 us | 117,878.4583 us | 2,446,554.400 us |
/// | Bench_OneOpCode | 64        |         5.620 us |       0.2815 us |       0.8030 us |         5.600 us |
/// | Bench_OneGAS    | 64        | 2,872,760.714 us | 166,562.4809 us | 491,113.2687 us | 2,841,859.900 us |
/// | Bench_OneOpCode | 128       |         5.957 us |       0.9815 us |       2.8939 us |         7.100 us |
/// | Bench_OneGAS    | 128       | 3,065,885.583 us | 179,512.9245 us | 526,480.3710 us | 3,039,864.500 us |
/// | Bench_OneOpCode | 256       |         7.266 us |       1.7125 us |       5.0494 us |         3.900 us |
/// | Bench_OneGAS    | 256       | 2,476,305.753 us | 140,225.2341 us | 413,457.2966 us | 2,412,541.750 us |
/// | Bench_OneOpCode | 512       |         4.588 us |       0.2526 us |       0.7041 us |         4.550 us |
/// | Bench_OneGAS    | 512       | 2,084,555.849 us | 138,291.6739 us | 407,756.1502 us | 1,993,295.250 us |
/// | Bench_OneOpCode | 1024      |         5.770 us |       0.3625 us |       1.0044 us |         5.500 us |
/// | Bench_OneGAS    | 1024      | 1,909,691.591 us | 114,110.0703 us | 334,665.1075 us | 1,901,288.400 us |
/// | Bench_OneOpCode | 2040      |         9.324 us |       0.4825 us |       1.3998 us |         8.900 us |
/// | Bench_OneGAS    | 2040      | 1,776,001.333 us | 102,368.0234 us | 301,834.4486 us | 1,818,497.100 us |
