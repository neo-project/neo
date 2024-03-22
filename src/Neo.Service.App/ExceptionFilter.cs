// Copyright (C) 2015-2024 The Neo Project.
//
// ExceptionFilter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Service.App.Extensions;
using System;
using System.CommandLine.Invocation;
using System.CommandLine.IO;

namespace Neo.Service.App
{
    internal static class ExceptionFilter
    {
        internal static void Handler(Exception exception, InvocationContext context)
        {
            var ex = exception.InnerException ?? exception;

            context.Console.ResetTerminalForegroundColor();
            context.Console.SetTerminalForegroundRed();

            context.Console.Error.WriteLine($"Error: {ex.Message}");

            context.Console.ResetTerminalForegroundColor();

            context.ExitCode = 1;
        }
    }
}
