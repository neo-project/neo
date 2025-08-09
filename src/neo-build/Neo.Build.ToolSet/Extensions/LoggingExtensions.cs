// Copyright (C) 2015-2025 The Neo Project.
//
// LoggingExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Neo.Build.ToolSet.Providers;

namespace Neo.Build.ToolSet.Extensions
{
    internal static class LoggingExtensions
    {
        public static ILoggingBuilder AddNeoBuildConsole(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, NeoBuildConsoleLoggerProvider>());

            return builder;
        }
    }
}
