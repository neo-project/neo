// Copyright (C) 2015-2025 The Neo Project.
//
// StateServiceTelemetryEventArgs.cs file belongs to the neo project and is free
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
    public enum StateServiceTelemetryEventType : byte
    {
        SnapshotApplied,
        LocalRootCommitted,
        ValidatedRootAdvanced,
        SnapshotError
    }

    public readonly struct StateServiceTelemetryEventArgs
    {
        public StateServiceTelemetryEventType EventType { get; }
        public uint Height { get; }
        public uint? LocalRootIndex { get; }
        public uint? ValidatedRootIndex { get; }
        public TimeSpan? Duration { get; }
        public int? ChangeCount { get; }
        public string? Stage { get; }
        public string? Reason { get; }

        public StateServiceTelemetryEventArgs(
            StateServiceTelemetryEventType eventType,
            uint height,
            uint? localRootIndex = null,
            uint? validatedRootIndex = null,
            TimeSpan? duration = null,
            int? changeCount = null,
            string? stage = null,
            string? reason = null)
        {
            EventType = eventType;
            Height = height;
            LocalRootIndex = localRootIndex;
            ValidatedRootIndex = validatedRootIndex;
            Duration = duration;
            ChangeCount = changeCount;
            Stage = stage;
            Reason = reason;
        }
    }
}
