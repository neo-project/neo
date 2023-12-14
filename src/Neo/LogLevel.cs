// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using static Akka.Event.LogLevel;

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
        Debug = DebugLevel,

        /// <summary>
        /// The information log level.
        /// </summary>
        Info = InfoLevel,

        /// <summary>
        /// The warning log level.
        /// </summary>
        Warning = WarningLevel,

        /// <summary>
        /// The error log level.
        /// </summary>
        Error = ErrorLevel,

        /// <summary>
        /// The fatal log level.
        /// </summary>
        Fatal = Error + 1
    }
}
