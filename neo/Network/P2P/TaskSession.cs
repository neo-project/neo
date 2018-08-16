using Akka.Actor;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;

namespace Neo.Network.P2P
{
    internal class TaskSession
    {
        public readonly IActorRef RemoteNode;
        public readonly VersionPayload Version;
        public readonly Dictionary<UInt256, DateTime> Tasks = new Dictionary<UInt256, DateTime>();
        public readonly HashSet<UInt256> AvailableTasks = new HashSet<UInt256>();

        public bool HasTask => Tasks.Count > 0;
        public bool HeaderTask => Tasks.ContainsKey(UInt256.Zero);

        public TaskSession(IActorRef node, VersionPayload version)
        {
            this.RemoteNode = node;
            this.Version = version;
        }
    }
}
