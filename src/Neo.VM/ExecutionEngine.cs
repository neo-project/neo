// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Neo.VM.Types;
using Buffer = Neo.VM.Types.Buffer;
using VMArray = Neo.VM.Types.Array;

namespace Neo.VM
{
    /// <summary>
    /// Represents the VM used to execute the script.
    /// </summary>
    public class ExecutionEngine : IDisposable
    {
        private VMState state = VMState.BREAK;
        private bool isJumping = false;

        /// <summary>
        /// Restrictions on the VM.
        /// </summary>
        public ExecutionEngineLimits Limits { get; }

        /// <summary>
        /// Used for reference counting of objects in the VM.
        /// </summary>
        public ReferenceCounter ReferenceCounter { get; }

        /// <summary>
        /// The invocation stack of the VM.
        /// </summary>
        public Stack<ExecutionContext> InvocationStack { get; } = new Stack<ExecutionContext>();

        /// <summary>
        /// The top frame of the invocation stack.
        /// </summary>
        public ExecutionContext? CurrentContext { get; private set; }

        /// <summary>
        /// The bottom frame of the invocation stack.
        /// </summary>
        public ExecutionContext? EntryContext { get; private set; }

        /// <summary>
        /// The stack to store the return values.
        /// </summary>
        public EvaluationStack ResultStack { get; }

        /// <summary>
        /// The VM object representing the uncaught exception.
        /// </summary>
        public StackItem? UncaughtException { get; private set; }

        /// <summary>
        /// The current state of the VM.
        /// </summary>
        public VMState State
        {
            get
            {
                return state;
            }
            internal protected set
            {
                if (state != value)
                {
                    state = value;
                    OnStateChanged();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionEngine"/> class.
        /// </summary>
        public ExecutionEngine() : this(new ReferenceCounter(), ExecutionEngineLimits.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionEngine"/> class with the specified <see cref="VM.ReferenceCounter"/> and <see cref="ExecutionEngineLimits"/>.
        /// </summary>
        /// <param name="referenceCounter">The reference counter to be used.</param>
        /// <param name="limits">Restrictions on the VM.</param>
        protected ExecutionEngine(ReferenceCounter referenceCounter, ExecutionEngineLimits limits)
        {
            this.Limits = limits;
            this.ReferenceCounter = referenceCounter;
            this.ResultStack = new EvaluationStack(referenceCounter);
        }

        /// <summary>
        /// Called when a context is unloaded.
        /// </summary>
        /// <param name="context">The context being unloaded.</param>
        protected virtual void ContextUnloaded(ExecutionContext context)
        {
            if (InvocationStack.Count == 0)
            {
                CurrentContext = null;
                EntryContext = null;
            }
            else
            {
                CurrentContext = InvocationStack.Peek();
            }
            if (context.StaticFields != null && context.StaticFields != CurrentContext?.StaticFields)
            {
                context.StaticFields.ClearReferences();
            }
            context.LocalVariables?.ClearReferences();
            context.Arguments?.ClearReferences();
        }

        public virtual void Dispose()
        {
            InvocationStack.Clear();
        }

        /// <summary>
        /// Start execution of the VM.
        /// </summary>
        /// <returns></returns>
        public virtual VMState Execute()
        {
            if (State == VMState.BREAK)
                State = VMState.NONE;
            while (State != VMState.HALT && State != VMState.FAULT)
                ExecuteNext();
            return State;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteCall(int position)
        {
            LoadContext(CurrentContext!.Clone(position));
        }

        private void ExecuteInstruction(Instruction instruction)
        {
            switch (instruction.OpCode)
            {
                //Push
                case OpCode.PUSHINT8:
                case OpCode.PUSHINT16:
                case OpCode.PUSHINT32:
                case OpCode.PUSHINT64:
                case OpCode.PUSHINT128:
                case OpCode.PUSHINT256:
                    {
                        Push(new BigInteger(instruction.Operand.Span));
                        break;
                    }
                case OpCode.PUSHT:
                    {
                        Push(StackItem.True);
                        break;
                    }
                case OpCode.PUSHF:
                    {
                        Push(StackItem.False);
                        break;
                    }
                case OpCode.PUSHA:
                    {
                        int position = checked(CurrentContext!.InstructionPointer + instruction.TokenI32);
                        if (position < 0 || position > CurrentContext.Script.Length)
                            throw new InvalidOperationException($"Bad pointer address: {position}");
                        Push(new Pointer(CurrentContext.Script, position));
                        break;
                    }
                case OpCode.PUSHNULL:
                    {
                        Push(StackItem.Null);
                        break;
                    }
                case OpCode.PUSHDATA1:
                case OpCode.PUSHDATA2:
                case OpCode.PUSHDATA4:
                    {
                        Limits.AssertMaxItemSize(instruction.Operand.Length);
                        Push(instruction.Operand);
                        break;
                    }
                case OpCode.PUSHM1:
                case OpCode.PUSH0:
                case OpCode.PUSH1:
                case OpCode.PUSH2:
                case OpCode.PUSH3:
                case OpCode.PUSH4:
                case OpCode.PUSH5:
                case OpCode.PUSH6:
                case OpCode.PUSH7:
                case OpCode.PUSH8:
                case OpCode.PUSH9:
                case OpCode.PUSH10:
                case OpCode.PUSH11:
                case OpCode.PUSH12:
                case OpCode.PUSH13:
                case OpCode.PUSH14:
                case OpCode.PUSH15:
                case OpCode.PUSH16:
                    {
                        Push((int)instruction.OpCode - (int)OpCode.PUSH0);
                        break;
                    }

                // Control
                case OpCode.NOP: break;
                case OpCode.JMP:
                    {
                        ExecuteJumpOffset(instruction.TokenI8);
                        break;
                    }
                case OpCode.JMP_L:
                    {
                        ExecuteJumpOffset(instruction.TokenI32);
                        break;
                    }
                case OpCode.JMPIF:
                    {
                        if (Pop().GetBoolean())
                            ExecuteJumpOffset(instruction.TokenI8);
                        break;
                    }
                case OpCode.JMPIF_L:
                    {
                        if (Pop().GetBoolean())
                            ExecuteJumpOffset(instruction.TokenI32);
                        break;
                    }
                case OpCode.JMPIFNOT:
                    {
                        if (!Pop().GetBoolean())
                            ExecuteJumpOffset(instruction.TokenI8);
                        break;
                    }
                case OpCode.JMPIFNOT_L:
                    {
                        if (!Pop().GetBoolean())
                            ExecuteJumpOffset(instruction.TokenI32);
                        break;
                    }
                case OpCode.JMPEQ:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 == x2)
                            ExecuteJumpOffset(instruction.TokenI8);
                        break;
                    }
                case OpCode.JMPEQ_L:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 == x2)
                            ExecuteJumpOffset(instruction.TokenI32);
                        break;
                    }
                case OpCode.JMPNE:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 != x2)
                            ExecuteJumpOffset(instruction.TokenI8);
                        break;
                    }
                case OpCode.JMPNE_L:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 != x2)
                            ExecuteJumpOffset(instruction.TokenI32);
                        break;
                    }
                case OpCode.JMPGT:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 > x2)
                            ExecuteJumpOffset(instruction.TokenI8);
                        break;
                    }
                case OpCode.JMPGT_L:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 > x2)
                            ExecuteJumpOffset(instruction.TokenI32);
                        break;
                    }
                case OpCode.JMPGE:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 >= x2)
                            ExecuteJumpOffset(instruction.TokenI8);
                        break;
                    }
                case OpCode.JMPGE_L:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 >= x2)
                            ExecuteJumpOffset(instruction.TokenI32);
                        break;
                    }
                case OpCode.JMPLT:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 < x2)
                            ExecuteJumpOffset(instruction.TokenI8);
                        break;
                    }
                case OpCode.JMPLT_L:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 < x2)
                            ExecuteJumpOffset(instruction.TokenI32);
                        break;
                    }
                case OpCode.JMPLE:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 <= x2)
                            ExecuteJumpOffset(instruction.TokenI8);
                        break;
                    }
                case OpCode.JMPLE_L:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        if (x1 <= x2)
                            ExecuteJumpOffset(instruction.TokenI32);
                        break;
                    }
                case OpCode.CALL:
                    {
                        ExecuteCall(checked(CurrentContext!.InstructionPointer + instruction.TokenI8));
                        break;
                    }
                case OpCode.CALL_L:
                    {
                        ExecuteCall(checked(CurrentContext!.InstructionPointer + instruction.TokenI32));
                        break;
                    }
                case OpCode.CALLA:
                    {
                        var x = Pop<Pointer>();
                        if (x.Script != CurrentContext!.Script)
                            throw new InvalidOperationException("Pointers can't be shared between scripts");
                        ExecuteCall(x.Position);
                        break;
                    }
                case OpCode.CALLT:
                    {
                        LoadToken(instruction.TokenU16);
                        break;
                    }
                case OpCode.ABORT:
                    {
                        throw new Exception($"{OpCode.ABORT} is executed.");
                    }
                case OpCode.ASSERT:
                    {
                        var x = Pop().GetBoolean();
                        if (!x)
                            throw new Exception($"{OpCode.ASSERT} is executed with false result.");
                        break;
                    }
                case OpCode.THROW:
                    {
                        ExecuteThrow(Pop());
                        break;
                    }
                case OpCode.TRY:
                    {
                        int catchOffset = instruction.TokenI8;
                        int finallyOffset = instruction.TokenI8_1;
                        ExecuteTry(catchOffset, finallyOffset);
                        break;
                    }
                case OpCode.TRY_L:
                    {
                        int catchOffset = instruction.TokenI32;
                        int finallyOffset = instruction.TokenI32_1;
                        ExecuteTry(catchOffset, finallyOffset);
                        break;
                    }
                case OpCode.ENDTRY:
                    {
                        int endOffset = instruction.TokenI8;
                        ExecuteEndTry(endOffset);
                        break;
                    }
                case OpCode.ENDTRY_L:
                    {
                        int endOffset = instruction.TokenI32;
                        ExecuteEndTry(endOffset);
                        break;
                    }
                case OpCode.ENDFINALLY:
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
                        break;
                    }
                case OpCode.RET:
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
                        break;
                    }
                case OpCode.SYSCALL:
                    {
                        OnSysCall(instruction.TokenU32);
                        break;
                    }

                // Stack ops
                case OpCode.DEPTH:
                    {
                        Push(CurrentContext!.EvaluationStack.Count);
                        break;
                    }
                case OpCode.DROP:
                    {
                        Pop();
                        break;
                    }
                case OpCode.NIP:
                    {
                        CurrentContext!.EvaluationStack.Remove<StackItem>(1);
                        break;
                    }
                case OpCode.XDROP:
                    {
                        int n = (int)Pop().GetInteger();
                        if (n < 0)
                            throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
                        CurrentContext!.EvaluationStack.Remove<StackItem>(n);
                        break;
                    }
                case OpCode.CLEAR:
                    {
                        CurrentContext!.EvaluationStack.Clear();
                        break;
                    }
                case OpCode.DUP:
                    {
                        Push(Peek());
                        break;
                    }
                case OpCode.OVER:
                    {
                        Push(Peek(1));
                        break;
                    }
                case OpCode.PICK:
                    {
                        int n = (int)Pop().GetInteger();
                        if (n < 0)
                            throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
                        Push(Peek(n));
                        break;
                    }
                case OpCode.TUCK:
                    {
                        CurrentContext!.EvaluationStack.Insert(2, Peek());
                        break;
                    }
                case OpCode.SWAP:
                    {
                        var x = CurrentContext!.EvaluationStack.Remove<StackItem>(1);
                        Push(x);
                        break;
                    }
                case OpCode.ROT:
                    {
                        var x = CurrentContext!.EvaluationStack.Remove<StackItem>(2);
                        Push(x);
                        break;
                    }
                case OpCode.ROLL:
                    {
                        int n = (int)Pop().GetInteger();
                        if (n < 0)
                            throw new InvalidOperationException($"The negative value {n} is invalid for OpCode.{instruction.OpCode}.");
                        if (n == 0) break;
                        var x = CurrentContext!.EvaluationStack.Remove<StackItem>(n);
                        Push(x);
                        break;
                    }
                case OpCode.REVERSE3:
                    {
                        CurrentContext!.EvaluationStack.Reverse(3);
                        break;
                    }
                case OpCode.REVERSE4:
                    {
                        CurrentContext!.EvaluationStack.Reverse(4);
                        break;
                    }
                case OpCode.REVERSEN:
                    {
                        int n = (int)Pop().GetInteger();
                        CurrentContext!.EvaluationStack.Reverse(n);
                        break;
                    }

                //Slot
                case OpCode.INITSSLOT:
                    {
                        if (CurrentContext!.StaticFields != null)
                            throw new InvalidOperationException($"{instruction.OpCode} cannot be executed twice.");
                        if (instruction.TokenU8 == 0)
                            throw new InvalidOperationException($"The operand {instruction.TokenU8} is invalid for OpCode.{instruction.OpCode}.");
                        CurrentContext.StaticFields = new Slot(instruction.TokenU8, ReferenceCounter);
                        break;
                    }
                case OpCode.INITSLOT:
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
                        break;
                    }
                case OpCode.LDSFLD0:
                case OpCode.LDSFLD1:
                case OpCode.LDSFLD2:
                case OpCode.LDSFLD3:
                case OpCode.LDSFLD4:
                case OpCode.LDSFLD5:
                case OpCode.LDSFLD6:
                    {
                        ExecuteLoadFromSlot(CurrentContext!.StaticFields, instruction.OpCode - OpCode.LDSFLD0);
                        break;
                    }
                case OpCode.LDSFLD:
                    {
                        ExecuteLoadFromSlot(CurrentContext!.StaticFields, instruction.TokenU8);
                        break;
                    }
                case OpCode.STSFLD0:
                case OpCode.STSFLD1:
                case OpCode.STSFLD2:
                case OpCode.STSFLD3:
                case OpCode.STSFLD4:
                case OpCode.STSFLD5:
                case OpCode.STSFLD6:
                    {
                        ExecuteStoreToSlot(CurrentContext!.StaticFields, instruction.OpCode - OpCode.STSFLD0);
                        break;
                    }
                case OpCode.STSFLD:
                    {
                        ExecuteStoreToSlot(CurrentContext!.StaticFields, instruction.TokenU8);
                        break;
                    }
                case OpCode.LDLOC0:
                case OpCode.LDLOC1:
                case OpCode.LDLOC2:
                case OpCode.LDLOC3:
                case OpCode.LDLOC4:
                case OpCode.LDLOC5:
                case OpCode.LDLOC6:
                    {
                        ExecuteLoadFromSlot(CurrentContext!.LocalVariables, instruction.OpCode - OpCode.LDLOC0);
                        break;
                    }
                case OpCode.LDLOC:
                    {
                        ExecuteLoadFromSlot(CurrentContext!.LocalVariables, instruction.TokenU8);
                        break;
                    }
                case OpCode.STLOC0:
                case OpCode.STLOC1:
                case OpCode.STLOC2:
                case OpCode.STLOC3:
                case OpCode.STLOC4:
                case OpCode.STLOC5:
                case OpCode.STLOC6:
                    {
                        ExecuteStoreToSlot(CurrentContext!.LocalVariables, instruction.OpCode - OpCode.STLOC0);
                        break;
                    }
                case OpCode.STLOC:
                    {
                        ExecuteStoreToSlot(CurrentContext!.LocalVariables, instruction.TokenU8);
                        break;
                    }
                case OpCode.LDARG0:
                case OpCode.LDARG1:
                case OpCode.LDARG2:
                case OpCode.LDARG3:
                case OpCode.LDARG4:
                case OpCode.LDARG5:
                case OpCode.LDARG6:
                    {
                        ExecuteLoadFromSlot(CurrentContext!.Arguments, instruction.OpCode - OpCode.LDARG0);
                        break;
                    }
                case OpCode.LDARG:
                    {
                        ExecuteLoadFromSlot(CurrentContext!.Arguments, instruction.TokenU8);
                        break;
                    }
                case OpCode.STARG0:
                case OpCode.STARG1:
                case OpCode.STARG2:
                case OpCode.STARG3:
                case OpCode.STARG4:
                case OpCode.STARG5:
                case OpCode.STARG6:
                    {
                        ExecuteStoreToSlot(CurrentContext!.Arguments, instruction.OpCode - OpCode.STARG0);
                        break;
                    }
                case OpCode.STARG:
                    {
                        ExecuteStoreToSlot(CurrentContext!.Arguments, instruction.TokenU8);
                        break;
                    }

                // Splice
                case OpCode.NEWBUFFER:
                    {
                        int length = (int)Pop().GetInteger();
                        Limits.AssertMaxItemSize(length);
                        Push(new Buffer(length));
                        break;
                    }
                case OpCode.MEMCPY:
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
                        break;
                    }
                case OpCode.CAT:
                    {
                        var x2 = Pop().GetSpan();
                        var x1 = Pop().GetSpan();
                        int length = x1.Length + x2.Length;
                        Limits.AssertMaxItemSize(length);
                        Buffer result = new(length, false);
                        x1.CopyTo(result.InnerBuffer.Span);
                        x2.CopyTo(result.InnerBuffer.Span[x1.Length..]);
                        Push(result);
                        break;
                    }
                case OpCode.SUBSTR:
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
                        break;
                    }
                case OpCode.LEFT:
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
                        break;
                    }
                case OpCode.RIGHT:
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
                        break;
                    }

                // Bitwise logic
                case OpCode.INVERT:
                    {
                        var x = Pop().GetInteger();
                        Push(~x);
                        break;
                    }
                case OpCode.AND:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 & x2);
                        break;
                    }
                case OpCode.OR:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 | x2);
                        break;
                    }
                case OpCode.XOR:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 ^ x2);
                        break;
                    }
                case OpCode.EQUAL:
                    {
                        StackItem x2 = Pop();
                        StackItem x1 = Pop();
                        Push(x1.Equals(x2, Limits));
                        break;
                    }
                case OpCode.NOTEQUAL:
                    {
                        StackItem x2 = Pop();
                        StackItem x1 = Pop();
                        Push(!x1.Equals(x2, Limits));
                        break;
                    }

                // Numeric
                case OpCode.SIGN:
                    {
                        var x = Pop().GetInteger();
                        Push(x.Sign);
                        break;
                    }
                case OpCode.ABS:
                    {
                        var x = Pop().GetInteger();
                        Push(BigInteger.Abs(x));
                        break;
                    }
                case OpCode.NEGATE:
                    {
                        var x = Pop().GetInteger();
                        Push(-x);
                        break;
                    }
                case OpCode.INC:
                    {
                        var x = Pop().GetInteger();
                        Push(x + 1);
                        break;
                    }
                case OpCode.DEC:
                    {
                        var x = Pop().GetInteger();
                        Push(x - 1);
                        break;
                    }
                case OpCode.ADD:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 + x2);
                        break;
                    }
                case OpCode.SUB:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 - x2);
                        break;
                    }
                case OpCode.MUL:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 * x2);
                        break;
                    }
                case OpCode.DIV:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 / x2);
                        break;
                    }
                case OpCode.MOD:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 % x2);
                        break;
                    }
                case OpCode.POW:
                    {
                        var exponent = (int)Pop().GetInteger();
                        Limits.AssertShift(exponent);
                        var value = Pop().GetInteger();
                        Push(BigInteger.Pow(value, exponent));
                        break;
                    }
                case OpCode.SQRT:
                    {
                        Push(Pop().GetInteger().Sqrt());
                        break;
                    }
                case OpCode.MODMUL:
                    {
                        var modulus = Pop().GetInteger();
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 * x2 % modulus);
                        break;
                    }
                case OpCode.MODPOW:
                    {
                        var modulus = Pop().GetInteger();
                        var exponent = Pop().GetInteger();
                        var value = Pop().GetInteger();
                        var result = exponent == -1
                            ? value.ModInverse(modulus)
                            : BigInteger.ModPow(value, exponent, modulus);
                        Push(result);
                        break;
                    }
                case OpCode.SHL:
                    {
                        int shift = (int)Pop().GetInteger();
                        Limits.AssertShift(shift);
                        if (shift == 0) break;
                        var x = Pop().GetInteger();
                        Push(x << shift);
                        break;
                    }
                case OpCode.SHR:
                    {
                        int shift = (int)Pop().GetInteger();
                        Limits.AssertShift(shift);
                        if (shift == 0) break;
                        var x = Pop().GetInteger();
                        Push(x >> shift);
                        break;
                    }
                case OpCode.NOT:
                    {
                        var x = Pop().GetBoolean();
                        Push(!x);
                        break;
                    }
                case OpCode.BOOLAND:
                    {
                        var x2 = Pop().GetBoolean();
                        var x1 = Pop().GetBoolean();
                        Push(x1 && x2);
                        break;
                    }
                case OpCode.BOOLOR:
                    {
                        var x2 = Pop().GetBoolean();
                        var x1 = Pop().GetBoolean();
                        Push(x1 || x2);
                        break;
                    }
                case OpCode.NZ:
                    {
                        var x = Pop().GetInteger();
                        Push(!x.IsZero);
                        break;
                    }
                case OpCode.NUMEQUAL:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 == x2);
                        break;
                    }
                case OpCode.NUMNOTEQUAL:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(x1 != x2);
                        break;
                    }
                case OpCode.LT:
                    {
                        var x2 = Pop();
                        var x1 = Pop();
                        if (x1.IsNull || x2.IsNull)
                            Push(false);
                        else
                            Push(x1.GetInteger() < x2.GetInteger());
                        break;
                    }
                case OpCode.LE:
                    {
                        var x2 = Pop();
                        var x1 = Pop();
                        if (x1.IsNull || x2.IsNull)
                            Push(false);
                        else
                            Push(x1.GetInteger() <= x2.GetInteger());
                        break;
                    }
                case OpCode.GT:
                    {
                        var x2 = Pop();
                        var x1 = Pop();
                        if (x1.IsNull || x2.IsNull)
                            Push(false);
                        else
                            Push(x1.GetInteger() > x2.GetInteger());
                        break;
                    }
                case OpCode.GE:
                    {
                        var x2 = Pop();
                        var x1 = Pop();
                        if (x1.IsNull || x2.IsNull)
                            Push(false);
                        else
                            Push(x1.GetInteger() >= x2.GetInteger());
                        break;
                    }
                case OpCode.MIN:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(BigInteger.Min(x1, x2));
                        break;
                    }
                case OpCode.MAX:
                    {
                        var x2 = Pop().GetInteger();
                        var x1 = Pop().GetInteger();
                        Push(BigInteger.Max(x1, x2));
                        break;
                    }
                case OpCode.WITHIN:
                    {
                        BigInteger b = Pop().GetInteger();
                        BigInteger a = Pop().GetInteger();
                        var x = Pop().GetInteger();
                        Push(a <= x && x < b);
                        break;
                    }

                // Compound-type
                case OpCode.PACKMAP:
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
                        break;
                    }
                case OpCode.PACKSTRUCT:
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
                        break;
                    }
                case OpCode.PACK:
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
                        break;
                    }
                case OpCode.UNPACK:
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
                        break;
                    }
                case OpCode.NEWARRAY0:
                    {
                        Push(new VMArray(ReferenceCounter));
                        break;
                    }
                case OpCode.NEWARRAY:
                case OpCode.NEWARRAY_T:
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
                        break;
                    }
                case OpCode.NEWSTRUCT0:
                    {
                        Push(new Struct(ReferenceCounter));
                        break;
                    }
                case OpCode.NEWSTRUCT:
                    {
                        int n = (int)Pop().GetInteger();
                        if (n < 0 || n > Limits.MaxStackSize)
                            throw new InvalidOperationException($"MaxStackSize exceed: {n}");
                        Struct result = new(ReferenceCounter);
                        for (var i = 0; i < n; i++)
                            result.Add(StackItem.Null);
                        Push(result);
                        break;
                    }
                case OpCode.NEWMAP:
                    {
                        Push(new Map(ReferenceCounter));
                        break;
                    }
                case OpCode.SIZE:
                    {
                        var x = Pop();
                        switch (x)
                        {
                            case CompoundType compound:
                                Push(compound.Count);
                                break;
                            case PrimitiveType primitive:
                                Push(primitive.Size);
                                break;
                            case Buffer buffer:
                                Push(buffer.Size);
                                break;
                            default:
                                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
                        }
                        break;
                    }
                case OpCode.HASKEY:
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
                                    break;
                                }
                            case Map map:
                                {
                                    Push(map.ContainsKey(key));
                                    break;
                                }
                            case Buffer buffer:
                                {
                                    int index = (int)key.GetInteger();
                                    if (index < 0)
                                        throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
                                    Push(index < buffer.Size);
                                    break;
                                }
                            case ByteString array:
                                {
                                    int index = (int)key.GetInteger();
                                    if (index < 0)
                                        throw new InvalidOperationException($"The negative value {index} is invalid for OpCode.{instruction.OpCode}.");
                                    Push(index < array.Size);
                                    break;
                                }
                            default:
                                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
                        }
                        break;
                    }
                case OpCode.KEYS:
                    {
                        Map map = Pop<Map>();
                        Push(new VMArray(ReferenceCounter, map.Keys));
                        break;
                    }
                case OpCode.VALUES:
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
                        break;
                    }
                case OpCode.PICKITEM:
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
                                    break;
                                }
                            case Map map:
                                {
                                    if (!map.TryGetValue(key, out StackItem? value))
                                        throw new CatchableException($"Key not found in {nameof(Map)}");
                                    Push(value);
                                    break;
                                }
                            case PrimitiveType primitive:
                                {
                                    ReadOnlySpan<byte> byteArray = primitive.GetSpan();
                                    int index = (int)key.GetInteger();
                                    if (index < 0 || index >= byteArray.Length)
                                        throw new CatchableException($"The value {index} is out of range.");
                                    Push((BigInteger)byteArray[index]);
                                    break;
                                }
                            case Buffer buffer:
                                {
                                    int index = (int)key.GetInteger();
                                    if (index < 0 || index >= buffer.Size)
                                        throw new CatchableException($"The value {index} is out of range.");
                                    Push((BigInteger)buffer.InnerBuffer.Span[index]);
                                    break;
                                }
                            default:
                                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
                        }
                        break;
                    }
                case OpCode.APPEND:
                    {
                        StackItem newItem = Pop();
                        VMArray array = Pop<VMArray>();
                        if (newItem is Struct s) newItem = s.Clone(Limits);
                        array.Add(newItem);
                        break;
                    }
                case OpCode.SETITEM:
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
                                    break;
                                }
                            case Map map:
                                {
                                    map[key] = value;
                                    break;
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
                                    break;
                                }
                            default:
                                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
                        }
                        break;
                    }
                case OpCode.REVERSEITEMS:
                    {
                        var x = Pop();
                        switch (x)
                        {
                            case VMArray array:
                                array.Reverse();
                                break;
                            case Buffer buffer:
                                buffer.InnerBuffer.Span.Reverse();
                                break;
                            default:
                                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
                        }
                        break;
                    }
                case OpCode.REMOVE:
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
                                break;
                            case Map map:
                                map.Remove(key);
                                break;
                            default:
                                throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
                        }
                        break;
                    }
                case OpCode.CLEARITEMS:
                    {
                        CompoundType x = Pop<CompoundType>();
                        x.Clear();
                        break;
                    }
                case OpCode.POPITEM:
                    {
                        VMArray x = Pop<VMArray>();
                        int index = x.Count - 1;
                        Push(x[index]);
                        x.RemoveAt(index);
                        break;
                    }

                //Types
                case OpCode.ISNULL:
                    {
                        var x = Pop();
                        Push(x.IsNull);
                        break;
                    }
                case OpCode.ISTYPE:
                    {
                        var x = Pop();
                        StackItemType type = (StackItemType)instruction.TokenU8;
                        if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                            throw new InvalidOperationException($"Invalid type: {type}");
                        Push(x.Type == type);
                        break;
                    }
                case OpCode.CONVERT:
                    {
                        var x = Pop();
                        Push(x.ConvertTo((StackItemType)instruction.TokenU8));
                        break;
                    }
                case OpCode.ABORTMSG:
                    {
                        var msg = Pop().GetString();
                        throw new Exception($"{OpCode.ABORTMSG} is executed. Reason: {msg}");
                    }
                case OpCode.ASSERTMSG:
                    {
                        var msg = Pop().GetString();
                        var x = Pop().GetBoolean();
                        if (!x)
                            throw new Exception($"{OpCode.ASSERTMSG} is executed with false result. Reason: {msg}");
                        break;
                    }
                default: throw new InvalidOperationException($"Opcode {instruction.OpCode} is undefined.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteEndTry(int endOffset)
        {
            if (CurrentContext!.TryStack is null)
                throw new InvalidOperationException($"The corresponding TRY block cannot be found.");
            if (!CurrentContext.TryStack.TryPeek(out ExceptionHandlingContext? currentTry))
                throw new InvalidOperationException($"The corresponding TRY block cannot be found.");
            if (currentTry.State == ExceptionHandlingState.Finally)
                throw new InvalidOperationException($"The opcode {OpCode.ENDTRY} can't be executed in a FINALLY block.");

            int endPointer = checked(CurrentContext.InstructionPointer + endOffset);
            if (currentTry.HasFinally)
            {
                currentTry.State = ExceptionHandlingState.Finally;
                currentTry.EndPointer = endPointer;
                CurrentContext.InstructionPointer = currentTry.FinallyPointer;
            }
            else
            {
                CurrentContext.TryStack.Pop();
                CurrentContext.InstructionPointer = endPointer;
            }
            isJumping = true;
        }

        /// <summary>
        /// Jump to the specified position.
        /// </summary>
        /// <param name="position">The position to jump to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExecuteJump(int position)
        {
            if (position < 0 || position >= CurrentContext!.Script.Length)
                throw new ArgumentOutOfRangeException($"Jump out of range for position: {position}");
            CurrentContext.InstructionPointer = position;
            isJumping = true;
        }

        /// <summary>
        /// Jump to the specified offset from the current position.
        /// </summary>
        /// <param name="offset">The offset from the current position to jump to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExecuteJumpOffset(int offset)
        {
            ExecuteJump(checked(CurrentContext!.InstructionPointer + offset));
        }

        private void ExecuteLoadFromSlot(Slot? slot, int index)
        {
            if (slot is null)
                throw new InvalidOperationException("Slot has not been initialized.");
            if (index < 0 || index >= slot.Count)
                throw new InvalidOperationException($"Index out of range when loading from slot: {index}");
            Push(slot[index]);
        }

        /// <summary>
        /// Execute the next instruction.
        /// </summary>
        internal protected void ExecuteNext()
        {
            if (InvocationStack.Count == 0)
            {
                State = VMState.HALT;
            }
            else
            {
                try
                {
                    ExecutionContext context = CurrentContext!;
                    Instruction instruction = context.CurrentInstruction ?? Instruction.RET;
                    PreExecuteInstruction(instruction);
                    try
                    {
                        ExecuteInstruction(instruction);
                    }
                    catch (CatchableException ex) when (Limits.CatchEngineExceptions)
                    {
                        ExecuteThrow(ex.Message);
                    }
                    PostExecuteInstruction(instruction);
                    if (!isJumping) context.MoveNext();
                    isJumping = false;
                }
                catch (Exception e)
                {
                    OnFault(e);
                }
            }
        }

        private void ExecuteStoreToSlot(Slot? slot, int index)
        {
            if (slot is null)
                throw new InvalidOperationException("Slot has not been initialized.");
            if (index < 0 || index >= slot.Count)
                throw new InvalidOperationException($"Index out of range when storing to slot: {index}");
            slot[index] = Pop();
        }

        /// <summary>
        /// Throws a specified exception in the VM.
        /// </summary>
        /// <param name="ex">The exception to be thrown.</param>
        protected void ExecuteThrow(StackItem ex)
        {
            UncaughtException = ex;
            HandleException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteTry(int catchOffset, int finallyOffset)
        {
            if (catchOffset == 0 && finallyOffset == 0)
                throw new InvalidOperationException($"catchOffset and finallyOffset can't be 0 in a TRY block");
            if (CurrentContext!.TryStack is null)
                CurrentContext.TryStack = new Stack<ExceptionHandlingContext>();
            else if (CurrentContext.TryStack.Count >= Limits.MaxTryNestingDepth)
                throw new InvalidOperationException("MaxTryNestingDepth exceed.");
            int catchPointer = catchOffset == 0 ? -1 : checked(CurrentContext.InstructionPointer + catchOffset);
            int finallyPointer = finallyOffset == 0 ? -1 : checked(CurrentContext.InstructionPointer + finallyOffset);
            CurrentContext.TryStack.Push(new ExceptionHandlingContext(catchPointer, finallyPointer));
        }

        private void HandleException()
        {
            int pop = 0;
            foreach (var executionContext in InvocationStack)
            {
                if (executionContext.TryStack != null)
                {
                    while (executionContext.TryStack.TryPeek(out var tryContext))
                    {
                        if (tryContext.State == ExceptionHandlingState.Finally || (tryContext.State == ExceptionHandlingState.Catch && !tryContext.HasFinally))
                        {
                            executionContext.TryStack.Pop();
                            continue;
                        }
                        for (int i = 0; i < pop; i++)
                        {
                            ContextUnloaded(InvocationStack.Pop());
                        }
                        if (tryContext.State == ExceptionHandlingState.Try && tryContext.HasCatch)
                        {
                            tryContext.State = ExceptionHandlingState.Catch;
                            Push(UncaughtException!);
                            executionContext.InstructionPointer = tryContext.CatchPointer;
                            UncaughtException = null;
                        }
                        else
                        {
                            tryContext.State = ExceptionHandlingState.Finally;
                            executionContext.InstructionPointer = tryContext.FinallyPointer;
                        }
                        isJumping = true;
                        return;
                    }
                }
                ++pop;
            }

            throw new VMUnhandledException(UncaughtException!);
        }

        /// <summary>
        /// Loads the specified context into the invocation stack.
        /// </summary>
        /// <param name="context">The context to load.</param>
        protected virtual void LoadContext(ExecutionContext context)
        {
            if (InvocationStack.Count >= Limits.MaxInvocationStackSize)
                throw new InvalidOperationException($"MaxInvocationStackSize exceed: {InvocationStack.Count}");
            InvocationStack.Push(context);
            if (EntryContext is null) EntryContext = context;
            CurrentContext = context;
        }

        /// <summary>
        /// Create a new context with the specified script without loading.
        /// </summary>
        /// <param name="script">The script used to create the context.</param>
        /// <param name="rvcount">The number of values that the context should return when it is unloaded.</param>
        /// <param name="initialPosition">The pointer indicating the current instruction.</param>
        /// <returns>The created context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ExecutionContext CreateContext(Script script, int rvcount, int initialPosition)
        {
            return new ExecutionContext(script, rvcount, ReferenceCounter)
            {
                InstructionPointer = initialPosition
            };
        }

        /// <summary>
        /// Create a new context with the specified script and load it.
        /// </summary>
        /// <param name="script">The script used to create the context.</param>
        /// <param name="rvcount">The number of values that the context should return when it is unloaded.</param>
        /// <param name="initialPosition">The pointer indicating the current instruction.</param>
        /// <returns>The created context.</returns>
        public ExecutionContext LoadScript(Script script, int rvcount = -1, int initialPosition = 0)
        {
            ExecutionContext context = CreateContext(script, rvcount, initialPosition);
            LoadContext(context);
            return context;
        }

        /// <summary>
        /// When overridden in a derived class, loads the specified method token.
        /// Called when <see cref="OpCode.CALLT"/> is executed.
        /// </summary>
        /// <param name="token">The method token to be loaded.</param>
        /// <returns>The created context.</returns>
        protected virtual ExecutionContext LoadToken(ushort token)
        {
            throw new InvalidOperationException($"Token not found: {token}");
        }

        /// <summary>
        /// Called when an exception that cannot be caught by the VM is thrown.
        /// </summary>
        /// <param name="ex">The exception that caused the <see cref="VMState.FAULT"/> state.</param>
        protected virtual void OnFault(Exception ex)
        {
            State = VMState.FAULT;
        }

        /// <summary>
        /// Called when the state of the VM changed.
        /// </summary>
        protected virtual void OnStateChanged()
        {
        }

        /// <summary>
        /// When overridden in a derived class, invokes the specified system call.
        /// Called when <see cref="OpCode.SYSCALL"/> is executed.
        /// </summary>
        /// <param name="method">The system call to be invoked.</param>
        protected virtual void OnSysCall(uint method)
        {
            throw new InvalidOperationException($"Syscall not found: {method}");
        }

        /// <summary>
        /// Returns the item at the specified index from the top of the current stack without removing it.
        /// </summary>
        /// <param name="index">The index of the object from the top of the stack.</param>
        /// <returns>The item at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Peek(int index = 0)
        {
            return CurrentContext!.EvaluationStack.Peek(index);
        }

        /// <summary>
        /// Removes and returns the item at the top of the current stack.
        /// </summary>
        /// <returns>The item removed from the top of the stack.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Pop()
        {
            return CurrentContext!.EvaluationStack.Pop();
        }

        /// <summary>
        /// Removes and returns the item at the top of the current stack and convert it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The item removed from the top of the stack.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop<T>() where T : StackItem
        {
            return CurrentContext!.EvaluationStack.Pop<T>();
        }

        /// <summary>
        /// Called after an instruction is executed.
        /// </summary>
        protected virtual void PostExecuteInstruction(Instruction instruction)
        {
            if (ReferenceCounter.CheckZeroReferred() > Limits.MaxStackSize)
                throw new InvalidOperationException($"MaxStackSize exceed: {ReferenceCounter.Count}");
        }

        /// <summary>
        /// Called before an instruction is executed.
        /// </summary>
        protected virtual void PreExecuteInstruction(Instruction instruction) { }

        /// <summary>
        /// Pushes an item onto the top of the current stack.
        /// </summary>
        /// <param name="item">The item to be pushed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(StackItem item)
        {
            CurrentContext!.EvaluationStack.Push(item);
        }
    }
}
