using Akka.Actor;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.P2P
{
    internal class TaskManager : UntypedActor
    {
        public class Register { public VersionPayload Version; }
        public class NewTasks { public InvPayload Payload; }
        public class TaskCompleted { public UInt256 Hash; }
        public class HeaderTaskCompleted { }
        public class AllowHashes { public UInt256[] Hashes; }
        public class RestartTasks { public InvPayload Payload; }
        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TaskTimeout = TimeSpan.FromMinutes(1);

        private readonly HashSet<UInt256> knownHashes = new HashSet<UInt256>();
        private readonly HashSet<UInt256> globalTasks = new HashSet<UInt256>();
        private readonly Dictionary<IActorRef, TaskSession> sessions = new Dictionary<IActorRef, TaskSession>();
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);

        private bool HeaderTask => sessions.Values.Any(p => p.HeaderTask);

        private void OnAllowHashes(UInt256[] hashes)
        {
            knownHashes.ExceptWith(hashes);
        }

        private void OnHeaderTaskCompleted()
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            session.Tasks.Remove(UInt256.Zero);
            RequestTasks(session);
        }

        private void OnNewTasks(InvPayload payload)
        {
            TaskSession session = sessions[Sender];
            if (payload.Type == InventoryType.TX && Blockchain.Singleton.Snapshot.Height < Blockchain.Singleton.Snapshot.HeaderHeight)
            {
                RequestTasks(session);
                return;
            }
            HashSet<UInt256> hashes = new HashSet<UInt256>(payload.Hashes.Take(InvPayload.MaxHashesCount));
            hashes.ExceptWith(knownHashes);
            switch (payload.Type)
            {
                case InventoryType.Block:
                    using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                        foreach (UInt256 hash in hashes.ToArray())
                            if (snapshot.ContainsBlock(hash))
                            {
                                hashes.Remove(hash);
                                knownHashes.Add(hash);
                            }
                    foreach (UInt256 hash in hashes)
                        if (globalTasks.Contains(hash))
                            session.AvailableTasks.Add(hash);
                    break;
                case InventoryType.TX:
                    using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                        foreach (UInt256 hash in hashes.ToArray())
                            if (snapshot.ContainsTransaction(hash))
                            {
                                hashes.Remove(hash);
                                knownHashes.Add(hash);
                            }
                    break;
            }
            hashes.ExceptWith(globalTasks);
            if (hashes.Count == 0)
            {
                RequestTasks(session);
                return;
            }
            globalTasks.UnionWith(hashes);
            foreach (UInt256 hash in hashes)
                session.Tasks[hash] = DateTime.UtcNow;
            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, hashes.ToArray()))
                Sender.Tell(new RemoteNode.Send
                {
                    Message = Message.Create("getdata", group)
                });
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Register register:
                    OnRegister(register.Version);
                    break;
                case NewTasks tasks:
                    OnNewTasks(tasks.Payload);
                    break;
                case TaskCompleted completed:
                    OnTaskCompleted(completed.Hash);
                    break;
                case HeaderTaskCompleted _:
                    OnHeaderTaskCompleted();
                    break;
                case AllowHashes allow:
                    OnAllowHashes(allow.Hashes);
                    break;
                case RestartTasks restart:
                    OnRestartTasks(restart.Payload);
                    break;
                case Timer _:
                    OnTimer();
                    break;
                case Terminated terminated:
                    OnTerminated(terminated.ActorRef);
                    break;
            }
        }

        private void OnRegister(VersionPayload version)
        {
            Context.Watch(Sender);
            TaskSession session = new TaskSession(Sender, version);
            sessions.Add(Sender, session);
            RequestTasks(session);
        }

        private void OnRestartTasks(InvPayload payload)
        {
            OnAllowHashes(payload.Hashes);
            globalTasks.ExceptWith(payload.Hashes);
            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, payload.Hashes))
                Context.Parent.Tell(new LocalNode.Broadcast
                {
                    Message = Message.Create("getdata", group)
                });
        }

        private void OnTaskCompleted(UInt256 hash)
        {
            knownHashes.Add(hash);
            globalTasks.Remove(hash);
            foreach (TaskSession ms in sessions.Values)
                ms.AvailableTasks.Remove(hash);
            if (sessions.TryGetValue(Sender, out TaskSession session))
            {
                session.Tasks.Remove(hash);
                RequestTasks(session);
            }
        }

        private void OnTerminated(IActorRef actor)
        {
            if (!sessions.TryGetValue(actor, out TaskSession session))
                return;
            sessions.Remove(actor);
            globalTasks.ExceptWith(session.Tasks.Keys);
        }

        private void OnTimer()
        {
            foreach (TaskSession session in sessions.Values)
                foreach (var task in session.Tasks.ToArray())
                    if (DateTime.UtcNow - task.Value > TaskTimeout)
                    {
                        globalTasks.Remove(task.Key);
                        session.Tasks.Remove(task.Key);
                    }
            foreach (TaskSession session in sessions.Values)
                RequestTasks(session);
        }

        protected override void PostStop()
        {
            timer.CancelIfNotNull();
            base.PostStop();
        }

        private void RequestTasks(TaskSession session)
        {
            if (session.HasTask) return;
            if (session.AvailableTasks.Count > 0)
            {
                session.AvailableTasks.ExceptWith(knownHashes);
                session.AvailableTasks.RemoveWhere(p => Blockchain.Singleton.ContainsBlock(p));
                HashSet<UInt256> hashes = new HashSet<UInt256>(session.AvailableTasks);
                hashes.ExceptWith(globalTasks);
                if (hashes.Count > 0)
                {
                    session.AvailableTasks.ExceptWith(hashes);
                    globalTasks.UnionWith(hashes);
                    foreach (UInt256 hash in hashes)
                        session.Tasks[hash] = DateTime.UtcNow;
                    foreach (InvPayload group in InvPayload.CreateGroup(InventoryType.Block, hashes.ToArray()))
                        session.RemoteNode.Tell(new RemoteNode.Send
                        {
                            Message = Message.Create("getdata", group)
                        });
                    return;
                }
            }
            if (!HeaderTask && Blockchain.Singleton.Snapshot.HeaderHeight < session.Version.StartHeight)
            {
                session.Tasks[UInt256.Zero] = DateTime.UtcNow;
                session.RemoteNode.Tell(new RemoteNode.Send
                {
                    Message = Message.Create("getheaders", GetBlocksPayload.Create(Blockchain.Singleton.Snapshot.CurrentHeaderHash))
                });
            }
            else if (Blockchain.Singleton.Snapshot.Height < session.Version.StartHeight)
            {
                UInt256 hash = Blockchain.Singleton.Snapshot.CurrentBlockHash;
                for (uint i = Blockchain.Singleton.Snapshot.Height + 1; i <= Blockchain.Singleton.Snapshot.HeaderHeight; i++)
                {
                    hash = Blockchain.Singleton.GetBlockHash(i);
                    if (!globalTasks.Contains(hash))
                    {
                        hash = Blockchain.Singleton.GetBlockHash(i - 1);
                        break;
                    }
                }
                session.RemoteNode.Tell(new RemoteNode.Send
                {
                    Message = Message.Create("getblocks", GetBlocksPayload.Create(hash))
                });
            }
        }
    }
}
