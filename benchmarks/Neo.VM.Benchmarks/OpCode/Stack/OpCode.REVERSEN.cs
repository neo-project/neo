// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.REVERSEN.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_REVERSEN : OpCodeBase
{
    protected override byte[] CreateScript(BenchmarkMode benchmarkMode)
    {
        var builder = new InstructionBuilder();
        var initBegin = new JumpTarget();
        builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
        builder.Push(ItemCount);
        builder.AddInstruction(VM.OpCode.STLOC0);
        initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
        builder.Push(0);
        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.AddInstruction(VM.OpCode.DEC);
        builder.AddInstruction(VM.OpCode.STLOC0);
        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.Jump(VM.OpCode.JMPIF, initBegin);
        if (benchmarkMode == BenchmarkMode.BaseLine)
        {
            return builder.ToArray();
        }
        builder.Push(ItemCount);
        builder.AddInstruction(VM.OpCode.REVERSEN);
        if (benchmarkMode == BenchmarkMode.OneGAS)
        {
            // just keep running until GAS is exhausted
            var loopStart = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.REVERSEN);
            builder.Jump(VM.OpCode.JMP, loopStart);
        }

        return builder.ToArray();
    }
}
