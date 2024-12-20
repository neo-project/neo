// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.ROLL.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_ROLL : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.ROLL;

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

            builder.Push(ItemCount - 1);
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

            builder.Push(ItemCount - 1);
            builder.AddInstruction(Opcode);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error         | StdDev        |
/// |---------------- |---------- |---------------:|--------------:|--------------:|
/// | Bench_OneOpCode | 4         |       2.236 us |     0.1728 us |     0.5041 us |
/// | Bench_OneGAS    | 4         | 205,228.440 us | 3,724.7004 us | 3,484.0871 us |
/// | Bench_OneOpCode | 8         |       2.221 us |     0.1680 us |     0.4953 us |
/// | Bench_OneGAS    | 8         | 199,327.945 us | 2,765.9412 us | 4,916.4581 us |
/// | Bench_OneOpCode | 16        |       2.305 us |     0.2480 us |     0.7312 us |
/// | Bench_OneGAS    | 16        | 208,034.013 us | 2,830.7401 us | 2,647.8760 us |
/// | Bench_OneOpCode | 32        |       2.728 us |     0.2167 us |     0.6321 us |
/// | Bench_OneGAS    | 32        | 213,990.507 us | 2,268.8248 us | 2,122.2601 us |
/// | Bench_OneOpCode | 64        |       2.844 us |     0.1831 us |     0.5313 us |
/// | Bench_OneGAS    | 64        | 223,507.527 us | 2,351.0471 us | 2,199.1709 us |
/// | Bench_OneOpCode | 128       |       3.041 us |     0.3582 us |     1.0393 us |
/// | Bench_OneGAS    | 128       | 231,296.267 us | 2,934.5367 us | 2,744.9675 us |
/// | Bench_OneOpCode | 256       |       2.698 us |     0.3417 us |     0.9913 us |
/// | Bench_OneGAS    | 256       | 252,419.586 us | 2,397.1406 us | 2,125.0027 us |
/// | Bench_OneOpCode | 512       |       3.025 us |     0.3503 us |     1.0106 us |
/// | Bench_OneGAS    | 512       | 304,450.700 us | 4,754.7721 us | 4,214.9816 us |
/// | Bench_OneOpCode | 1024      |       3.432 us |     0.3181 us |     0.9280 us |
/// | Bench_OneGAS    | 1024      | 362,570.829 us | 3,107.9063 us | 2,755.0780 us |
/// | Bench_OneOpCode | 2040      |       3.327 us |     0.2397 us |     0.6991 us |
/// | Bench_OneGAS    | 2040      | 498,622.329 us | 4,426.6578 us | 3,924.1168 us |
