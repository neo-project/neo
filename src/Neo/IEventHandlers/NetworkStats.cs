// Copyright (C) 2015-2025 The Neo Project.
//
// NetworkStats.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.IEventHandlers
{
    /// <summary>
    /// Network statistics snapshot
    /// </summary>
    public class NetworkStats
    {
        public int ConnectedPeers { get; set; }
        public int UnconnectedPeers { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public int PendingTasks { get; set; }
        public int HighPriorityQueueSize { get; set; }
        public int LowPriorityQueueSize { get; set; }
        public Dictionary<string, long> MessagesSentByType { get; set; } = [];
        public Dictionary<string, long> MessagesReceivedByType { get; set; } = [];
    }
}
