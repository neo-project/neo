using Akka.Actor;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.P2P
{
    internal class SyncManager : UntypedActor
    {
        public class Register { public RemoteNode Node; }
        public class IndexTask { public uint StartIndex; public uint EndIndex; public BitArray IndexArray; public DateTime Time; }
        public class PersistedBlockIndex { public uint PersistedIndex; }
        public class InvalidBlockIndex { public uint InvalidIndex; }
        public class StartSync { }
        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan SyncTimeout = TimeSpan.FromMinutes(1);

        private readonly Dictionary<IActorRef, RemoteNode> nodes = new Dictionary<IActorRef, RemoteNode>();
        private readonly NeoSystem system;
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);
        private readonly List<IndexTask> uncompletedTasks = new List<IndexTask>();

        private const uint BlocksPerTask = 50;
        private const int MaxTasksCount = 10;
        private const int PingCoolingOffPeriod = 60; // in secconds.

        private readonly int maxTasksPerSession = 3;
        private int totalTasksCount = 0;
        private uint highestBlockIndex = 0;
        private uint lastTaskIndex = 0;
        private uint persistIndex = 0;

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
                    OnReceiveBlockIndex(blockIndex);
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

        private bool CheckNodes()
        {
            RemoteNode[] remoteNodes = nodes.Values.ToArray();
            if (remoteNodes.Count() == 0 || remoteNodes.Any(p => p.session.HasIndexTask))
                return false;
            else
                return true;
        }

        private void OnReceiveInvalidBlockIndex(InvalidBlockIndex invalidBlockIndex)
        {
            if (!CheckNodes()) return;
            foreach (Tuple<NodeSession, IndexTask> sessionWithIndexTask in GetIndexTask())
            {
                if (sessionWithIndexTask.Item2.StartIndex <= invalidBlockIndex.InvalidIndex && sessionWithIndexTask.Item2.EndIndex >= invalidBlockIndex.InvalidIndex)
                {
                    sessionWithIndexTask.Item1.InvalidBlockCount++;
                    sessionWithIndexTask.Item1.IndexTasks.Remove(sessionWithIndexTask.Item2);
                    if (!ReSync(sessionWithIndexTask.Item1, sessionWithIndexTask.Item2))
                        IncrementUncompletedTasks(sessionWithIndexTask.Item2);
                    return;
                }
            }
        }

        private void OnReceiveBlockIndex(PersistedBlockIndex persistedIndex)
        {
            if (persistedIndex.PersistedIndex <= persistIndex) return;
            persistIndex = persistedIndex.PersistedIndex;
            if (persistIndex > lastTaskIndex)
                lastTaskIndex = persistIndex;
            if (persistIndex % BlocksPerTask == 0 || persistIndex == highestBlockIndex)
            {
                foreach (Tuple<NodeSession, IndexTask> sessionWithIndexTask in GetIndexTask())
                {
                    if (sessionWithIndexTask.Item2.EndIndex <= persistIndex)
                    {
                        sessionWithIndexTask.Item1.IndexTasks.Remove(sessionWithIndexTask.Item2);
                        totalTasksCount--;
                        RequestSync();
                        return;
                    }
                }
            }
        }

        private IEnumerable<Tuple<NodeSession, IndexTask>> GetIndexTask()
        {
            foreach (RemoteNode node in nodes.Values.Where(p => p.session.HasIndexTask))
            {
                NodeSession session = node.session;
                int count = session.IndexTasks.Count();
                for (int i = 0; i < count; i++)
                    yield return new Tuple<NodeSession, IndexTask>(session, session.IndexTasks[i]);
            }
        }

        private void OnTimer()
        {
            if (!CheckNodes()) return;
            foreach (Tuple<NodeSession, IndexTask> sessionWithIndexTask in GetIndexTask())
            {
                if (DateTime.UtcNow - sessionWithIndexTask.Item2.Time > SyncTimeout)
                {
                    sessionWithIndexTask.Item1.IndexTasks.Remove(sessionWithIndexTask.Item2);
                    sessionWithIndexTask.Item1.TimeoutTimes++;
                    if (!ReSync(sessionWithIndexTask.Item1, sessionWithIndexTask.Item2))
                        IncrementUncompletedTasks(sessionWithIndexTask.Item2);
                }
                if (sessionWithIndexTask.Item2.IndexArray.Cast<bool>().All(p => p == true))
                {
                    totalTasksCount--;
                }
            }
            RequestSync();
        }

        private bool ReSync(NodeSession oldSession, IndexTask task)
        {
            Random rand = new Random();
            RemoteNode remoteNode = nodes.Values.Where(p => p.session != oldSession && p.session.IndexTasks.Count <= maxTasksPerSession && p.LastBlockIndex >= task.EndIndex)
                .OrderBy(p => p.session.IndexTasks.Count).ThenBy(s => rand.Next()).FirstOrDefault();
            if (remoteNode == null)
                return false;
            NodeSession session = remoteNode.session;
            int count = (int)(task.EndIndex - task.StartIndex + 1);
            session.IndexTasks.Add(new IndexTask { StartIndex = task.StartIndex, EndIndex = task.EndIndex, IndexArray = new BitArray(count), Time = DateTime.UtcNow });
            nodes.FirstOrDefault(p => p.Value == remoteNode).Key.Tell(Message.Create(MessageCommand.GetBlockData, GetBlockDataPayload.Create(task.StartIndex, (ushort)count)));
            return true;
        }

        private void IncrementUncompletedTasks(IndexTask task)
        {
            uncompletedTasks.Add(task);
            totalTasksCount--;
        }

        private void DecrementUncompletedTasks(IndexTask task)
        {
            uncompletedTasks.Remove(task);
            totalTasksCount++;
        }

        private void OnReceiveBlock(Block block)
        {
            if (!nodes.TryGetValue(Sender, out RemoteNode remoteNode))
                return;
            var index = block.Index;
            NodeSession session = remoteNode.session;
            if (session.HasIndexTask)
            {
                for (int i = 0; i < session.IndexTasks.Count(); i++)
                {
                    if (session.IndexTasks[i].StartIndex <= index && session.IndexTasks[i].EndIndex >= index)
                    {
                        session.IndexTasks[i].IndexArray[(int)(index - session.IndexTasks[i].StartIndex)] = true;
                        system.Blockchain.Tell(block);
                        break;
                    }
                }
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
            RemoteNode[] remoteNodes = nodes.Values.ToArray();
            if (totalTasksCount >= MaxTasksCount || remoteNodes.Count() == 0) return;
            SendPingMessage();
            highestBlockIndex = remoteNodes.Max(p => p.LastBlockIndex);
            if (lastTaskIndex == 0)
                lastTaskIndex = Blockchain.Singleton.Height;
            Random rand = new Random();
            while (totalTasksCount <= MaxTasksCount)
            {
                if (!StartUncompletedTasks()) break;
                if (lastTaskIndex >= highestBlockIndex) break;
                uint startIndex = lastTaskIndex + 1;
                uint endIndex = Math.Min((startIndex / BlocksPerTask + 1) * BlocksPerTask, highestBlockIndex);
                int count = (int)(endIndex - startIndex + 1);
                var remoteNode = remoteNodes.Where(p => p.session.IndexTasks.Count < maxTasksPerSession && p.LastBlockIndex >= endIndex)
                    .OrderBy(p => p.session.IndexTasks.Count).ThenBy(s => rand.Next()).FirstOrDefault();
                if (remoteNode == null) break;
                remoteNode.session.IndexTasks.Add(new IndexTask { StartIndex = startIndex, EndIndex = endIndex, IndexArray = new BitArray(count), Time = DateTime.UtcNow });
                totalTasksCount++;
                lastTaskIndex = endIndex;
                nodes.FirstOrDefault(p => p.Value == remoteNode).Key.Tell(Message.Create(MessageCommand.GetBlockData, GetBlockDataPayload.Create(startIndex, (ushort)count)));
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

        private bool StartUncompletedTasks()
        {
            if (uncompletedTasks.Count() > 0)
            {
                for (int i = 0; i < uncompletedTasks.Count(); i++)
                {
                    if (totalTasksCount >= MaxTasksCount)
                        return false;
                    if (ReSync(null, uncompletedTasks[i]))
                        DecrementUncompletedTasks(uncompletedTasks[i]);
                    else
                        return false;
                }
            }
            return true;
        }

        private void OnTerminated(IActorRef actor)
        {
            if (!nodes.TryGetValue(Sender, out RemoteNode remoteNode))
                return;
            NodeSession session = remoteNode.session;
            if (session.HasIndexTask)
            {
                for (int i = 0; i < session.IndexTasks.Count(); i++)
                {
                    if (!ReSync(session, session.IndexTasks[i]))
                        IncrementUncompletedTasks(session.IndexTasks[i]);
                }
            }
            nodes.Remove(actor);
            if (totalTasksCount == 0) lastTaskIndex = 0;
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
