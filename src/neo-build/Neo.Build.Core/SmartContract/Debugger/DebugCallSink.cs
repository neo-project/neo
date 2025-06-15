// Copyright (C) 2015-2025 The Neo Project.
//
// DebugCallSink.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.Build.Core.SmartContract.Debugger
{
    internal class DebugCallSink(
        string methodName,
        int eventId)
    {
        public DebugCallSink(
            string methodName,
            int eventId,
            IEnumerable<object> arguments)
            : this(methodName, eventId)
        {
            Arguments = arguments;
        }

        public DebugCallSink(
            string methodName,
            int eventId,
            IEnumerable<object> arguments,
            IEnumerable<StackItem> ResultStack)
            : this(methodName, eventId, arguments)
        {
            Results = ResultStack;
        }

        /// <summary>
        /// Gets call method name.
        /// </summary>
        public string Name { get; set; } = methodName;

        /// <summary>
        /// Gets event id for the call.
        /// </summary>
        public int EventId { get; set; } = eventId;

        /// <summary>
        /// Gets arguments for the call.
        /// </summary>
        public IEnumerable<object> Arguments { get; set; } = [];

        /// <summary>
        /// Gets the <see cref="StackItem"/>'s of the call.
        /// </summary>
        public IEnumerable<StackItem> Results { get; set; } = [];
    }
}
