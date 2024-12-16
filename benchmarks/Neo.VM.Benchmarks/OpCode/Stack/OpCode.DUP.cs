// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.DUP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_DUP : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.DUP;

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

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);

            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error         | StdDev        | Median         |
/// |---------------- |---------- |---------------:|--------------:|--------------:|---------------:|
/// | Bench_OneOpCode | 4         |       1.487 us |     0.0770 us |     0.2257 us |       1.500 us |
/// | Bench_OneGAS    | 4         | 399,715.307 us | 3,029.4881 us | 2,833.7850 us | 400,137.900 us |
/// | Bench_OneOpCode | 8         |       1.555 us |     0.0686 us |     0.2001 us |       1.550 us |
/// | Bench_OneGAS    | 8         | 406,537.147 us | 7,853.7506 us | 7,346.4032 us | 406,012.000 us |
/// | Bench_OneOpCode | 16        |       1.666 us |     0.0832 us |     0.2386 us |       1.700 us |
/// | Bench_OneGAS    | 16        | 411,354.273 us | 5,229.6614 us | 4,891.8285 us | 410,964.600 us |
/// | Bench_OneOpCode | 32        |       1.748 us |     0.0893 us |     0.2564 us |       1.700 us |
/// | Bench_OneGAS    | 32        | 409,337.807 us | 4,247.1435 us | 3,972.7807 us | 409,111.800 us |
/// | Bench_OneOpCode | 64        |       2.006 us |     0.1240 us |     0.3557 us |       1.900 us |
/// | Bench_OneGAS    | 64        | 409,228.087 us | 5,274.6258 us | 4,933.8882 us | 409,368.300 us |
/// | Bench_OneOpCode | 128       |       2.147 us |     0.3287 us |     0.9432 us |       1.900 us |
/// | Bench_OneGAS    | 128       | 397,918.073 us | 2,717.3272 us | 2,541.7896 us | 397,415.800 us |
/// | Bench_OneOpCode | 256       |       1.840 us |     0.2868 us |     0.8136 us |       1.700 us |
/// | Bench_OneGAS    | 256       | 402,291.013 us | 3,541.4276 us | 3,312.6535 us | 402,386.100 us |
/// | Bench_OneOpCode | 512       |       1.873 us |     0.2376 us |     0.6854 us |       1.800 us |
/// | Bench_OneGAS    | 512       | 403,765.313 us | 2,146.3901 us | 2,007.7346 us | 403,546.900 us |
/// | Bench_OneOpCode | 1024      |       2.573 us |     0.2870 us |     0.8188 us |       2.500 us |
