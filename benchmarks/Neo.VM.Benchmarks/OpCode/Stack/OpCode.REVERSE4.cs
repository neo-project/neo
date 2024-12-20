// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.REVERSE4.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_REVERSE4 : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.REVERSE4;

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
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            builder.AddInstruction(Opcode);
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
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(Opcode);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error         | StdDev         |
/// |---------------- |---------- |---------------:|--------------:|---------------:|
/// | Bench_OneOpCode | 4         |       1.448 us |     0.1110 us |      0.3274 us |
/// | Bench_OneGAS    | 4         | 428,752.779 us | 4,087.6952 us |  3,623.6353 us |
/// | Bench_OneOpCode | 8         |       1.512 us |     0.1222 us |      0.3545 us |
/// | Bench_OneGAS    | 8         | 414,702.600 us | 3,945.6514 us |  3,294.7976 us |
/// | Bench_OneOpCode | 16        |       1.536 us |     0.1244 us |      0.3649 us |
/// | Bench_OneGAS    | 16        | 428,474.029 us | 3,866.2943 us |  3,427.3692 us |
/// | Bench_OneOpCode | 32        |       1.508 us |     0.1359 us |      0.3986 us |
/// | Bench_OneGAS    | 32        | 441,603.324 us | 8,526.9097 us | 11,383.1805 us |
/// | Bench_OneOpCode | 64        |       2.004 us |     0.1615 us |      0.4555 us |
/// | Bench_OneGAS    | 64        | 435,719.829 us | 3,891.5682 us |  3,449.7739 us |
/// | Bench_OneOpCode | 128       |       2.217 us |     0.2355 us |      0.6832 us |
/// | Bench_OneGAS    | 128       | 434,403.029 us | 4,909.6591 us |  4,352.2850 us |
/// | Bench_OneOpCode | 256       |       2.014 us |     0.3696 us |      1.0485 us |
/// | Bench_OneGAS    | 256       | 434,472.431 us | 2,923.5623 us |  2,441.3070 us |
/// | Bench_OneOpCode | 512       |       2.275 us |     0.3304 us |      0.9585 us |
/// | Bench_OneGAS    | 512       | 434,447.321 us | 3,437.6060 us |  3,047.3482 us |
/// | Bench_OneOpCode | 1024      |       2.580 us |     0.4383 us |      1.2575 us |
/// | Bench_OneGAS    | 1024      | 438,286.308 us | 4,917.4668 us |  4,106.3075 us |
/// | Bench_OneOpCode | 2040      |       2.014 us |     0.3649 us |      1.0758 us |
/// | Bench_OneGAS    | 2040      | 434,674.827 us | 2,835.7096 us |  2,652.5245 us |
