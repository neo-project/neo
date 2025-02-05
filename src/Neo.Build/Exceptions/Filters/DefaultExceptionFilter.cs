// Copyright (C) 2015-2025 The Neo Project.
//
// DefaultExceptionFilter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions.Interfaces;
using Neo.Build.Extensions;
using System;
using System.CommandLine.Invocation;

namespace Neo.Build.Exceptions.Filters
{
    internal class DefaultExceptionFilter
    {
        internal static void Handler(Exception exception, InvocationContext context)
        {
            if (exception is not OperationCanceledException)
            {
                context.Console.WriteLine(string.Empty);

                if (exception.InnerException is INeoBuildException nbe)
                {
                    context.Console.Error.Write(nbe.Message + Environment.NewLine);
                    context.ExitCode = nbe.HResult;
                    return;
                }

                context.Console.ErrorMessage(exception);
                context.ExitCode = exception.HResult;
            }
        }
    }
}
