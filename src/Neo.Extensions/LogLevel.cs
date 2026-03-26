// Copyright (C) 2015-2026 The Neo Project.
//
// LogLevel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Serilog.Events;

namespace Neo
{
    /// <summary>
    /// Represents the level of logs.
    /// </summary>
    public enum LogLevel : byte
    {
        /// <summary>
        /// The debug log level.
        /// </summary>
        Debug = LogEventLevel.Debug,

        /// <summary>
        /// The information log level.
        /// </summary>
        Info = LogEventLevel.Information,

        /// <summary>
        /// The warning log level.
        /// </summary>
        Warning = LogEventLevel.Warning,

        /// <summary>
        /// The error log level.
        /// </summary>
        Error = LogEventLevel.Error,

        /// <summary>
        /// The fatal log level.
        /// </summary>
        Fatal = LogEventLevel.Fatal
    }
}
