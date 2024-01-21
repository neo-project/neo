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
        private void PUSHINT(Instruction instruction)
        {
            Push(new BigInteger(instruction.Operand.Span));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PUSHT(Instruction instruction)
        {
            Push(StackItem.True);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PUSHF(Instruction instruction)
        {
            Push(StackItem.False);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PUSHA(Instruction instruction)
        {
            int position = checked(CurrentContext!.InstructionPointer + instruction.TokenI32);
            if (position < 0 || position > CurrentContext.Script.Length)
                throw new InvalidOperationException($"Bad pointer address(Instruction instruction) {position}");
            Push(new Pointer(CurrentContext.Script, position));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PUSHNULL(Instruction instruction)
        {
            Push(StackItem.Null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PUSHDATA(Instruction instruction)
        {
            Limits.AssertMaxItemSize(instruction.Operand.Length);
            Push(instruction.Operand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PUSH(Instruction instruction)
        {
            Push((int)instruction.OpCode - (int)OpCode.PUSH0);
        }

        // Control
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMP(Instruction instruction)
        {
            ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMP_L(Instruction instruction)
        {
            ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPIF(Instruction instruction)
        {
            if (Pop().GetBoolean())
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPIF_L(Instruction instruction)
        {
            if (Pop().GetBoolean())
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPIFNOT(Instruction instruction)
        {
            if (!Pop().GetBoolean())
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPIFNOT_L(Instruction instruction)
        {
            if (!Pop().GetBoolean())
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPEQ(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 == x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPEQ_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 == x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPNE(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 != x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPNE_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 != x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPGT(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 > x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPGT_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 > x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPGE(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 >= x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPGE_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 >= x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPLT(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 < x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPLT_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 < x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPLE(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 <= x2)
                ExecuteJumpOffset(instruction.TokenI8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void JMPLE_L(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            if (x1 <= x2)
                ExecuteJumpOffset(instruction.TokenI32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CALL(Instruction instruction)
        {
            ExecuteCall(checked(CurrentContext!.InstructionPointer + instruction.TokenI8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CALL_L(Instruction instruction)
        {
            ExecuteCall(checked(CurrentContext!.InstructionPointer + instruction.TokenI32));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CALLA(Instruction instruction)
        {
            var x = Pop<Pointer>();
            if (x.Script != CurrentContext!.Script)
                throw new InvalidOperationException("Pointers can't be shared between scripts");
            ExecuteCall(x.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CALLT(Instruction instruction)
        {
            LoadToken(instruction.TokenU16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ABORT(Instruction instruction)
        {
            throw new Exception($"{OpCode.ABORT} is executed.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ASSERT(Instruction instruction)
        {
            var x = Pop().GetBoolean();
            if (!x)
                throw new Exception($"{OpCode.ASSERT} is executed with false result.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void THROW(Instruction instruction)
        {
            ExecuteThrow(Pop());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TRY(Instruction instruction)
        {
            int catchOffset = instruction.TokenI8;
            int finallyOffset = instruction.TokenI8_1;
            ExecuteTry(catchOffset, finallyOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TRY_L(Instruction instruction)
        {
            int catchOffset = instruction.TokenI32;
            int finallyOffset = instruction.TokenI32_1;
            ExecuteTry(catchOffset, finallyOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ENDTRY(Instruction instruction)
        {
            int endOffset = instruction.TokenI8;
            ExecuteEndTry(endOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ENDTRY_L(Instruction instruction)
        {
            int endOffset = instruction.TokenI32;
            ExecuteEndTry(endOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ENDFINALLY(Instruction instruction)
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
        private void RET(Instruction instruction)
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
        private void SYSCALL(Instruction instruction)
        {
            OnSysCall(instruction.TokenU32);
        }

        // Stack ops
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DEPTH(Instruction instruction)
        {
            Push(CurrentContext!.EvaluationStack.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DROP(Instruction instruction)
        {
            Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NIP(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Remove<StackItem>(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void XDROP(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            if (n < 0)
                throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
            CurrentContext!.EvaluationStack.Remove<StackItem>(n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CLEAR(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DUP(Instruction instruction)
        {
            Push(Peek());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OVER(Instruction instruction)
        {
            Push(Peek(1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PICK(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            if (n < 0)
                throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
            Push(Peek(n));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TUCK(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Insert(2, Peek());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SWAP(Instruction instruction)
        {
            var x = CurrentContext!.EvaluationStack.Remove<StackItem>(1);
            Push(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ROT(Instruction instruction)
        {
            var x = CurrentContext!.EvaluationStack.Remove<StackItem>(2);
            Push(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ROLL(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            if (n < 0)
                throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
            if (n == 0) return;
            var x = CurrentContext!.EvaluationStack.Remove<StackItem>(n);
            Push(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void REVERSE3(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Reverse(3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void REVERSE4(Instruction instruction)
        {
            CurrentContext!.EvaluationStack.Reverse(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void REVERSEN(Instruction instruction)
        {
            int n = (int)Pop().GetInteger();
            CurrentContext!.EvaluationStack.Reverse(n);
        }

        //Slot
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void INITSSLOT(Instruction instruction)
        {
            if (CurrentContext!.StaticFields != null)
                throw new InvalidOperationException($"{instruction.OpCode} cannot be executed twice.");
            if (instruction.TokenU8 == 0)
                throw new InvalidOperationException($"The operand {instruction.TokenU8} is invalid for OpCode.{instruction.OpCode}.");
            CurrentContext.StaticFields = new Slot(instruction.TokenU8, ReferenceCounter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void INITSLOT(Instruction instruction)
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
        private void LDSFLDM(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.StaticFields, instruction.OpCode - OpCode.LDSFLD0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LDSFLD(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.StaticFields, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void STSFLDM(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.StaticFields, instruction.OpCode - OpCode.STSFLD0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void STSFLD(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.StaticFields, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LDLOCM(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.LocalVariables, instruction.OpCode - OpCode.LDLOC0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LDLOC(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void STLOCM(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.LocalVariables, instruction.OpCode - OpCode.STLOC0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void STLOC(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.LocalVariables, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LDARGM(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.Arguments, instruction.OpCode - OpCode.LDARG0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LDARG(Instruction instruction)
        {
            ExecuteLoadFromSlot(CurrentContext!.Arguments, instruction.TokenU8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void STARGM(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.Arguments, instruction.OpCode - OpCode.STARG0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void STARG(Instruction instruction)
        {
            ExecuteStoreToSlot(CurrentContext!.Arguments, instruction.TokenU8);
        }

        // Splice
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NEWBUFFER(Instruction instruction)
        {
            int length = (int)Pop().GetInteger();
            Limits.AssertMaxItemSize(length);
            Push(new Buffer(length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MEMCPY(Instruction instruction)
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
        private void CAT(Instruction instruction)
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
        private void SUBSTR(Instruction instruction)
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
        private void LEFT(Instruction instruction)
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
        private void RIGHT(Instruction instruction)
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
        private void INVERT(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(~x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AND(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 & x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OR(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 | x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void XOR(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 ^ x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EQUAL(Instruction instruction)
        {
            StackItem x2 = Pop();
            StackItem x1 = Pop();
            Push(x1.Equals(x2, Limits));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NOTEQUAL(Instruction instruction)
        {
            StackItem x2 = Pop();
            StackItem x1 = Pop();
            Push(!x1.Equals(x2, Limits));
        }

        // Numeric
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SIGN(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(x.Sign);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ABS(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(BigInteger.Abs(x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NEGATE(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(-x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void INC(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(x + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DEC(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(x - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ADD(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 + x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SUB(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 - x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MUL(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 * x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DIV(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 / x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MOD(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 % x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void POW(Instruction instruction)
        {
            var exponent = (int)Pop().GetInteger();
            Limits.AssertShift(exponent);
            var value = Pop().GetInteger();
            Push(BigInteger.Pow(value, exponent));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SQRT(Instruction instruction)
        {
            Push(Pop().GetInteger().Sqrt());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MODMUL(Instruction instruction)
        {
            var modulus = Pop().GetInteger();
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 * x2 % modulus);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MODPOW(Instruction instruction)
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
        private void SHL(Instruction instruction)
        {
            int shift = (int)Pop().GetInteger();
            Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = Pop().GetInteger();
            Push(x << shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SHR(Instruction instruction)
        {
            int shift = (int)Pop().GetInteger();
            Limits.AssertShift(shift);
            if (shift == 0) return;
            var x = Pop().GetInteger();
            Push(x >> shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NOT(Instruction instruction)
        {
            var x = Pop().GetBoolean();
            Push(!x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BOOLAND(Instruction instruction)
        {
            var x2 = Pop().GetBoolean();
            var x1 = Pop().GetBoolean();
            Push(x1 && x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BOOLOR(Instruction instruction)
        {
            var x2 = Pop().GetBoolean();
            var x1 = Pop().GetBoolean();
            Push(x1 || x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NZ(Instruction instruction)
        {
            var x = Pop().GetInteger();
            Push(!x.IsZero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NUMEQUAL(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 == x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NUMNOTEQUAL(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(x1 != x2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LT(Instruction instruction)
        {
            var x2 = Pop();
            var x1 = Pop();
            if (x1.IsNull || x2.IsNull)
                Push(false);
            else
                Push(x1.GetInteger() < x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LE(Instruction instruction)
        {
            var x2 = Pop();
            var x1 = Pop();
            if (x1.IsNull || x2.IsNull)
                Push(false);
            else
                Push(x1.GetInteger() <= x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GT(Instruction instruction)
        {
            var x2 = Pop();
            var x1 = Pop();
            if (x1.IsNull || x2.IsNull)
                Push(false);
            else
                Push(x1.GetInteger() > x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GE(Instruction instruction)
        {
            var x2 = Pop();
            var x1 = Pop();
            if (x1.IsNull || x2.IsNull)
                Push(false);
            else
                Push(x1.GetInteger() >= x2.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MIN(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(BigInteger.Min(x1, x2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MAX(Instruction instruction)
        {
            var x2 = Pop().GetInteger();
            var x1 = Pop().GetInteger();
            Push(BigInteger.Max(x1, x2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WITHIN(Instruction instruction)
        {
            BigInteger b = Pop().GetInteger();
            BigInteger a = Pop().GetInteger();
            var x = Pop().GetInteger();
            Push(a <= x && x < b);
        }

        // Compound-type
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PACKMAP(Instruction instruction)
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
        private void PACKSTRUCT(Instruction instruction)
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
        private void PACK(Instruction instruction)
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
        private void UNPACK(Instruction instruction)
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
        private void NEWARRAY0(Instruction instruction)
        {
            Push(new VMArray(ReferenceCounter));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NEWARRAY_T(Instruction instruction)
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
        private void NEWSTRUCT0(Instruction instruction)
        {
            Push(new Struct(ReferenceCounter));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NEWSTRUCT(Instruction instruction)
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
        private void NEWMAP(Instruction instruction)
        {
            Push(new Map(ReferenceCounter));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SIZE(Instruction instruction)
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
        private void HASKEY(Instruction instruction)
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
        private void KEYS(Instruction instruction)
        {
            Map map = Pop<Map>();
            Push(new VMArray(ReferenceCounter, map.Keys));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void VALUES(Instruction instruction)
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
        private void PICKITEM(Instruction instruction)
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
        private void APPEND(Instruction instruction)
        {
            StackItem newItem = Pop();
            VMArray array = Pop<VMArray>();
            if (newItem is Struct s) newItem = s.Clone(Limits);
            array.Add(newItem);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SETITEM(Instruction instruction)
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
        private void REVERSEITEMS(Instruction instruction)
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
        private void REMOVE(Instruction instruction)
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
        private void CLEARITEMS(Instruction instruction)
        {
            CompoundType x = Pop<CompoundType>();
            x.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void POPITEM(Instruction instruction)
        {
            VMArray x = Pop<VMArray>();
            int index = x.Count - 1;
            Push(x[index]);
            x.RemoveAt(index);
        }

        //Types
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ISNULL(Instruction instruction)
        {
            var x = Pop();
            Push(x.IsNull);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ISTYPE(Instruction instruction)
        {
            var x = Pop();
            StackItemType type = (StackItemType)instruction.TokenU8;
            if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                throw new InvalidOperationException($"Invalid type: {type}");
            Push(x.Type == type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CONVERT(Instruction instruction)
        {
            var x = Pop();
            Push(x.ConvertTo((StackItemType)instruction.TokenU8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ABORTMSG(Instruction instruction)
        {
            var msg = Pop().GetString();
            throw new Exception($"{OpCode.ABORTMSG} is executed. Reason: {msg}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ASSERTMSG(Instruction instruction)
        {
            var msg = Pop().GetString();
            var x = Pop().GetBoolean();
            if (!x)
                throw new Exception($"{OpCode.ASSERTMSG} is executed with false result. Reason: {msg}");
        }
    }
}
