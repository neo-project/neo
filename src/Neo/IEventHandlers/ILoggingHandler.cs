// Copyright (C) 2015-2025 The Neo Project.
//
// ILoggingHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IEventHandlers
{
    public interface ILoggingHandler
    {
        /// <summary>
        /// The handler of Logging event from <see cref="Utility"/>
        /// Triggered when a new log is added by calling <see cref="Utility.Log"/>
        /// </summary>
        /// <param name="source">The source of the log. Used to identify the producer of the log.</param>
        /// <param name="level">The level of the log.</param>
        /// <param name="message">The message of the log.</param>
        void Utility_Logging_Handler(string source, LogLevel level, object message);
    }
}
