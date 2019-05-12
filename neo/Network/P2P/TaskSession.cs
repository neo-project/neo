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
        public bool HeaderTask => Tasks.ContainsKey(UInt256.Zero);
        public uint StartHeight { get; }

        public TaskSession(IActorRef node, VersionPayload version)
        {
            RemoteNode = node;
            Version = version;
            StartHeight = version.Capabilities
              .Where(u => u is FullNodeCapability)
              .Cast<FullNodeCapability>()
              .FirstOrDefault()?.StartHeight ?? 0;
        }
    }
}