// Copyright (C) 2015-2024 The Neo Project.
//
// NodeUtilities.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Neo.Service
{
    public static class NodeUtilities
    {
        private static readonly ConcurrentDictionary<string, ILogger> s_neoLoggerCache = new();

        public static UInt160? TryParseUInt160(string? value)
        {
            if (string.IsNullOrEmpty(value)) return default;
            if (UInt160.TryParse(value, out var result))
                return result;
            return UInt160.Zero;
        }

        public static Version GetApplicationVersion() =>
            Assembly.GetExecutingAssembly().GetName().Version!;

        public static int GetApplicationVersionNumber()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version!;
            return version.Major * 1000 + version.Minor * 100 + version.Build * 10 + version.Revision;
        }

        public static ILogger CreateOrGetLogger(ILoggerFactory loggerFactory, string categoryName)
        {
            if (s_neoLoggerCache.TryGetValue(categoryName, out var logger) == false)
            {
                logger = loggerFactory.CreateLogger(categoryName);
                s_neoLoggerCache.TryAdd(categoryName, logger);
            }
            return logger;
        }
    }
}
