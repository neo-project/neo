// Copyright (C) 2015-2024 The Neo Project.
//
// JumpTable.Push.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public partial class JumpTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHINT8(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHINT16(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHINT32(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHINT64(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHINT128(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHINT256(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHT(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.True);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHF(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.False);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHA(ExecutionEngine engine, Instruction instruction)
        {
            var position = checked(engine.CurrentContext!.InstructionPointer + instruction.TokenI32);
            if (position < 0 || position > engine.CurrentContext.Script.Length)
                throw new InvalidOperationException($"Bad pointer address(Instruction instruction) {position}");
            engine.Push(new Pointer(engine.CurrentContext.Script, position));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHNULL(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.Null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHDATA1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHDATA2(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHDATA4(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHM1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(-1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH0(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH2(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH3(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH4(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH5(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH6(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH7(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH8(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH9(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(9);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH10(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH11(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH12(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH13(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(13);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH14(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH15(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSH16(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(16);
        }
    }
}
