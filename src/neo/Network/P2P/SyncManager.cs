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
        public class Task { public uint startIndex; public uint endIndex; public BitArray indexArray; public DateTime time; }
        public class BlockIndex { public uint blockIndex; }
        public class InvalidBlockIndex { public uint invalidBlockIndex; }
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
        private uint taskIndex = 0;
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
                case BlockIndex blockIndex:
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
            foreach (SyncSession session in sessions.Values.Where(p => p.HasTask))
            {
                for (int i = 0; i < session.Tasks.Count(); i++)
                {
                    if (session.Tasks[i].startIndex <= invalidBlockIndex.invalidBlockIndex && session.Tasks[i].endIndex >= invalidBlockIndex.invalidBlockIndex)
                    {
                        session.isBadNode = true;
                        session.Tasks.Remove(session.Tasks[i]);
                        if (!ReSync(session, session.Tasks[i].startIndex, session.Tasks[i].endIndex))
                        {
                            uncompletedTasks.Add(session.Tasks[i]);
                            totalTasksCount--;
                        }
                    }
                }
            }
        }

        private void OnReceiveBlockIndex(BlockIndex blockIndex)
        {
            if (blockIndex.blockIndex <= persistIndex) return;
            persistIndex = blockIndex.blockIndex;
            if (persistIndex > taskIndex)
                taskIndex = persistIndex;
            if (persistIndex % blocksPerTask == 0 || persistIndex == hightestBlockIndex)
            {
                foreach (SyncSession session in sessions.Values.Where(p => p.HasTask))
                {
                    for (int i = 0; i < session.Tasks.Count(); i++)
                    {
                        if (session.Tasks[i].endIndex == persistIndex)
                        {
                            session.Tasks.Remove(session.Tasks[i]);
                            totalTasksCount--;
                            RequestSync();
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
                    if (DateTime.UtcNow - session.Tasks[i].time > SyncTimeout)
                    {
                        session.Tasks.Remove(session.Tasks[i]);
                        session.timeoutTimes++;
                        if (!ReSync(session, session.Tasks[i].startIndex, session.Tasks[i].endIndex))
                        {
                            uncompletedTasks.Add(session.Tasks[i]);
                            totalTasksCount--;
                        }       
                    }
                    if (session.Tasks[i].indexArray.Cast<bool>().All(p => p == true))
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

        private bool ReSync(SyncSession oldSession, uint startIndex, uint endIndex)
        {
            Random rand = new Random();
            SyncSession session = sessions.Values.Where(p => p != oldSession && p.Tasks.Count <= maxTasksPerSession && p.LastBlockIndex >= endIndex).OrderBy(s => rand.Next()).FirstOrDefault();
            if (session == null)
            {
                return false;
            }
            int count = (int)(endIndex - startIndex + 1);
            session.Tasks.Add(new Task { startIndex = startIndex, endIndex = endIndex, indexArray = new BitArray(count), time = DateTime.UtcNow });
            session.RemoteNode.Tell(Message.Create(MessageCommand.GetBlockData, GetBlockDataPayload.Create(startIndex, (ushort)count)));
            return true;
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
                    if (session.Tasks[i].startIndex <= index && session.Tasks[i].endIndex >= index)
                    {
                        session.Tasks[i].indexArray[(int)(index - session.Tasks[i].startIndex)] = true;
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
            if (taskIndex == 0)
                taskIndex = Blockchain.Singleton.Height;
            Random rand = new Random();
            while (maxTasksCount - totalTasksCount >= 0)
            {
                if (uncompletedTasks.Count() != 0)
                {
                    for (int i = 0; i < uncompletedTasks.Count(); i++)
                    {
                        if (ReSync(null, uncompletedTasks[i].startIndex, uncompletedTasks[i].endIndex))
                        {
                            uncompletedTasks.Remove(uncompletedTasks[i]);
                            totalTasksCount++;
                        }
                    }
                }
                if (taskIndex + 5 > hightestBlockIndex) break;
                uint startIndex = taskIndex + 1;
                uint endIndex = Math.Min((startIndex / blocksPerTask + 1) * blocksPerTask, hightestBlockIndex);
                SyncSession session = sessions.Values.Where(p => p.Tasks.Count < maxTasksPerSession && p.LastBlockIndex >= endIndex).OrderBy(s => rand.Next()).FirstOrDefault();
                if (session == null) break;
                int count = (int)(endIndex - startIndex + 1);
                session.Tasks.Add(new Task { startIndex = startIndex, endIndex = endIndex, indexArray = new BitArray(count), time = DateTime.UtcNow });
                totalTasksCount++;
                taskIndex = endIndex;
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
                    var result = ReSync(session, session.Tasks[i].startIndex, session.Tasks[i].endIndex);
                    if (result == false)
                    {
                        uncompletedTasks.Add(session.Tasks[i]);
                        totalTasksCount--;
                    }
                }
            }   
            sessions.Remove(actor);
            if (totalTasksCount == 0) taskIndex = 0;
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
