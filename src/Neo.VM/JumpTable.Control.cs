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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NOP(ExecutionEngine engine, Instruction instruction)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMP(ExecutionEngine engine, Instruction instruction)
        {
            engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMP_L(ExecutionEngine engine, Instruction instruction)
        {
            engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPIF(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.Pop().GetBoolean())
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPIF_L(ExecutionEngine engine, Instruction instruction)
        {
            if (engine.Pop().GetBoolean())
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPIFNOT(ExecutionEngine engine, Instruction instruction)
        {
            if (!engine.Pop().GetBoolean())
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPIFNOT_L(ExecutionEngine engine, Instruction instruction)
        {
            if (!engine.Pop().GetBoolean())
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPEQ(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 == x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPEQ_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 == x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPNE(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 != x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPNE_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 != x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPGT(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 > x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPGT_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 > x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPGE(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 >= x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPGE_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 >= x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPLT(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 < x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPLT_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 < x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPLE(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 <= x2)
                engine.ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void JMPLE_L(ExecutionEngine engine, Instruction instruction)
        {
            var x2 = engine.Pop().GetInteger();
            var x1 = engine.Pop().GetInteger();
            if (x1 <= x2)
                engine.ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CALL(ExecutionEngine engine, Instruction instruction)
        {
            engine.ExecuteCall(checked(engine.CurrentContext!.InstructionPointer + instruction.TokenI8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CALL_L(ExecutionEngine engine, Instruction instruction)
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
