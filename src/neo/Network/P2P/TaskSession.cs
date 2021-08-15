// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
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
        public Dictionary<UInt256, DateTime> InvTasks { get; } = new Dictionary<UInt256, DateTime>();
        public Dictionary<uint, DateTime> IndexTasks { get; } = new Dictionary<uint, DateTime>();
        public HashSet<UInt256> AvailableTasks { get; } = new HashSet<UInt256>();
        public Dictionary<uint, Block> ReceivedBlock { get; } = new Dictionary<uint, Block>();
        public bool HasTooManyTasks => InvTasks.Count + IndexTasks.Count >= 100;
        public bool IsFullNode { get; }
        public uint LastBlockIndex { get; set; }
        public bool MempoolSent { get; set; }

        public TaskSession(VersionPayload version)
        {
            var fullNode = version.Capabilities.OfType<FullNodeCapability>().FirstOrDefault();
            this.IsFullNode = fullNode != null;
            this.LastBlockIndex = fullNode?.StartHeight ?? 0;
        }
    }
}
