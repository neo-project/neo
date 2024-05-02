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
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Represents the VM used to execute the script.
    /// </summary>
    public class ExecutionEngine : IDisposable
    {
        private VMState state = VMState.BREAK;
        internal bool isJumping = false;

        public JumpTable JumpTable { get; }

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
        public StackItem? UncaughtException { get; internal set; }

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
        public ExecutionEngine(JumpTable? jumpTable = null) : this(jumpTable, new ReferenceCounter(), ExecutionEngineLimits.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionEngine"/> class with the specified <see cref="VM.ReferenceCounter"/> and <see cref="ExecutionEngineLimits"/>.
        /// </summary>
        /// <param name="jumpTable">The jump table to be used.</param>
        /// <param name="referenceCounter">The reference counter to be used.</param>
        /// <param name="limits">Restrictions on the VM.</param>
        protected ExecutionEngine(JumpTable? jumpTable, ReferenceCounter referenceCounter, ExecutionEngineLimits limits)
        {
            JumpTable = jumpTable ?? JumpTable.Default;
            Limits = limits;
            ReferenceCounter = referenceCounter;
            ResultStack = new EvaluationStack(referenceCounter);
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
                    var context = CurrentContext!;
                    var instruction = context.CurrentInstruction ?? Instruction.RET;
                    PreExecuteInstruction(instruction);
#if VMPERF
                    Console.WriteLine("op:["
                                      + this.CurrentContext.InstructionPointer.ToString("X04")
                                      + "]"
                                      + this.CurrentContext.CurrentInstruction?.OpCode
                                      + " "
                                      + this.CurrentContext.EvaluationStack);
#endif
                    try
                    {
                        JumpTable[instruction.OpCode](this, instruction);
                    }
                    catch (CatchableException ex) when (Limits.CatchEngineExceptions)
                    {
                        JumpTable.ExecuteThrow(this, ex.Message);
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

        /// <summary>
        /// Loads the specified context into the invocation stack.
        /// </summary>
        /// <param name="context">The context to load.</param>
        internal virtual void LoadContext(ExecutionContext context)
        {
            if (InvocationStack.Count >= Limits.MaxInvocationStackSize)
                throw new InvalidOperationException($"MaxInvocationStackSize exceed: {InvocationStack.Count}");
            InvocationStack.Push(context);
            if (EntryContext is null) EntryContext = context;
            CurrentContext = context;
        }

        /// <summary>
        /// Called when a context is unloaded.
        /// </summary>
        /// <param name="context">The context being unloaded.</param>
        internal virtual void UnloadContext(ExecutionContext context)
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
            var context = CreateContext(script, rvcount, initialPosition);
            LoadContext(context);
            return context;
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
            if (ReferenceCounter.Count < Limits.MaxStackSize) return;
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
