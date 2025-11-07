// Copyright (C) 2015-2025 The Neo Project.
//
// ConsensusTelemetryEventArgs.cs file belongs to the neo project and is free
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
    public enum ConsensusTelemetryEventType : byte
    {
        ConsensusStarted,
        ViewChanged,
        MessageSent,
        MessageReceived,
        BlockPersisted,
        RecoveryRequested
    }

    public enum ConsensusMessageKind : byte
    {
        PrepareRequest,
        PrepareResponse,
        Commit,
        ChangeView,
        RecoveryRequest,
        RecoveryMessage
    }

    public readonly struct ConsensusTelemetryEventArgs
    {
        public ConsensusTelemetryEventType EventType { get; }
        public uint Height { get; }
        public byte ViewNumber { get; }
        public int ValidatorCount { get; }
        public int PrimaryIndex { get; }
        public TimeSpan? Duration { get; }
        public string? Reason { get; }
        public ConsensusMessageKind? MessageKind { get; }
        public bool? MessageSent { get; }

        public ConsensusTelemetryEventArgs(
            ConsensusTelemetryEventType eventType,
            uint height,
            byte viewNumber,
            int validatorCount,
            int primaryIndex,
            TimeSpan? duration = null,
            string? reason = null,
            ConsensusMessageKind? messageKind = null,
            bool? messageSent = null)
        {
            EventType = eventType;
            Height = height;
            ViewNumber = viewNumber;
            ValidatorCount = validatorCount;
            PrimaryIndex = primaryIndex;
            Duration = duration;
            Reason = reason;
            MessageKind = messageKind;
            MessageSent = messageSent;
        }
    }
}
