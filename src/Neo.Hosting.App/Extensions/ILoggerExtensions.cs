// Copyright (C) 2015-2024 The Neo Project.
//
// ILoggerExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Hosting.App.Extensions
{
    using Microsoft.Extensions.Logging;

    internal static class ILoggerExtensions
    {
        public static void WriteLine(this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.None, "{Text}", string.Format(format, args));
        }

        public static void WriteLine(this ILogger logger)
        {
            logger.Log(LogLevel.None, "{WhiteSpace}", string.Empty);
        }
    }
}
