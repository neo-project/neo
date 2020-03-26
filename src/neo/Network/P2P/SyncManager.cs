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
        private const int PingCoolingOffPeriod = 60; // in secconds.

        private uint lastTaskIndex = 0;

        public SyncManager(NeoSystem system)
        {
            this.system = system;
            this.lastTaskIndex = Blockchain.Singleton.Height;
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.PersistCompleted));
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
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
                case Blockchain.PersistCompleted persistBlock:
                    OnReceivePersistedBlockIndex(persistBlock.Block.Index);
                    break;
                case Blockchain.RelayResult rr:
                    if (rr.Inventory is Block invalidBlock && rr.Result == VerifyResult.Invalid)
                        OnReceiveInvalidBlockIndex(invalidBlock.Index);
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

        private void OnRegister(RemoteNode node)
        {
            Context.Watch(Sender);
            nodes.Add(Sender, node);
            RequestSync();
        }

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

        private void OnTimer()
        {
            foreach (var node in nodes.Values)
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

        private void OnTerminated(IActorRef actor)
        {
            if (!nodes.TryGetValue(Sender, out RemoteNode remoteNode))
                return;
            NodeSession session = remoteNode.session;
            foreach (uint index in session.IndexTasks.Keys)
                AssignTask(index, session);
            nodes.Remove(actor);
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
            session.IndexTasks.Add(index, DateTime.UtcNow);
            remoteNode.Key.Tell(Message.Create(MessageCommand.GetBlockData, GetBlockDataPayload.Create(index, 1)));
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
