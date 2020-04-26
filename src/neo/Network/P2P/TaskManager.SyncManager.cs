using Akka.Actor;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.P2P
{
    partial class TaskManager : UntypedActor
    {
        public class PersistedBlockIndex { public uint PersistedIndex; }
        public class InvalidBlockIndex { public uint InvalidIndex; }
        public class StartSync { }

        private readonly Dictionary<uint, RemoteNode> receivedBlockIndex = new Dictionary<uint, RemoteNode>();
        private readonly List<uint> failedTasks = new List<uint>();

        private const int MaxTasksCount = 50;
        private const int PingCoolingOffPeriod = 60; // in secconds.

        private uint lastTaskIndex = 0;

        private void OnReceiveBlock(Block block)
        {
            var node = nodes.Values.FirstOrDefault(p => p.session.IndexTasks.ContainsKey(block.Index));
            if (node is null) return;
            node.session.IndexTasks.Remove(block.Index);
            receivedBlockIndex.Add(block.Index, node);
            system.Blockchain.Tell(block);
            RequestSync();
        }

        private void OnReceivePersistedBlockIndex(uint blockIndex)
        {
            receivedBlockIndex.Remove(blockIndex);
        }

        private void OnReceiveInvalidBlockIndex(uint invalidIndex)
        {
            receivedBlockIndex.TryGetValue(invalidIndex, out RemoteNode node);
            if (node is null) return;
            node.session.InvalidBlockCount++;
            node.session.IndexTasks.Remove(invalidIndex);
            receivedBlockIndex.Remove(invalidIndex);
            AssignTask(invalidIndex, node.session);
        }

        private void RequestSync()
        {
            if (nodes.Count() == 0) return;

            SendPingMessage();

            while (failedTasks.Count() > 0)
            {
                if (failedTasks[0] <= Blockchain.Singleton.Height)
                {
                    failedTasks.Remove(failedTasks[0]);
                    continue;
                }
                if (!AssignTask(failedTasks[0])) return;
            }

            int taskCounts = nodes.Values.Sum(p => p.session.IndexTasks.Count);
            var highestBlockIndex = nodes.Values.Max(p => p.LastBlockIndex);
            for (; taskCounts < MaxTasksCount; taskCounts++)
            {
                if (lastTaskIndex >= highestBlockIndex) break;
                if (!AssignTask(++lastTaskIndex)) break;
            }
        }

        private bool AssignTask(uint index, NodeSession filterSession = null)
        {
            if (index <= Blockchain.Singleton.Height || nodes.Values.Any(p => p.session != filterSession && p.session.IndexTasks.ContainsKey(index)))
                return true;
            Random rand = new Random();
            KeyValuePair<IActorRef, RemoteNode> remoteNode = nodes.Where(p => p.Value.session != filterSession && p.Value.LastBlockIndex >= index)
                .OrderBy(p => p.Value.session.IndexTasks.Count)
                .ThenBy(s => rand.Next())
                .FirstOrDefault();
            if (remoteNode.Value == null)
            {
                failedTasks.Add(index);
                return false;
            }
            NodeSession session = remoteNode.Value.session;
            session.IndexTasks.Add(index, TimeProvider.Current.UtcNow);
            remoteNode.Key.Tell(Message.Create(MessageCommand.GetBlockByIndex, GetBlockByIndexPayload.Create(index, 1)));
            failedTasks.Remove(index);
            return true;
        }

        private void SendPingMessage()
        {
            foreach (KeyValuePair<IActorRef, RemoteNode> item in nodes)
            {
                var node = item.Key;
                var remoteNode = item.Value;
                if (Blockchain.Singleton.Height >= remoteNode.LastBlockIndex
                    && TimeProvider.Current.UtcNow.ToTimestamp() - PingCoolingOffPeriod >= Blockchain.Singleton.GetBlock(Blockchain.Singleton.CurrentBlockHash)?.Timestamp)
                {
                    node.Tell(Message.Create(MessageCommand.Ping, PingPayload.Create(Blockchain.Singleton.Height)));
                }
            }
        }
    }
}
