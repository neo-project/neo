using System;
using System.Collections.Generic;

namespace Neo.Network.P2P
{
    internal class NodeSession
    {
        public readonly Dictionary<UInt256, DateTime> InvTasks = new Dictionary<UInt256, DateTime>();
        public Dictionary<uint, DateTime> IndexTasks = new Dictionary<uint, DateTime>();

        public uint TimeoutTimes = 0;
        public uint InvalidBlockCount = 0;

        public bool HasInvTask => InvTasks.Count > 0;
        public bool HasIndexTask => IndexTasks.Count > 0;
    }
}
