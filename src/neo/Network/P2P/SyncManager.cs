using Akka.Actor;
using Akka.Configuration;
using Neo.IO.Actors;
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
        public class Register { public VersionPayload Version; }
        public class Task { public uint StartIndex; public uint EndIndex; public BitArray IndexArray; public DateTime Time; }
        public class PersistedBlockIndex { public uint PersistedIndex; }
        public class InvalidBlockIndex { public uint InvalidIndex; }
        public class Update { public uint LastBlockIndex; }
        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan SyncTimeout = TimeSpan.FromMinutes(1);

        private readonly Dictionary<IActorRef, SyncSession> sessions = new Dictionary<IActorRef, SyncSession>();
        private readonly NeoSystem system;
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);
        private readonly List<Task> uncompletedTasks = new List<Task>();

        private const uint blocksPerTask = 50;
        private const int maxTasksCount = 10;
        private readonly int maxTasksPerSession = 3;
        private int totalTasksCount = 0;
        private uint hightestBlockIndex = 0;
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
                    OnRegister(register.Version);
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
                case Timer _:
                    OnTimer();
                    break;
                case Update update:
                    OnUpdate(update.LastBlockIndex);
                    break;
                case Terminated terminated:
                    OnTerminated(terminated.ActorRef);
                    break;
            }
        }

        private void OnReceiveInvalidBlockIndex(InvalidBlockIndex invalidBlockIndex)
        {
            if (sessions.Count() == 0 || sessions.Values.Where(p => p.HasTask).Count() == 0) return;
            foreach (SyncSession session in sessions.Values.Where(p => p.HasTask))
            {
                for (int i = 0; i < session.Tasks.Count(); i++)
                {
                    if (session.Tasks[i].StartIndex <= invalidBlockIndex.InvalidIndex && session.Tasks[i].EndIndex >= invalidBlockIndex.InvalidIndex)
                    {
                        session.InvalidBlockCount++;
                        session.Tasks.Remove(session.Tasks[i]);
                        ReSync(session, session.Tasks[i]);
                        break;
                    }
                }
            }
        }

        private void OnReceiveBlockIndex(PersistedBlockIndex persistedIndex)
        {
            if (persistedIndex.PersistedIndex <= persistIndex) return;
            persistIndex = persistedIndex.PersistedIndex;
            if (persistIndex > lastTaskIndex)
                lastTaskIndex = persistIndex;
            if (persistIndex % blocksPerTask == 0 || persistIndex == hightestBlockIndex)
            {
                foreach (SyncSession session in sessions.Values.Where(p => p.HasTask))
                {
                    for (int i = 0; i < session.Tasks.Count(); i++)
                    {
                        if (session.Tasks[i].EndIndex <= persistIndex)
                        {
                            session.Tasks.Remove(session.Tasks[i]);
                            totalTasksCount--;
                            RequestSync();
                            break;
                        }
                    }
                } 
            }
        }

        private void OnTimer()
        {
            if (sessions.Count() == 0 || sessions.Values.Where(p => p.HasTask).Count() == 0) return;
            foreach (SyncSession session in sessions.Values.Where(p => p.HasTask))
            {
                for (int i = 0; i < session.Tasks.Count(); i++)
                {
                    if (DateTime.UtcNow - session.Tasks[i].Time > SyncTimeout)
                    {
                        session.Tasks.Remove(session.Tasks[i]);
                        session.TimeoutTimes++;
                        ReSync(session, session.Tasks[i]);
                    }
                    if (session.Tasks[i].IndexArray.Cast<bool>().All(p => p == true))
                    {
                        totalTasksCount--;
                    }
                }
            }
            RequestSync();
        }

        private void OnUpdate(uint lastBlockIndex)
        {
            if (!sessions.TryGetValue(Sender, out SyncSession session))
                return;
            session.LastBlockIndex = lastBlockIndex;
        }

        private void ReSync(SyncSession oldSession, Task task)
        {
            Random rand = new Random();
            SyncSession session = sessions.Values.Where(p => p != oldSession && p.Tasks.Count <= maxTasksPerSession && p.LastBlockIndex >= task.EndIndex).OrderBy(p => p.Tasks.Count).ThenBy(s => rand.Next()).FirstOrDefault();
            if (session == null)
            {
                uncompletedTasks.Add(task);
                totalTasksCount--;
                return;
            }
            int count = (int)(task.EndIndex - task.StartIndex + 1);
            session.Tasks.Add(new Task { StartIndex = task.StartIndex, EndIndex = task.EndIndex, IndexArray = new BitArray(count), Time = DateTime.UtcNow });
            session.RemoteNode.Tell(Message.Create(MessageCommand.GetBlockData, GetBlockDataPayload.Create(task.StartIndex, (ushort)count)));
            uncompletedTasks.Remove(task);
            totalTasksCount++;
        }

        private void OnReceiveBlock(Block block)
        {
            if (!sessions.TryGetValue(Sender, out SyncSession session))
                return;
            var index = block.Index;
            if (session.HasTask)
            {
                for (int i = 0; i < session.Tasks.Count(); i++)
                {
                    if (session.Tasks[i].StartIndex <= index && session.Tasks[i].EndIndex >= index)
                    {
                        session.Tasks[i].IndexArray[(int)(index - session.Tasks[i].StartIndex)] = true;
                        system.Blockchain.Tell(block);
                    }
                }
            }
        }

        private void OnRegister(VersionPayload version)
        {
            Context.Watch(Sender);
            SyncSession session = new SyncSession(Sender, version);
            sessions.Add(Sender, session);
            RequestSync();
        }

        private void RequestSync()
        {
            if (totalTasksCount >= maxTasksCount || sessions.Count() == 0) return;
            hightestBlockIndex = sessions.Max(p => p.Value.LastBlockIndex);
            if (lastTaskIndex == 0)
                lastTaskIndex = Blockchain.Singleton.Height;
            Random rand = new Random();
            while (maxTasksCount - totalTasksCount >= 0)
            {
                if (uncompletedTasks.Count() != 0)
                {
                    for (int i = 0; i < uncompletedTasks.Count(); i++)
                    {
                        ReSync(null, uncompletedTasks[i]);
                    }
                }
                if (lastTaskIndex + 3 > hightestBlockIndex) break;
                uint startIndex = lastTaskIndex + 1;
                uint endIndex = Math.Min((startIndex / blocksPerTask + 1) * blocksPerTask, hightestBlockIndex);
                int count = (int)(endIndex - startIndex + 1);
                SyncSession session = sessions.Values.Where(p => p.Tasks.Count < maxTasksPerSession && p.LastBlockIndex >= endIndex).OrderBy(p => p.Tasks.Count).ThenBy(s => rand.Next()).FirstOrDefault();
                if (session == null) break;
                session.Tasks.Add(new Task { StartIndex = startIndex, EndIndex = endIndex, IndexArray = new BitArray(count), Time = DateTime.UtcNow });
                totalTasksCount++;
                lastTaskIndex = endIndex;
                session.RemoteNode.Tell(Message.Create(MessageCommand.GetBlockData, GetBlockDataPayload.Create(startIndex, (ushort)count)));
            }
        }

        private void OnTerminated(IActorRef actor)
        {
            if (!sessions.TryGetValue(actor, out SyncSession session))
                return;
            if (session.HasTask)
            {
                for (int i = 0; i < session.Tasks.Count(); i++)
                {
                    ReSync(session, session.Tasks[i]);
                }
            }
            sessions.Remove(actor);
            if (totalTasksCount == 0) lastTaskIndex = 0;
        }

        protected override void PostStop()
        {
            timer.CancelIfNotNull();
            base.PostStop();
        }

        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new SyncManager(system)).WithMailbox("sync-manager-mailbox");
        }
    }

    internal class SyncManagerMailbox : PriorityMailbox
    {
        public SyncManagerMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
        }
    }
}
