// Copyright (C) 2015-2025 The Neo Project.
//
// FaultEventArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Fuzzer.Runners
{
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
