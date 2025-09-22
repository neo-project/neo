// Copyright (C) 2015-2025 The Neo Project.
//
// LoopScriptFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Utility to compose loop-based scripts for opcode scenarios.
    /// </summary>
    internal static class LoopScriptFactory
    {
        public static byte[] BuildCountingLoop(ScenarioProfile profile, Action<InstructionBuilder> iteration, byte localCount = 1, byte argumentCount = 0) =>
            BuildCountingLoop(profile, null, iteration, null, localCount, argumentCount);

        public static byte[] BuildCountingLoop(
            ScenarioProfile profile,
            Action<InstructionBuilder>? prolog,
            Action<InstructionBuilder> iteration,
            Action<InstructionBuilder>? epilog = null,
            byte localCount = 1,
            byte argumentCount = 0)
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = new[] { localCount, argumentCount } });
            builder.Push(profile.Iterations);
            builder.AddInstruction(VM.OpCode.STLOC0);

            prolog?.Invoke(builder);

            var loopStart = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            iteration(builder);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.Jump(VM.OpCode.JMPIF_L, loopStart);
            epilog?.Invoke(builder);
            builder.AddInstruction(VM.OpCode.RET);
            return builder.ToArray();
        }

        public static byte[] BuildInfiniteLoop(Action<InstructionBuilder> iteration) =>
            BuildInfiniteLoop(null, iteration, null);

        public static byte[] BuildInfiniteLoop(
            Action<InstructionBuilder>? prolog,
            Action<InstructionBuilder> iteration,
            Action<InstructionBuilder>? epilog = null)
        {
            var builder = new InstructionBuilder();
            prolog?.Invoke(builder);
            var loopStart = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            iteration(builder);
            epilog?.Invoke(builder);
            builder.Jump(VM.OpCode.JMP_L, loopStart);
            builder.AddInstruction(VM.OpCode.RET);
            return builder.ToArray();
        }
    }
}
