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
        public virtual void PushInt8(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt16(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt32(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt64(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt128(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushInt256(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(new BigInteger(instruction.Operand.Span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushT(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.True);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PUSHF(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.False);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushA(ExecutionEngine engine, Instruction instruction)
        {
            var position = checked(engine.CurrentContext!.InstructionPointer + instruction.TokenI32);
            if (position < 0 || position > engine.CurrentContext.Script.Length)
                throw new InvalidOperationException($"Bad pointer address(Instruction instruction) {position}");
            engine.Push(new Pointer(engine.CurrentContext.Script, position));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushNull(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(StackItem.Null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushData1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushData2(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushData4(ExecutionEngine engine, Instruction instruction)
        {
            engine.Limits.AssertMaxItemSize(instruction.Operand.Length);
            engine.Push(instruction.Operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PushM1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(-1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push0(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push1(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push2(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push3(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push4(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push5(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push6(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push7(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push8(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push9(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(9);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push10(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push11(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push12(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push13(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(13);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push14(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push15(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Push16(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(16);
        }
    }
}
