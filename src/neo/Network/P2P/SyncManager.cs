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
        public class Task { public uint indexStart; public ushort count; public DateTime time; }
        public class BlockIndex { public uint blockIndex; }
        public class Update { public uint LastBlockIndex; }
        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan SyncTimeout = TimeSpan.FromMinutes(1);

        private readonly Dictionary<IActorRef, SyncSession> sessions = new Dictionary<IActorRef, SyncSession>();
        private readonly NeoSystem system;
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);

        private const ushort blocksPerSession = 100;
        private const uint maxTasksCount = 5;
        private uint totalTasksCount = 0;
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

        private void OnReceiveBlockIndex(BlockIndex blockIndex)
        {
            if (blockIndex.blockIndex <= persistIndex) return;
            persistIndex = blockIndex.blockIndex;
            if (persistIndex > taskIndex)
                taskIndex = persistIndex;
            foreach (SyncSession s in sessions.Values)
            {
                if (s.Task != null)
                {
                    if (s.Task.indexStart + s.Task.count <= persistIndex + 1)
                    {
                        s.Task = null;
                        totalTasksCount--;
                        RequestSync();
                    }
                } 
            }
        }

        private void OnTimer()
        {
            foreach (SyncSession session in sessions.Values)
            {
                if (session.Task != null && DateTime.UtcNow - session.Task.time > SyncTimeout)
                {
                    if (ReSync(session, session.Task.indexStart, session.Task.count))
                    {
                        session.Task = null;
                        session.timeoutTimes++;
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

        private bool ReSync(SyncSession oldSession, uint indexStart, ushort count)
        {
            Random rand = new Random();
            SyncSession session = sessions.Where(p => p.Value != oldSession && p.Value.Task == null && p.Value.LastBlockIndex >= (indexStart + count)).OrderBy(s => rand.Next()).FirstOrDefault().Value;
            if (session == null)
                return false;
            session.Task = new Task { indexStart = persistIndex, count = count, time = DateTime.UtcNow };
            session.RemoteNode.Tell(Message.Create(MessageCommand.GetBlockData, GetBlockDataPayload.Create(persistIndex, count)));
            return true;
        }

        private void OnReceiveBlock(Block block)
        {
            system.Blockchain.Tell(block);
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
            uint availableTasks = (uint)Math.Min(maxTasksCount - totalTasksCount, sessions.Where(p => p.Value.Task == null).Count());
            uint hightestBlockIndex = sessions.Max(p => p.Value.LastBlockIndex);
            if (taskIndex == 0)
                taskIndex = Blockchain.Singleton.Height + 1;
            //Console.WriteLine("taskIndex:" + taskIndex + "hightestBlockIndex: " + hightestBlockIndex);
            if (taskIndex + 5 < hightestBlockIndex)
            {
                Random rand = new Random();
                for (uint i = availableTasks; i > 0; i--)
                {
                    var count = (ushort)((hightestBlockIndex - taskIndex > blocksPerSession) ? blocksPerSession : (hightestBlockIndex - taskIndex + 1));
                    SyncSession session = sessions.Where(p => p.Value.Task == null && p.Value.LastBlockIndex >= taskIndex).OrderBy(s => rand.Next()).FirstOrDefault().Value;
                    if (count == 0 || session == null) continue;
                    uint IndexStart = taskIndex;
                    session.Task = new Task { indexStart = IndexStart, count = count, time = DateTime.UtcNow };
                    totalTasksCount++;
                    //Console.WriteLine("requ:" + taskIndex + "count: "+ count);
                    taskIndex += count;   
                    session.RemoteNode.Tell(Message.Create(MessageCommand.GetBlockData, GetBlockDataPayload.Create(IndexStart, count)));
                }
            }
        }

        private void OnTerminated(IActorRef actor)
        {
            if (!sessions.TryGetValue(actor, out SyncSession session))
                return;
            sessions.Remove(actor);
            totalTasksCount--;
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
