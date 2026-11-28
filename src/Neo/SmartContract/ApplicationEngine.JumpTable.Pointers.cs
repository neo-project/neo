// Copyright (C) 2015-2026 The Neo Project.
//
// ApplicationEngine.JumpTable.Pointers.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    public partial class ApplicationEngine
    {
        private static void CallA_BeforeHuyao(ExecutionEngine engine, Instruction instruction, ref RunStats runStats)
        {
            var pointer = engine.Pop<Pointer>();
            if (!ReferenceEquals(pointer.Script, engine.CurrentContext!.Script))
                throw new InvalidOperationException("Pointers can't be shared between scripts");
            DefaultJumpTable.ExecuteCall(engine, pointer.Position);
        }

        private static void Equal_BeforeHuyao(ExecutionEngine engine, Instruction instruction, ref RunStats runStats)
            => Compare_BeforeHuyao(engine, instruction, ref runStats, false);

        private static void NotEqual_BeforeHuyao(ExecutionEngine engine, Instruction instruction, ref RunStats runStats)
            => Compare_BeforeHuyao(engine, instruction, ref runStats, true);

        private static void Compare_BeforeHuyao(ExecutionEngine engine, Instruction instruction, ref RunStats runStats, bool negate)
        {
            var stack = engine.CurrentContext!.EvaluationStack;
            var first = stack.Peek(1);
            if (first is not Pointer and not Struct)
            {
                if (negate)
                    DefaultJumpTable.NotEqual(engine, instruction, ref runStats);
                else
                    DefaultJumpTable.Equal(engine, instruction, ref runStats);
                return;
            }

            var second = engine.Pop();
            first = engine.Pop();
            bool equal = first is Pointer pointer
                ? second is Pointer other && pointer.Position == other.Position && ReferenceEquals(pointer.Script, other.Script)
                : StructEquals_BeforeHuyao((Struct)first, second, engine.Limits);
            engine.Push(negate ? !equal : equal);
        }

        private static bool StructEquals_BeforeHuyao(Struct first, StackItem second, ExecutionEngineLimits limits)
        {
            if (second is not Struct other) return false;
            Stack<StackItem> left = new();
            Stack<StackItem> right = new();
            left.Push(first);
            right.Push(other);
            uint count = limits.MaxStackSize;
            uint comparableSize = limits.MaxComparableSize;
            while (left.Count > 0)
            {
                if (count-- == 0)
                    throw new InvalidOperationException("Too many struct items to compare in struct comparison.");
                StackItem a = left.Pop();
                StackItem b = right.Pop();
                if (a is ByteString bytes)
                {
                    if (bytes.Size > comparableSize || comparableSize == 0)
                        throw new InvalidOperationException($"The operand exceeds the maximum comparable size, {bytes.Size}/{comparableSize}.");
                    uint comparedSize = 1;
                    if (b is not ByteString otherBytes) return false;
                    comparedSize = Math.Max((uint)Math.Max(bytes.Size, otherBytes.Size), comparedSize);
                    if (otherBytes.Size > comparableSize)
                        throw new InvalidOperationException($"The operand exceeds the maximum comparable size, {otherBytes.Size}/{comparableSize}.");
                    comparableSize -= comparedSize;
                    if (!bytes.GetSpan().SequenceEqual(otherBytes.GetSpan())) return false;
                }
                else
                {
                    if (comparableSize == 0)
                        throw new InvalidOperationException("The operand exceeds the maximum comparable size in struct comparison.");
                    comparableSize--;
                    if (a is Struct nested)
                    {
                        if (ReferenceEquals(a, b)) continue;
                        if (b is not Struct otherNested || nested.Count != otherNested.Count) return false;
                        foreach (StackItem item in nested) left.Push(item);
                        foreach (StackItem item in otherNested) right.Push(item);
                    }
                    else if (a is Pointer pointer)
                    {
                        if (b is not Pointer otherPointer || pointer.Position != otherPointer.Position || !ReferenceEquals(pointer.Script, otherPointer.Script)) return false;
                    }
                    else if (!a.Equals(b)) return false;
                }
            }
            return true;
        }
    }
}
