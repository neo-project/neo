// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Build.Core.Exceptions
{
    public class NeoBuildException(
        string errorMessage,
        int errorCode = NeoBuildErrorCodes.General.InternalException) : Exception(), INeoBuildException
    {
        public NeoBuildException(Exception exception) : this(exception.Message, exception.HResult) { }
        public NeoBuildException(Exception exception, int exitCode) : this(exception.Message, exitCode) { }

        /// <summary>
        /// Exit code from the build process.
        /// </summary>
        public int ExitCode => HResult = errorCode;

        /// <summary>
        /// An error code for referencing the problem.
        /// </summary>
        public string ErrorCode => $"{NeoBuildErrorCodes.StringPrefix}{(uint)ExitCode:d04}";

        /// <summary>
        /// A description of the root cause of the problem.
        /// </summary>
        public override string Message => $"Error {ErrorCode} {errorMessage}";

        public override string ToString() =>
            Message;
    }
}
