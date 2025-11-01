// Copyright (C) 2015-2025 The Neo Project.
//
// RpcTelemetryEventArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

#nullable enable

namespace Neo.IEventHandlers
{
    public enum RpcTelemetryEventType : byte
    {
        Started,
        Completed
    }

    public readonly struct RpcTelemetryEventArgs
    {
        public RpcTelemetryEventType EventType { get; }
        public string Method { get; }
        public TimeSpan? Duration { get; }
        public bool? Success { get; }
        public int? ErrorCode { get; }
        public string? ErrorMessage { get; }

        public RpcTelemetryEventArgs(
            RpcTelemetryEventType eventType,
            string method,
            TimeSpan? duration = null,
            bool? success = null,
            int? errorCode = null,
            string? errorMessage = null)
        {
            EventType = eventType;
            Method = method;
            Duration = duration;
            Success = success;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }
}
