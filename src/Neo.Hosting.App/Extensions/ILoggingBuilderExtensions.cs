// Copyright (C) 2015-2024 The Neo Project.
//
// ILoggingBuilderExtensions.cs file belongs to the neo project and is free
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
using Microsoft.Extensions.Logging.Configuration;
using Neo.Hosting.App.Configuration.Logging;
using Neo.Hosting.App.Extensions.Logging;
using System;

namespace Neo.Hosting.App.Extensions
{
    internal static class ILoggingBuilderExtensions
    {
        public static ILoggingBuilder AddNeoErrorLogger(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, NeoErrorLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions<NeoErrorLoggerOptions, NeoErrorLoggerProvider>(builder.Services);

            return builder;
        }

        public static ILoggingBuilder AddNeoErrorLogger(this ILoggingBuilder builder, Action<NeoErrorLoggerOptions> configure)
        {
            builder.AddNeoErrorLogger();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
