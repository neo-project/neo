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
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    public partial class JumpTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DEPTH(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(engine.CurrentContext!.EvaluationStack.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DROP(ExecutionEngine engine, Instruction instruction)
        {
            engine.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NIP(ExecutionEngine engine, Instruction instruction)
        {
            engine.CurrentContext!.EvaluationStack.Remove<StackItem>(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void XDROP(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0)
                throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
            engine.CurrentContext!.EvaluationStack.Remove<StackItem>(n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void CLEAR(ExecutionEngine engine, Instruction instruction)
        {
            engine.CurrentContext!.EvaluationStack.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DUP(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(engine.Peek());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void OVER(ExecutionEngine engine, Instruction instruction)
        {
            engine.Push(engine.Peek(1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void PICK(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0)
                throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
            engine.Push(engine.Peek(n));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void TUCK(ExecutionEngine engine, Instruction instruction)
        {
            engine.CurrentContext!.EvaluationStack.Insert(2, engine.Peek());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SWAP(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.CurrentContext!.EvaluationStack.Remove<StackItem>(1);
            engine.Push(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ROT(ExecutionEngine engine, Instruction instruction)
        {
            var x = engine.CurrentContext!.EvaluationStack.Remove<StackItem>(2);
            engine.Push(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ROLL(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            if (n < 0)
                throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
            if (n == 0) return;
            var x = engine.CurrentContext!.EvaluationStack.Remove<StackItem>(n);
            engine.Push(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void REVERSE3(ExecutionEngine engine, Instruction instruction)
        {
            engine.CurrentContext!.EvaluationStack.Reverse(3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void REVERSE4(ExecutionEngine engine, Instruction instruction)
        {
            engine.CurrentContext!.EvaluationStack.Reverse(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void REVERSEN(ExecutionEngine engine, Instruction instruction)
        {
            var n = (int)engine.Pop().GetInteger();
            engine.CurrentContext!.EvaluationStack.Reverse(n);
        }
    }
}
