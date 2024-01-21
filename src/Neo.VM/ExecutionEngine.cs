// Copyright (C) 2015-2024 The Neo Project.
//
// ExecutionEngine.cs file belongs to the neo project and is free
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Represents the VM used to execute the script.
    /// </summary>
    public partial class ExecutionEngine : IDisposable
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
            protected internal set
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
                    PushInt(instruction);
                    break;
                case OpCode.PUSHT:
                    PushT(instruction);
                    break;
                case OpCode.PUSHF:
                    PushF(instruction);
                    break;
                case OpCode.PUSHA:
                    PushA(instruction);
                    break;
                case OpCode.PUSHNULL:
                    PushNull(instruction);
                    break;
                case OpCode.PUSHDATA1:
                case OpCode.PUSHDATA2:
                case OpCode.PUSHDATA4:
                    PushData(instruction);
                    break;
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
                    Push(instruction);
                    break;

                // Control
                case OpCode.NOP: break;
                case OpCode.JMP:
                    Jmp(instruction);
                    break;
                case OpCode.JMP_L:
                    Jmp_L(instruction);
                    break;
                case OpCode.JMPIF:
                    JmpIf(instruction);
                    break;
                case OpCode.JMPIF_L:
                    JmpIf_L(instruction);
                    break;
                case OpCode.JMPIFNOT:
                    JmpIfNot(instruction);
                    break;
                case OpCode.JMPIFNOT_L:
                    JmpIfNot_L(instruction);
                    break;
                case OpCode.JMPEQ:
                    JmpEq(instruction);
                    break;
                case OpCode.JMPEQ_L:
                    JmpEq_L(instruction);
                    break;
                case OpCode.JMPNE:
                    JmpNe(instruction);
                    break;
                case OpCode.JMPNE_L:
                    JmpNe_L(instruction);
                    break;
                case OpCode.JMPGT:
                    JmpGt(instruction);
                    break;
                case OpCode.JMPGT_L:
                    JmpGt_L(instruction);
                    break;
                case OpCode.JMPGE:
                    JmpGe(instruction);
                    break;
                case OpCode.JMPGE_L:
                    JmpGe_L(instruction);
                    break;
                case OpCode.JMPLT:
                    JmpLt(instruction);
                    break;
                case OpCode.JMPLT_L:
                    JmpLt_L(instruction);
                    break;
                case OpCode.JMPLE:
                    JmpLe(instruction);
                    break;
                case OpCode.JMPLE_L:
                    JmpLe_L(instruction);
                    break;
                case OpCode.CALL:
                    Call(instruction);
                    break;
                case OpCode.CALL_L:
                    Call_L(instruction);
                    break;
                case OpCode.CALLA:
                    CallA(instruction);
                    break;
                case OpCode.CALLT:
                    CallT(instruction);
                    break;
                case OpCode.ABORT:
                    Abort(instruction);
                    break;
                case OpCode.ASSERT:
                    Assert(instruction);
                    break;
                case OpCode.THROW:
                    Throw(instruction);
                    break;
                case OpCode.TRY:
                    Try(instruction);
                    break;
                case OpCode.TRY_L:
                    Try_L(instruction);
                    break;
                case OpCode.ENDTRY:
                    EndTry(instruction);
                    break;
                case OpCode.ENDTRY_L:
                    EndTry_L(instruction);
                    break;
                case OpCode.ENDFINALLY:
                    EndFinally(instruction);
                    break;
                case OpCode.RET:
                    Ret(instruction);
                    break;
                case OpCode.SYSCALL:
                    Syscall(instruction);
                    break;

                // Stack ops
                case OpCode.DEPTH:
                    Depth(instruction);
                    break;
                case OpCode.DROP:
                    Drop(instruction);
                    break;
                case OpCode.NIP:
                    Nip(instruction);
                    break;
                case OpCode.XDROP:
                    XDrop(instruction);
                    break;
                case OpCode.CLEAR:
                    Clear(instruction);
                    break;
                case OpCode.DUP:
                    Dup(instruction);
                    break;
                case OpCode.OVER:
                    Over(instruction);
                    break;
                case OpCode.PICK:
                    Pick(instruction);
                    break;
                case OpCode.TUCK:
                    Tuck(instruction);
                    break;
                case OpCode.SWAP:
                    Swap(instruction);
                    break;
                case OpCode.ROT:
                    Rot(instruction);
                    break;
                case OpCode.ROLL:
                    Roll(instruction);
                    break;
                case OpCode.REVERSE3:
                    Reverse3(instruction);
                    break;
                case OpCode.REVERSE4:
                    Reverse4(instruction);
                    break;
                case OpCode.REVERSEN:
                    ReverseN(instruction);
                    break;

                //Slot
                case OpCode.INITSSLOT:
                    InitSSlot(instruction);
                    break;
                case OpCode.INITSLOT:
                    InitSlot(instruction);
                    break;
                case OpCode.LDSFLD0:
                case OpCode.LDSFLD1:
                case OpCode.LDSFLD2:
                case OpCode.LDSFLD3:
                case OpCode.LDSFLD4:
                case OpCode.LDSFLD5:
                case OpCode.LDSFLD6:
                    LdSFldM(instruction);
                    break;
                case OpCode.LDSFLD:
                    LdSFld(instruction);
                    break;
                case OpCode.STSFLD0:
                case OpCode.STSFLD1:
                case OpCode.STSFLD2:
                case OpCode.STSFLD3:
                case OpCode.STSFLD4:
                case OpCode.STSFLD5:
                case OpCode.STSFLD6:
                    StSFldM(instruction);
                    break;
                case OpCode.STSFLD:
                    StSFld(instruction);
                    break;
                case OpCode.LDLOC0:
                case OpCode.LDLOC1:
                case OpCode.LDLOC2:
                case OpCode.LDLOC3:
                case OpCode.LDLOC4:
                case OpCode.LDLOC5:
                case OpCode.LDLOC6:
                    LdLocM(instruction);
                    break;
                case OpCode.LDLOC:
                    LdLoc(instruction);
                    break;
                case OpCode.STLOC0:
                case OpCode.STLOC1:
                case OpCode.STLOC2:
                case OpCode.STLOC3:
                case OpCode.STLOC4:
                case OpCode.STLOC5:
                case OpCode.STLOC6:
                    StLocM(instruction);
                    break;
                case OpCode.STLOC:
                    StLoc(instruction);
                    break;
                case OpCode.LDARG0:
                case OpCode.LDARG1:
                case OpCode.LDARG2:
                case OpCode.LDARG3:
                case OpCode.LDARG4:
                case OpCode.LDARG5:
                case OpCode.LDARG6:
                    LdArgM(instruction);
                    break;
                case OpCode.LDARG:
                    LdArg(instruction);
                    break;
                case OpCode.STARG0:
                case OpCode.STARG1:
                case OpCode.STARG2:
                case OpCode.STARG3:
                case OpCode.STARG4:
                case OpCode.STARG5:
                case OpCode.STARG6:
                    StArgM(instruction);
                    break;
                case OpCode.STARG:
                    StArg(instruction);
                    break;

                // Splice
                case OpCode.NEWBUFFER:
                    NewBuffer(instruction);
                    break;
                case OpCode.MEMCPY:
                    Memcpy(instruction);
                    break;
                case OpCode.CAT:
                    Cat(instruction);
                    break;
                case OpCode.SUBSTR:
                    Substr(instruction);
                    break;
                case OpCode.LEFT:
                    Left(instruction);
                    break;
                case OpCode.RIGHT:
                    Right(instruction);
                    break;

                // Bitwise logic
                case OpCode.INVERT:
                    Invert(instruction);
                    break;
                case OpCode.AND:
                    And(instruction);
                    break;
                case OpCode.OR:
                    Or(instruction);
                    break;
                case OpCode.XOR:
                    Xor(instruction);
                    break;
                case OpCode.EQUAL:
                    Equal(instruction);
                    break;
                case OpCode.NOTEQUAL:
                    NotEqual(instruction);
                    break;

                // Numeric
                case OpCode.SIGN:
                    Sign(instruction);
                    break;
                case OpCode.ABS:
                    Abs(instruction);
                    break;
                case OpCode.NEGATE:
                    Negate(instruction);
                    break;
                case OpCode.INC:
                    Inc(instruction);
                    break;
                case OpCode.DEC:
                    Dec(instruction);
                    break;
                case OpCode.ADD:
                    Add(instruction);
                    break;
                case OpCode.SUB:
                    Sub(instruction);
                    break;
                case OpCode.MUL:
                    Mul(instruction);
                    break;
                case OpCode.DIV:
                    Div(instruction);
                    break;
                case OpCode.MOD:
                    Mod(instruction);
                    break;
                case OpCode.POW:
                    Pow(instruction);
                    break;
                case OpCode.SQRT:
                    Sqrt(instruction);
                    break;
                case OpCode.MODMUL:
                    ModMul(instruction);
                    break;
                case OpCode.MODPOW:
                    ModPow(instruction);
                    break;
                case OpCode.SHL:
                    Shl(instruction);
                    break;
                case OpCode.SHR:
                    Shr(instruction);
                    break;
                case OpCode.NOT:
                    Not(instruction);
                    break;
                case OpCode.BOOLAND:
                    BoolAnd(instruction);
                    break;
                case OpCode.BOOLOR:
                    BoolOr(instruction);
                    break;
                case OpCode.NZ:
                    Nz(instruction);
                    break;
                case OpCode.NUMEQUAL:
                    NumEqual(instruction);
                    break;
                case OpCode.NUMNOTEQUAL:
                    NumNotEqual(instruction);
                    break;
                case OpCode.LT:
                    Lt(instruction);
                    break;
                case OpCode.LE:
                    Le(instruction);
                    break;
                case OpCode.GT:
                    Gt(instruction);
                    break;
                case OpCode.GE:
                    Ge(instruction);
                    break;
                case OpCode.MIN:
                    Min(instruction);
                    break;
                case OpCode.MAX:
                    Max(instruction);
                    break;
                case OpCode.WITHIN:
                    Within(instruction);
                    break;

                // Compound-type
                case OpCode.PACKMAP:
                    PackMap(instruction);
                    break;
                case OpCode.PACKSTRUCT:
                    PackStruct(instruction);
                    break;
                case OpCode.PACK:
                    Pack(instruction);
                    break;
                case OpCode.UNPACK:
                    Unpack(instruction);
                    break;
                case OpCode.NEWARRAY0:
                    NewArray0(instruction);
                    break;
                case OpCode.NEWARRAY:
                case OpCode.NEWARRAY_T:
                    NewArray_T(instruction);
                    break;
                case OpCode.NEWSTRUCT0:
                    NewStruct0(instruction);
                    break;
                case OpCode.NEWSTRUCT:
                    NewStruct(instruction);
                    break;
                case OpCode.NEWMAP:
                    NewMap(instruction);
                    break;
                case OpCode.SIZE:
                    Size(instruction);
                    break;
                case OpCode.HASKEY:
                    HasKey(instruction);
                    break;
                case OpCode.KEYS:
                    Keys(instruction);
                    break;
                case OpCode.VALUES:
                    Values(instruction);
                    break;
                case OpCode.PICKITEM:
                    PickItem(instruction);
                    break;
                case OpCode.APPEND:
                    Append(instruction);
                    break;
                case OpCode.SETITEM:
                    SetItem(instruction);
                    break;
                case OpCode.REVERSEITEMS:
                    ReverseItems(instruction);
                    break;
                case OpCode.REMOVE:
                    Remove(instruction);
                    break;
                case OpCode.CLEARITEMS:
                    ClearItems(instruction);
                    break;
                case OpCode.POPITEM:
                    PopItem(instruction);
                    break;

                //Types
                case OpCode.ISNULL:
                    IsNull(instruction);
                    break;
                case OpCode.ISTYPE:
                    IsType(instruction);
                    break;
                case OpCode.CONVERT:
                    Convert(instruction);
                    break;
                case OpCode.ABORTMSG:
                    AbortMsg(instruction);
                    break;
                case OpCode.ASSERTMSG:
                    AssertMsg(instruction);
                    break;
                default:
                    throw new InvalidOperationException($"Opcode {instruction.OpCode} is undefined.");
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
        protected internal void ExecuteNext()
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
