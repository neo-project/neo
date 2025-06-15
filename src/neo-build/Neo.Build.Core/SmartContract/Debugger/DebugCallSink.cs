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

using Neo.Extensions;
using Neo.Json;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Build.Core.SmartContract.Debugger
{
    internal class DebugCallSink(
        string methodName,
        int eventId)
    {
        public DebugCallSink(
            string methodName,
            int eventId,
            IEnumerable<string> arguments)
            : this(methodName, eventId)
        {
            Arguments = arguments;
        }

        public DebugCallSink(
            string methodName,
            int eventId,
            IEnumerable<string> arguments,
            IEnumerable<StackItem> resultStack)
            : this(methodName, eventId, arguments)
        {
            Results = resultStack;
        }

        /// <summary>
        /// Gets call method name.
        /// </summary>
        public string Name { get; } = methodName;

        /// <summary>
        /// Gets event id for the call.
        /// </summary>
        public int EventId { get; } = eventId;

        /// <summary>
        /// Gets arguments for the call.
        /// </summary>
        public IEnumerable<string> Arguments { get; } = [];

        /// <summary>
        /// Gets the <see cref="StackItem"/>'s of the call.
        /// </summary>
        public IEnumerable<StackItem> Results { get; } = [];

        public override string ToString() =>
            $"{Name}[{EventId}]({string.Join(", ", Arguments)}) results={new JArray(Results.Select(static s => s.ToJson()))}";
    }
}
