using Akka.Actor;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.P2P
{
    internal class SyncManager : UntypedActor
    {
        public class Register { public RemoteNode Node; }
        public class PersistedBlockIndex { public uint PersistedIndex; }
        public class InvalidBlockIndex { public uint InvalidIndex; }
        public class StartSync { }
        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan SyncTimeout = TimeSpan.FromMinutes(1);

        private readonly Dictionary<IActorRef, RemoteNode> nodes = new Dictionary<IActorRef, RemoteNode>();
        private readonly Dictionary<uint, RemoteNode> receivedBlockIndex = new Dictionary<uint, RemoteNode>();
        private readonly NeoSystem system;
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);
        private readonly List<uint> failedTasks = new List<uint>();

        private const int MaxTasksCount = 50;
        private const int MaxTasksPerSession = 10;
        private const int PingCoolingOffPeriod = 60; // in secconds.

        private uint highestBlockIndex = 0;
        private uint lastTaskIndex = 0;

        public SyncManager(NeoSystem system)
        {
            this.system = system;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Register register:
                    OnRegister(register.Node);
                    break;
                case Block block:
                    OnReceiveBlock(block);
                    break;
                case PersistedBlockIndex blockIndex:
                    OnReceivePersistedBlockIndex(blockIndex);
                    break;
                case InvalidBlockIndex invalidBlockIndex:
                    OnReceiveInvalidBlockIndex(invalidBlockIndex);
                    break;
                case StartSync _:
                    RequestSync();
                    break;
                case Timer _:
                    OnTimer();
                    break;
                case Terminated terminated:
                    OnTerminated(terminated.ActorRef);
                    break;
            }
        }

        private int GetTasksCount()
        {
            int count = 0;
            foreach (var node in nodes.Values)
                count += node.session.IndexTasks.Count;
            return count;
        }

        private void OnReceiveInvalidBlockIndex(InvalidBlockIndex invalidBlockIndex)
        {
            receivedBlockIndex.TryGetValue(invalidBlockIndex.InvalidIndex, out RemoteNode node);
            if (node != null)
            {
                node.session.InvalidBlockCount++;
                node.session.IndexTasks.Remove(invalidBlockIndex.InvalidIndex);
                receivedBlockIndex.Remove(invalidBlockIndex.InvalidIndex);
                AssignTask(invalidBlockIndex.InvalidIndex, node.session);
                return;
            }
        }

        private void OnReceivePersistedBlockIndex(PersistedBlockIndex blockIndex)
        {
            receivedBlockIndex.Remove(blockIndex.PersistedIndex);
        }

        private void OnTimer()
        {
            RemoteNode[] remoteNodes = nodes.Values.ToArray();
            if (remoteNodes.Count() == 0 || !remoteNodes.Any(p => p.session.HasIndexTask))
                return;
            foreach (var node in remoteNodes)
            {
                foreach (KeyValuePair<uint, DateTime> kvp in node.session.IndexTasks)
                {
                    if (DateTime.UtcNow - kvp.Value > SyncTimeout)
                    {
                        node.session.IndexTasks.Remove(kvp.Key);
                        node.session.TimeoutTimes++;
                        AssignTask(kvp.Key, node.session);
                    }
                }
            }
            RequestSync();
        }

        private bool AssignTask(uint index, NodeSession filterSession = null)
        {
            if (nodes.Values.Any(p => p.session.IndexTasks.ContainsKey(index)))
                return true;
            Random rand = new Random();
            RemoteNode remoteNode = nodes.Values.Where(p => p.session != filterSession && p.session.IndexTasks.Count <= MaxTasksPerSession && p.LastBlockIndex >= index)
                .OrderBy(p => p.session.IndexTasks.Count).ThenBy(s => rand.Next()).FirstOrDefault();
            if (remoteNode == null)
            {
                failedTasks.Add(index);
                return false;
            }
            NodeSession session = remoteNode.session;
            session.IndexTasks.TryAdd(index, DateTime.UtcNow);
            nodes.FirstOrDefault(p => p.Value == remoteNode).Key.Tell(Message.Create(MessageCommand.GetBlockData, GetBlockDataPayload.Create(index, 1)));
            failedTasks.Remove(index);
            return true;
        }

        private void OnReceiveBlock(Block block)
        {
            var node = nodes.Values.FirstOrDefault(p => p.session.IndexTasks.ContainsKey(block.Index));
            if (node != null)
            { 
                node.session.IndexTasks.Remove(block.Index);
                receivedBlockIndex.TryAdd(block.Index, node);
                system.Blockchain.Tell(block);
                RequestSync();
            }
        }

        private void OnRegister(RemoteNode node)
        {
            Context.Watch(Sender);
            nodes.Add(Sender, node);
            RequestSync();
        }

        private void RequestSync()
        {
            if (GetTasksCount() >= MaxTasksCount || nodes.Count() == 0) return;
            SendPingMessage();
            highestBlockIndex = nodes.Values.Max(p => p.LastBlockIndex);
            if (lastTaskIndex == 0)
                lastTaskIndex = Blockchain.Singleton.Height;
            while (GetTasksCount() <= MaxTasksCount)
            {
                if (!StartFailedTasks()) break;
                if (lastTaskIndex >= highestBlockIndex) break;
                uint index = lastTaskIndex + 1;
                if (receivedBlockIndex.ContainsKey(index)) break;
                if (!AssignTask(index)) break;
                lastTaskIndex = index;
            }
        }

        private void SendPingMessage()
        {
            foreach (RemoteNode remoteNode in nodes.Values)
            {
                if (Blockchain.Singleton.Height >= remoteNode.LastBlockIndex
                    && TimeProvider.Current.UtcNow.ToTimestamp() - PingCoolingOffPeriod >= Blockchain.Singleton.GetBlock(Blockchain.Singleton.CurrentBlockHash)?.Timestamp)
                {
                    nodes.FirstOrDefault(p => p.Value == remoteNode).Key.Tell(Message.Create(MessageCommand.Ping, PingPayload.Create(Blockchain.Singleton.Height)));
                }
            }
        }

        private bool StartFailedTasks()
        {
            for (int i = 0; i < failedTasks.Count(); i++)
            {
                if (failedTasks[i] <= Blockchain.Singleton.Height)
                {
                    failedTasks.Remove(failedTasks[i]);
                    continue;
                }
                if (GetTasksCount() >= MaxTasksCount)
                    return false;
                if (!AssignTask(failedTasks[i]))
                    return false;
            }
            return true;
        }

        private void OnTerminated(IActorRef actor)
        {
            if (!nodes.TryGetValue(Sender, out RemoteNode remoteNode))
                return;
            NodeSession session = remoteNode.session;
            foreach (uint index in session.IndexTasks.Keys)
                AssignTask(index, session);
            nodes.Remove(actor);
            if (GetTasksCount() == 0) lastTaskIndex = 0;
        }

        protected override void PostStop()
        {
            timer.CancelIfNotNull();
            base.PostStop();
        }

        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new SyncManager(system));
        }
    }
}
