using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.P2P
{
    internal class TaskSession
    {
        public readonly Dictionary<UInt256, DateTime> InvTasks = new Dictionary<UInt256, DateTime>();
        public readonly Dictionary<uint, DateTime> IndexTasks = new Dictionary<uint, DateTime>();

        public bool IsFullNode { get; }
        public uint LastBlockIndex { get; set; }
        public uint TimeoutTimes = 0;
        public uint InvalidBlockCount = 0;
        public DateTime ExpireTime = DateTime.MinValue;

        public TaskSession(VersionPayload version)
        {
            var fullNode = version.Capabilities.OfType<FullNodeCapability>().FirstOrDefault();
            this.IsFullNode = fullNode != null;
            this.LastBlockIndex = fullNode.StartHeight;
        }
    }
}
