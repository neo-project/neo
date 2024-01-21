// Copyright (C) 2015-2024 The Neo Project.
//
// ExecutionEngine.Instruction.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Buffer = Neo.VM.Types.Buffer;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM
{
    partial class ExecutionEngine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushInt(Instruction instruction)
        {
            Push(new BigInteger(instruction.Operand.Span));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushT(Instruction instruction)
        {
            Push(StackItem.True);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushF(Instruction instruction)
        {
            Push(StackItem.False);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushA(Instruction instruction)
        {
            int position = checked(CurrentContext!.InstructionPointer + instruction.TokenI32);
            if (position < 0 || position > CurrentContext.Script.Length)
                throw new InvalidOperationException($"Bad pointer address(Instruction instruction) {position}");
            Push(new Pointer(CurrentContext.Script, position));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushNull(Instruction instruction)
        {
            Push(StackItem.Null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushData(Instruction instruction)
        {
            Limits.AssertMaxItemSize(instruction.Operand.Length);
            Push(instruction.Operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(Instruction instruction)
        {
            Push((int)instruction.OpCode - (int)OpCode.PUSH0);
        }

        // Control
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Jmp(Instruction instruction)
        {
            ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Jmp_L(Instruction instruction)
        {
            ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpIf(Instruction instruction)
        {
            if (Pop().GetBoolean())
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpIf_L(Instruction instruction)
        {
            if (Pop().GetBoolean())
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpIfNot(Instruction instruction)
        {
            if (!Pop().GetBoolean())
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpIfNot_L(Instruction instruction)
        {
            if (!Pop().GetBoolean())
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpEq(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 == x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpEq_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 == x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpNe(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 != x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpNe_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 != x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpGt(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 > x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpGt_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 > x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpGe(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 >= x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpGe_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 >= x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpLt(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 < x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpLt_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 < x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpLe(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 <= x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JmpLe_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 <= x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Call(Instruction instruction)
        {
            ExecuteCall(checked(CurrentContext!.InstructionPointer + instruction.TokenI8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Call_L(Instruction instruction)
        {
            ExecuteCall(checked(CurrentContext!.InstructionPointer + instruction.TokenI32));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallA(Instruction instruction)
        {
            var x = Pop<Pointer>();
            if (x.Script != CurrentContext!.Script)
                throw new InvalidOperationException("Pointers can't be shared between scripts");
            ExecuteCall(x.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallT(Instruction instruction)
        {
            LoadToken(instruction.TokenU16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Abort(Instruction instruction)
        {
            throw new Exception($"{OpCode.ABORT} is executed.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Assert(Instruction instruction)
        {
            var x = Pop().GetBoolean();
            if (!x)
                throw new Exception($"{OpCode.ASSERT} is executed with false result.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Throw(Instruction instruction)
        {
            ExecuteThrow(Pop());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Try(Instruction instruction)
        {
            int catchOffset = instruction.TokenI8;
            int finallyOffset = instruction.TokenI8_1;
            ExecuteTry(catchOffset, finallyOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Try_L(Instruction instruction)
        {
            int catchOffset = instruction.TokenI32;
            int finallyOffset = instruction.TokenI32_1;
            ExecuteTry(catchOffset, finallyOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndTry(Instruction instruction)
        {
            int endOffset = instruction.TokenI8;
            ExecuteEndTry(endOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndTry_L(Instruction instruction)
        {
            int endOffset = instruction.TokenI32;
            ExecuteEndTry(endOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndFinally(Instruction instruction)
        {
            if (CurrentContext!.TryStack is null)
                throw new InvalidOperationException($"The corresponding TRY block cannot be found.");
            if (!CurrentContext.TryStack.TryPop(out ExceptionHandlingContext? currentTry))
                throw new InvalidOperationException($"The corresponding TRY block cannot be found.");

            if (UncaughtException is null)
                CurrentContext.InstructionPointer = currentTry.EndPointer;
            else
                HandleException();

            isJumping = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Ret(Instruction instruction)
        {
            ExecutionContext context_pop = InvocationStack.Pop();
            EvaluationStack stack_eval = InvocationStack.Count == 0 ? ResultStack : InvocationStack.Peek().EvaluationStack;
            if (context_pop.EvaluationStack != stack_eval)
            {
                if (context_pop.RVCount >= 0 && context_pop.EvaluationStack.Count != context_pop.RVCount)
                    throw new InvalidOperationException("RVCount doesn't match with EvaluationStack");
                context_pop.EvaluationStack.CopyTo(stack_eval);
            }
            if (InvocationStack.Count == 0)
                State = VMState.HALT;
            ContextUnloaded(context_pop);
            isJumping = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Syscall(Instruction instruction)
        {
            OnSysCall(instruction.TokenU32);
        }

        // Stack ops
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Depth(Instruction instruction)
        {
            Push(CurrentContext!.EvaluationStack.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Drop(Instruction instruction)
        {
            Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Nip(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Remove<StackItem>(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void XDrop(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            if (n < 0)
                throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
            CurrentContext!.EvaluationStack.Remove<StackItem>(n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Clear(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dup(Instruction instruction)
        {
            Push(Peek());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Over(Instruction instruction)
        {
            Push(Peek(1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Pick(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            if (n < 0)
                throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
            Push(Peek(n));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Tuck(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Insert(2, Peek());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(Instruction instruction)
        {
            var x = CurrentContext!.EvaluationStack.Remove<StackItem>(1);
            Push(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Rot(Instruction instruction)
        {
            var x = CurrentContext!.EvaluationStack.Remove<StackItem>(2);
            Push(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Roll(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            if (n < 0)
                throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
            if (n == 0) return;
            var x = CurrentContext!.EvaluationStack.Remove<StackItem>(n);
            Push(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reverse3(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Reverse(3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reverse4(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Reverse(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReverseN(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            CurrentContext!.EvaluationStack.Reverse(n);
        }

        //Slot
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitSSlot(Instruction instruction)
        {
            if (CurrentContext!.StaticFields != null)
                throw new InvalidOperationException($"{instruction.OpCode} cannot be executed twice.");
            if (instruction.TokenU8 == 0)
                throw new InvalidOperationException($"The operand {instruction.TokenU8} is invalid for OpCode.{instruction.OpCode}.");
            CurrentContext.StaticFields = new Slot(instruction.TokenU8, ReferenceCounter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitSlot(Instruction instruction)
        {
            if (CurrentContext!.LocalVariables != null || CurrentContext.Arguments != null)
                throw new InvalidOperationException($"{instruction.OpCode} cannot be executed twice.");
            if (instruction.TokenU16 == 0)
                throw new InvalidOperationException($"The operand {instruction.TokenU16} is invalid for OpCode.{instruction.OpCode}.");
            if (instruction.TokenU8 > 0)
            {
                CurrentContext.LocalVariables = new Slot(instruction.TokenU8, ReferenceCounter);
            }
            if (instruction.TokenU8_1 > 0)
            {
                StackItem[] items = new StackItem[instruction.TokenU8_1];
                for (int i = 0; i < instruction.TokenU8_1; i++)
                {
                    items[i] = Pop();
                }
                CurrentContext.Arguments = new Slot(items, ReferenceCounter);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LdSFldM(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.StaticFields, instruction.OpCode - OpCode.LDSFLD0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LdSFld(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.StaticFields, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StSFldM(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.StaticFields, instruction.OpCode - OpCode.STSFLD0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StSFld(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.StaticFields, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LdLocM(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.LocalVariables, instruction.OpCode - OpCode.LDLOC0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LdLoc(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StLocM(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.LocalVariables, instruction.OpCode - OpCode.STLOC0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StLoc(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LdArgM(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.Arguments, instruction.OpCode - OpCode.LDARG0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LdArg(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.Arguments, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StArgM(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.Arguments, instruction.OpCode - OpCode.STARG0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StArg(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.Arguments, instruction.TokenU8);
        }

        // Splice
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NewBuffer(Instruction instruction)
        {
            int length = (int)Pop().GetInteger();
            Limits.AssertMaxItemSize(length);
            Push(new Buffer(length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Memcpy(Instruction instruction)
        {
            int count = (int)Pop().GetInteger();
            if (count < 0)
                throw new InvalidOperationException($"The value {count} is out of range.");
            int si = (int)Pop().GetInteger();
            if (si < 0)
                throw new InvalidOperationException($"The value {si} is out of range.");
            ReadOnlySpan<byte> src = Pop().GetSpan();
            if (checked(si + count) > src.Length)
                throw new InvalidOperationException($"The value {count} is out of range.");
            int di = (int)Pop().GetInteger();
            if (di < 0)
                throw new InvalidOperationException($"The value {di} is out of range.");
            Buffer dst = Pop<Buffer>();
            if (checked(di + count) > dst.Size)
                throw new InvalidOperationException($"The value {count} is out of range.");
            src.Slice(si, count).CopyTo(dst.InnerBuffer.Span[di..]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Cat(Instruction instruction)
        {
            var x2 = Pop().GetSpan();
            var x1 = Pop().GetSpan();
            int length = x1.Length + x2.Length;
            Limits.AssertMaxItemSize(length);
            Buffer result = new(length, false);
            x1.CopyTo(result.InnerBuffer.Span);
            x2.CopyTo(result.InnerBuffer.Span[x1.Length..]);
            Push(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Substr(Instruction instruction)
        {
            int count = (int)Pop().GetInteger();
            if (count < 0)
                throw new InvalidOperationException($"The value {count} is out of range.");
            int index = (int)Pop().GetInteger();
            if (index < 0)
                throw new InvalidOperationException($"The value {index} is out of range.");
            var x = Pop().GetSpan();
            if (index + count > x.Length)
                throw new InvalidOperationException($"The value {count} is out of range.");
            Buffer result = new(count, false);
            x.Slice(index, count).CopyTo(result.InnerBuffer.Span);
            Push(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Left(Instruction instruction)
        {
            int count = (int)Pop().GetInteger();
            if (count < 0)
                throw new InvalidOperationException($"The value {count} is out of range.");
            var x = Pop().GetSpan();
            if (count > x.Length)
                throw new InvalidOperationException($"The value {count} is out of range.");
            Buffer result = new(count, false);
            x[..count].CopyTo(result.InnerBuffer.Span);
            Push(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Right(Instruction instruction)
        {
            int count = (int)Pop().GetInteger();
            if (count < 0)
                throw new InvalidOperationException($"The value {count} is out of range.");
            var x = Pop().GetSpan();
            if (count > x.Length)
                throw new InvalidOperationException($"The value {count} is out of range.");
            Buffer result = new(count, false);
            x[^count..^0].CopyTo(result.InnerBuffer.Span);
            Push(result);
        }

        // Bitwise logic
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Invert(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(~x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void And(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 & x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Or(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 | x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Xor(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 ^ x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Equal(Instruction instruction)
        {
            StackItem x2 = Pop();
            StackItem x1 = Pop();
            Push(x1.Equals(x2, Limits));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotEqual(Instruction instruction)
        {
            StackItem x2 = Pop();
            StackItem x1 = Pop();
            Push(!x1.Equals(x2, Limits));
        }

        // Numeric
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Sign(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(x.Sign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Abs(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(BigInteger.Abs(x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Negate(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(-x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Inc(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(x + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dec(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(x - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 + x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Sub(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 - x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Mul(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 * x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Div(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 / x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Mod(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 % x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Pow(Instruction instruction)
        {
            var exponent = (int)Pop().GetInteger();
            Limits.AssertShift(exponent);
            var value = Pop().GetInteger();
            Push(BigInteger.Pow(value, exponent));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Sqrt(Instruction instruction)
        {
            Push(Pop().GetInteger().Sqrt());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ModMul(Instruction instruction)
        {
            var modulus = Pop().GetInteger();
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 * x2 % modulus);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ModPow(Instruction instruction)
        {
            var modulus = Pop().GetInteger();
            var exponent = Pop().GetInteger();
            var value = Pop().GetInteger();
            var result = exponent == -1
                ? value.ModInverse(modulus)
                : BigInteger.ModPow(value, exponent, modulus);
            Push(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Shl(Instruction instruction)
        {
            int shift = (int)Pop().GetInteger();
            Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = Pop().GetInteger();
            Push(x << shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Shr(Instruction instruction)
        {
            int shift = (int)Pop().GetInteger();
            Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = Pop().GetInteger();
            Push(x >> shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Not(Instruction instruction)
        {
            var x = Pop().GetBoolean();
            Push(!x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BoolAnd(Instruction instruction)
        {
            var x2 = Pop().GetBoolean();
            var x1 = Pop().GetBoolean();
            Push(x1 && x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BoolOr(Instruction instruction)
        {
            var x2 = Pop().GetBoolean();
            var x1 = Pop().GetBoolean();
            Push(x1 || x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Nz(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(!x.IsZero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NumEqual(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 == x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NumNotEqual(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 != x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Lt(Instruction instruction)
        {
            var x2 = Pop();
            var x1 = Pop();
            if (x1.IsNull || x2.IsNull)
                Push(false);
            else
                Push(x1.GetInteger() < x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Le(Instruction instruction)
        {
            var x2 = Pop();
            var x1 = Pop();
            if (x1.IsNull || x2.IsNull)
                Push(false);
            else
                Push(x1.GetInteger() <= x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Gt(Instruction instruction)
        {
            var x2 = Pop();
            var x1 = Pop();
            if (x1.IsNull || x2.IsNull)
                Push(false);
            else
                Push(x1.GetInteger() > x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Ge(Instruction instruction)
        {
            var x2 = Pop();
            var x1 = Pop();
            if (x1.IsNull || x2.IsNull)
                Push(false);
            else
                Push(x1.GetInteger() >= x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Min(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(BigInteger.Min(x1, x2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Max(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(BigInteger.Max(x1, x2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Within(Instruction instruction)
        {
            BigInteger b = Pop().GetInteger();
            BigInteger a = Pop().GetInteger();
            var x = Pop().GetInteger();
            Push(a <= x && x < b);
        }

        // Compound-type
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PackMap(Instruction instruction)
        {
            int size = (int)Pop().GetInteger();
            if (size < 0 || size * 2 > CurrentContext!.EvaluationStack.Count)
                throw new InvalidOperationException($"The value {size} is out of range.");
            Map map = new(ReferenceCounter);
            for (int i = 0; i < size; i++)
            {
                PrimitiveType key = Pop<PrimitiveType>();
                StackItem value = Pop();
                map[key] = value;
            }
            Push(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PackStruct(Instruction instruction)
        {
            int size = (int)Pop().GetInteger();
            if (size < 0 || size > CurrentContext!.EvaluationStack.Count)
                throw new InvalidOperationException($"The value {size} is out of range.");
            Struct @struct = new(ReferenceCounter);
            for (int i = 0; i < size; i++)
            {
                StackItem item = Pop();
                @struct.Add(item);
            }
            Push(@struct);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Pack(Instruction instruction)
        {
            int size = (int)Pop().GetInteger();
            if (size < 0 || size > CurrentContext!.EvaluationStack.Count)
                throw new InvalidOperationException($"The value {size} is out of range.");
            VMArray array = new(ReferenceCounter);
            for (int i = 0; i < size; i++)
            {
                StackItem item = Pop();
                array.Add(item);
            }
            Push(array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Unpack(Instruction instruction)
        {
            CompoundType compound = Pop<CompoundType>();
            switch (compound)
            {
                case Map map:
                    foreach (var (key, value) in map.Reverse())
                    {
                        Push(value);
                        Push(key);
                    }
                    break;
                case VMArray array:
                    for (int i = array.Count - 1; i >= 0; i--)
                    {
                        Push(array[i]);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {compound.Type}");
            }
            Push(compound.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NewArray0(Instruction instruction)
        {
            Push(new VMArray(ReferenceCounter));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NewArray_T(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            if (n < 0 || n > Limits.MaxStackSize)
                throw new InvalidOperationException($"MaxStackSize exceed: {n}");
            StackItem item;
            if (instruction.OpCode == OpCode.NEWARRAY_T)
            {
                StackItemType type = (StackItemType)instruction.TokenU8;
                if (!Enum.IsDefined(typeof(StackItemType), type))
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {instruction.TokenU8}");
                item = instruction.TokenU8 switch
                {
                    (byte)StackItemType.Boolean => StackItem.False,
                    (byte)StackItemType.Integer => Integer.Zero,
                    (byte)StackItemType.ByteString => ByteString.Empty,
                    _ => StackItem.Null
                };
            }
            else
            {
                item = StackItem.Null;
            }
            Push(new VMArray(ReferenceCounter, Enumerable.Repeat(item, n)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NewStruct0(Instruction instruction)
        {
            Push(new Struct(ReferenceCounter));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NewStruct(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            if (n < 0 || n > Limits.MaxStackSize)
                throw new InvalidOperationException($"MaxStackSize exceed: {n}");
            Struct result = new(ReferenceCounter);
            for (var i = 0; i < n; i++)
                result.Add(StackItem.Null);
            Push(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NewMap(Instruction instruction)
        {
            Push(new Map(ReferenceCounter));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Size(Instruction instruction)
        {
            var x = Pop();
            switch (x)
            {
                case CompoundType compound:
                    Push(compound.Count);
                    return;
                case PrimitiveType primitive:
                    Push(primitive.Size);
                    return;
                case Buffer buffer:
                    Push(buffer.Size);
                    return;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HasKey(Instruction instruction)
        {
            PrimitiveType key = Pop<PrimitiveType>();
            var x = Pop();
            switch (x)
            {
                case VMArray array:
                    {
                        int index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
                        Push(index < array.Count);
                        return;
                    }
                case Map map:
                    {
                        Push(map.ContainsKey(key));
                        return;
                    }
                case Buffer buffer:
                    {
                        int index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
                        Push(index < buffer.Size);
                        return;
                    }
                case ByteString array:
                    {
                        int index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
                        Push(index < array.Size);
                        return;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}(Instruction instruction) {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Keys(Instruction instruction)
        {
            Map map = Pop<Map>();
            Push(new VMArray(ReferenceCounter, map.Keys));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Values(Instruction instruction)
        {
            var x = Pop();
            IEnumerable<StackItem> values = x switch
            {
                VMArray array => array,
                Map map => map.Values,
                _ => throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}"),
            };
            VMArray newArray = new(ReferenceCounter);
            foreach (StackItem item in values)
                if (item is Struct s)
                    newArray.Add(s.Clone(Limits));
                else
                    newArray.Add(item);
            Push(newArray);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PickItem(Instruction instruction)
        {
            PrimitiveType key = Pop<PrimitiveType>();
            var x = Pop();
            switch (x)
            {
                case VMArray array:
                    {
                        int index = (int)key.GetInteger();
                        if (index < 0 || index >= array.Count)
                            throw new CatchableException($"The value {index} is out of range.");
                        Push(array[index]);
                        return;
                    }
                case Map map:
                    {
                        if (!map.TryGetValue(key, out StackItem? value))
                            throw new CatchableException($"Key not found in {nameof(Map)}");
                        Push(value);
                        return;
                    }
                case PrimitiveType primitive:
                    {
                        ReadOnlySpan<byte> byteArray = primitive.GetSpan();
                        int index = (int)key.GetInteger();
                        if (index < 0 || index >= byteArray.Length)
                            throw new CatchableException($"The value {index} is out of range.");
                        Push((BigInteger)byteArray[index]);
                        return;
                    }
                case Buffer buffer:
                    {
                        int index = (int)key.GetInteger();
                        if (index < 0 || index >= buffer.Size)
                            throw new CatchableException($"The value {index} is out of range.");
                        Push((BigInteger)buffer.InnerBuffer.Span[index]);
                        return;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Append(Instruction instruction)
        {
            StackItem newItem = Pop();
            VMArray array = Pop<VMArray>();
            if (newItem is Struct s) newItem = s.Clone(Limits);
            array.Add(newItem);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetItem(Instruction instruction)
        {
            StackItem value = Pop();
            if (value is Struct s) value = s.Clone(Limits);
            PrimitiveType key = Pop<PrimitiveType>();
            var x = Pop();
            switch (x)
            {
                case VMArray array:
                    {
                        int index = (int)key.GetInteger();
                        if (index < 0 || index >= array.Count)
                            throw new CatchableException($"The value {index} is out of range.");
                        array[index] = value;
                        return;
                    }
                case Map map:
                    {
                        map[key] = value;
                        return;
                    }
                case Buffer buffer:
                    {
                        int index = (int)key.GetInteger();
                        if (index < 0 || index >= buffer.Size)
                            throw new CatchableException($"The value {index} is out of range.");
                        if (value is not PrimitiveType p)
                            throw new InvalidOperationException($"Value must be a primitive type in {instruction.OpCode}");
                        int b = (int)p.GetInteger();
                        if (b < sbyte.MinValue || b > byte.MaxValue)
                            throw new InvalidOperationException($"Overflow in {instruction.OpCode}, {b} is not a byte type.");
                        buffer.InnerBuffer.Span[index] = (byte)b;
                        return;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReverseItems(Instruction instruction)
        {
            var x = Pop();
            switch (x)
            {
                case VMArray array:
                    array.Reverse();
                    return;
                case Buffer buffer:
                    buffer.InnerBuffer.Span.Reverse();
                    return;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Remove(Instruction instruction)
        {
            PrimitiveType key = Pop<PrimitiveType>();
            var x = Pop();
            switch (x)
            {
                case VMArray array:
                    int index = (int)key.GetInteger();
                    if (index < 0 || index >= array.Count)
                        throw new InvalidOperationException($"The value {index} is out of range.");
                    array.RemoveAt(index);
                    return;
                case Map map:
                    map.Remove(key);
                    return;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearItems(Instruction instruction)
        {
            CompoundType x = Pop<CompoundType>();
            x.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PopItem(Instruction instruction)
        {
            VMArray x = Pop<VMArray>();
            int index = x.Count - 1;
            Push(x[index]);
            x.RemoveAt(index);
        }

        //Types
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IsNull(Instruction instruction)
        {
            var x = Pop();
            Push(x.IsNull);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IsType(Instruction instruction)
        {
            var x = Pop();
            StackItemType type = (StackItemType)instruction.TokenU8;
            if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                throw new InvalidOperationException($"Invalid type: {type}");
            Push(x.Type == type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Convert(Instruction instruction)
        {
            var x = Pop();
            Push(x.ConvertTo((StackItemType)instruction.TokenU8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AbortMsg(Instruction instruction)
        {
            var msg = Pop().GetString();
            throw new Exception($"{OpCode.ABORTMSG} is executed. Reason: {msg}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertMsg(Instruction instruction)
        {
            var msg = Pop().GetString();
            var x = Pop().GetBoolean();
            if (!x)
                throw new Exception($"{OpCode.ASSERTMSG} is executed with false result. Reason: {msg}");
        }
    }
}
