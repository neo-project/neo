// Copyright (C) 2015-2025 The Neo Project.
//
// DefaultExceptionFilter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Extensions;
using System;
using System.CommandLine.Invocation;

namespace Neo.Build.Exceptions.Filters
{
    internal class DefaultExceptionFilter
    {
        internal static void Handler(Exception exception, InvocationContext context)
        {
#if DEBUG
            if (exception is not OperationCanceledException)
            {
                context.Console.WriteLine(string.Empty);
                context.Console.ErrorMessage(exception);
            }
#endif

            context.ExitCode = exception.HResult;
        }
    }
}
