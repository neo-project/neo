// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.REVERSE3.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_REVERSE3 : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.REVERSE3;

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

/// | Method          | ItemCount | Mean           | Error         | StdDev        |
/// |---------------- |---------- |---------------:|--------------:|--------------:|
/// | Bench_OneOpCode | 4         |       1.531 us |     0.0936 us |     0.2759 us |
/// | Bench_OneGAS    | 4         | 420,008.888 us | 2,806.9195 us | 3,747.1573 us |
/// | Bench_OneOpCode | 8         |       1.640 us |     0.0770 us |     0.2258 us |
/// | Bench_OneGAS    | 8         | 416,729.514 us | 2,649.4601 us | 2,348.6774 us |
/// | Bench_OneOpCode | 16        |       1.644 us |     0.0816 us |     0.2381 us |
/// | Bench_OneGAS    | 16        | 416,861.413 us | 5,262.6788 us | 4,922.7130 us |
/// | Bench_OneOpCode | 32        |       1.702 us |     0.0867 us |     0.2403 us |
/// | Bench_OneGAS    | 32        | 410,911.993 us | 2,365.3271 us | 2,096.8009 us |
/// | Bench_OneOpCode | 64        |       1.680 us |     0.1454 us |     0.4220 us |
/// | Bench_OneGAS    | 64        | 413,322.060 us | 4,421.1163 us | 4,135.5149 us |
/// | Bench_OneOpCode | 128       |       2.336 us |     0.2829 us |     0.8297 us |
/// | Bench_OneGAS    | 128       | 419,681.943 us | 4,534.3493 us | 4,019.5825 us |
/// | Bench_OneOpCode | 256       |       2.114 us |     0.3770 us |     1.0997 us |
/// | Bench_OneGAS    | 256       | 412,889.407 us | 2,681.4425 us | 2,508.2230 us |
/// | Bench_OneOpCode | 512       |       2.204 us |     0.3683 us |     1.0626 us |
/// | Bench_OneGAS    | 512       | 407,308.713 us | 5,549.6590 us | 5,191.1544 us |
/// | Bench_OneOpCode | 1024      |       2.509 us |     0.3710 us |     1.0586 us |
/// | Bench_OneGAS    | 1024      | 419,020.907 us | 4,738.8630 us | 4,432.7354 us |
/// | Bench_OneOpCode | 2040      |       1.785 us |     0.3106 us |     0.8962 us |
/// | Bench_OneGAS    | 2040      | 405,451.107 us | 4,737.3787 us | 4,431.3470 us |
