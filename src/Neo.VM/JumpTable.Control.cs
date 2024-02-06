// Copyright (C) 2015-2024 The Neo Project.
//
// JumpTable.Control.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public partial class JumpTable
    {
        [OpcodeMethod(OpCode.NOP)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Nop(ExecutionEngine engine, Instruction instruction)
        {
        }

        [OpcodeMethod(OpCode.JMP)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Jmp(ExecutionEngine engine, Instruction instruction)
        {
            engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [OpcodeMethod(OpCode.JMP_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpL(ExecutionEngine engine, Instruction instruction)
        {
            engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [OpcodeMethod(OpCode.JMPIF)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpIf(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.Pop().GetBoolean())
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [OpcodeMethod(OpCode.JMPIF_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpIfL(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.Pop().GetBoolean())
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [OpcodeMethod(OpCode.JMPIFNOT)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpIfNot(ExecutionEngine engine, Instruction instruction)
        {
            if (!engine.Pop().GetBoolean())
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [OpcodeMethod(OpCode.JMPIFNOT_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpIfNotL(ExecutionEngine engine, Instruction instruction)
        {
            if (!engine.Pop().GetBoolean())
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [OpcodeMethod(OpCode.JMPEQ)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpEq(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 == x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [OpcodeMethod(OpCode.JMPEQ_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpEqL(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 == x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [OpcodeMethod(OpCode.JMPNE)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpNe(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 != x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [OpcodeMethod(OpCode.JMPNE_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpNeL(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 != x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [OpcodeMethod(OpCode.JMPGT)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpGt(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 > x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [OpcodeMethod(OpCode.JMPGT_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpGtL(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 > x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [OpcodeMethod(OpCode.JMPGE)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpGe(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 >= x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [OpcodeMethod(OpCode.JMPGE_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpGeL(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 >= x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [OpcodeMethod(OpCode.JMPLT)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpLt(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 < x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [OpcodeMethod(OpCode.JMPLT_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpLtL(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 < x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [OpcodeMethod(OpCode.JMPLE)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpLe(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 <= x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [OpcodeMethod(OpCode.JMPLE_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JmpLeL(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 <= x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [OpcodeMethod(OpCode.CALL)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Call(ExecutionEngine engine, Instruction instruction)
        {
            engine.ExecuteCall(checked(engine.CurrentContext!.InstructionPointer + instruction.TokenI8));
        }

        [OpcodeMethod(OpCode.CALL_L)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CallL(ExecutionEngine engine, Instruction instruction)
        {
            engine.ExecuteCall(checked(engine.CurrentContext!.InstructionPointer + instruction.TokenI32));
        }

        /* TODO
               case OpCode.CALLA:
               case OpCode.CALLT:
               case OpCode.ABORT:
               case OpCode.ASSERT:
               case OpCode.THROW:
               case OpCode.TRY:
               case OpCode.TRY_L:
               case OpCode.ENDTRY:
               case OpCode.ENDTRY_L:
               case OpCode.ENDFINALLY:
               case OpCode.RET:
               case OpCode.SYSCALL:
        */
    }
}
