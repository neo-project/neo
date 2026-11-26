// Copyright (C) 2015-2026 The Neo Project.
//
// TaskSession.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.P2P
{
    internal class TaskSession
    {
        public const int MaxTasks = 100;

        public Dictionary<UInt256, DateTime> InvTasks { get; } = new Dictionary<UInt256, DateTime>();
        public Dictionary<uint, DateTime> IndexTasks { get; } = new Dictionary<uint, DateTime>();
        public HashSet<UInt256> AvailableTasks { get; } = new HashSet<UInt256>();
        // Inventory beyond the active task limit remains available for later requests.
        public Queue<(InventoryType Type, UInt256 Hash)> DeferredTasks { get; } = new();
        public HashSet<UInt256> DeferredHashes { get; } = new();
        public Dictionary<uint, UInt256> ReceivedBlockHashes { get; } = new Dictionary<uint, UInt256>();
        public int PendingTaskCount => InvTasks.Count + IndexTasks.Count;
        public int TaskCount => PendingTaskCount + AvailableTasks.Count;
        public bool HasTooManyTasks => TaskCount >= MaxTasks;
        public bool IsFullNode { get; }
        public uint LastBlockIndex { get; set; }
        public bool MempoolSent { get; set; }
        public uint BlockTaskTimeouts { get; set; }

        public TaskSession(VersionPayload version)
        {
            var fullNode = version.Capabilities.OfType<FullNodeCapability>().FirstOrDefault();
            IsFullNode = fullNode != null;
            LastBlockIndex = fullNode?.StartHeight ?? 0;
        }
    }
}
