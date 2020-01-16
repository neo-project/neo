using Akka.Actor;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.Network.P2P.SyncManager;

namespace Neo.Network.P2P
{
    internal class NodeSession
    {
        public readonly Dictionary<UInt256, DateTime> InvTasks = new Dictionary<UInt256, DateTime>();
        public List<IndexTask> IndexTasks = new List<IndexTask>();

        public uint TimeoutTimes = 0;
        public uint InvalidBlockCount = 0;

        public bool HasInvTask => InvTasks.Count > 0;
        public bool HasIndexTask => IndexTasks.Count > 0;
    }
}
