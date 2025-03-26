// Copyright (C) 2015-2025 The Neo Project.
//
// EventArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.VM.Fuzzer.Runners
{
    /// <summary>
    /// Provides data for the step event in the execution engine
    /// </summary>
    public class StepEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the operation code being executed
        /// </summary>
        public OpCode OpCode { get; }

        /// <summary>
        /// Gets the current instruction pointer position
        /// </summary>
        public int InstructionPointer { get; }

        /// <summary>
        /// Gets the current size of the evaluation stack
        /// </summary>
        public int StackSize { get; }

        /// <summary>
        /// Initializes a new instance of the StepEventArgs class
        /// </summary>
        /// <param name="opCode">The operation code being executed</param>
        /// <param name="instructionPointer">The current instruction pointer position</param>
        /// <param name="stackSize">The current size of the evaluation stack</param>
        public StepEventArgs(OpCode opCode, int instructionPointer, int stackSize)
        {
            OpCode = opCode;
            InstructionPointer = instructionPointer;
            StackSize = stackSize;
        }
    }

    /// <summary>
    /// Provides data for the fault event in the execution engine
    /// </summary>
    public class FaultEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of exception that occurred
        /// </summary>
        public string ExceptionType { get; }

        /// <summary>
        /// Gets the detailed message of the exception
        /// </summary>
        public string ExceptionMessage { get; }

        /// <summary>
        /// Gets the instruction pointer position where the fault occurred
        /// </summary>
        public int InstructionPointer { get; }

        /// <summary>
        /// Initializes a new instance of the FaultEventArgs class
        /// </summary>
        /// <param name="exceptionType">The type of exception that occurred</param>
        /// <param name="exceptionMessage">The detailed message of the exception</param>
        /// <param name="instructionPointer">The instruction pointer position where the fault occurred</param>
        public FaultEventArgs(string exceptionType, string exceptionMessage, int instructionPointer)
        {
            ExceptionType = exceptionType;
            ExceptionMessage = exceptionMessage;
            InstructionPointer = instructionPointer;
        }
    }
}
