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
        public double RTT = 100.0;
        public double Weight = 1000.0;

        public const int MaxTaskCountPerNode = 10;

        public void UpdateRTT(double newRTT)
        {
            RTT = 0.9 * RTT + 0.1 * newRTT;
        }

        public void UpdateWeight()
        {
            Weight = RTT * (1.0 / Math.Pow(2, TimeoutTimes)) * (1.0 / Math.Pow(2, InvalidBlockCount)) * ((double)(MaxTaskCountPerNode + 1 - IndexTasks.Count) / (MaxTaskCountPerNode + 1));
        }

        public TaskSession(VersionPayload version)
        {
            var fullNode = version.Capabilities.OfType<FullNodeCapability>().FirstOrDefault();
            IsFullNode = fullNode != null;
            LastBlockIndex = fullNode.StartHeight;
        }
    }
}
