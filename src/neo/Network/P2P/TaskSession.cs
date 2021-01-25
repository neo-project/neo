using Akka.Actor;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.P2P
{
    internal class TaskSession
    {
        public readonly IActorRef RemoteNode;
        public readonly VersionPayload Version;
        public readonly Dictionary<UInt256, DateTime> Tasks = new Dictionary<UInt256, DateTime>();
        public readonly HashSet<UInt256> AvailableTasks = new HashSet<UInt256>();

        public bool HasTask => Tasks.Count > 0;
        public uint StartHeight { get; }
        public bool IsFullNode { get; }
        public uint LastBlockIndex { get; set; }

        public TaskSession(IActorRef node, VersionPayload version)
        {
            var fullNode = version.Capabilities.OfType<FullNodeCapability>().FirstOrDefault();
            this.IsFullNode = fullNode != null;
            this.RemoteNode = node;
            this.Version = version;
            this.StartHeight = fullNode?.StartHeight ?? 0;
            this.LastBlockIndex = this.StartHeight;
        }
    }
}
