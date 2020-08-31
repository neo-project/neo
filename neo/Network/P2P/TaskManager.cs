using Akka.Actor;
using Akka.Configuration;
using Neo.Cryptography;
using Neo.IO.Actors;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Network.P2P
{
    internal class TaskManager : UntypedActor
    {
        public class Register { public VersionPayload Version; }
        public class Update { public uint LastBlockIndex; }
        public class NewTasks { public InvPayload Payload; }
        public class TaskCompleted { public UInt256 Hash; }
        public class HeaderTaskCompleted { }
        public class StateRootTaskCompleted { }
        public class RestartTasks { public InvPayload Payload; }
        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TaskTimeout = TimeSpan.FromMinutes(1);

        private readonly NeoSystem system;
        private const int MaxConncurrentTasks = 3;
        private const int PingCoolingOffPeriod = 60; // in secconds.

        /// <summary>
        /// Max GetBlocks and Headers are limmited to 500 each
        /// Blockchain.Singleton.MemPool.Capacity * 2 was the same value used in ProtocolHandler
        /// </summary>
        private static readonly int MaxCachedHashes = Blockchain.Singleton.MemPool.Capacity * 2;
        private readonly FIFOSet<UInt256> knownHashes = new FIFOSet<UInt256>(MaxCachedHashes);

        private readonly Dictionary<UInt256, int> globalTasks = new Dictionary<UInt256, int>();
        private readonly Dictionary<IActorRef, TaskSession> sessions = new Dictionary<IActorRef, TaskSession>();
        private readonly ConcurrentDictionary<ActorPath, DateTime> _expiredTimes = new ConcurrentDictionary<ActorPath, DateTime>();
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);

        private readonly UInt256 HeaderTaskHash = UInt256.Zero;
        private readonly UInt256 StateRootTaskHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000001");
        private bool HasHeaderTask => globalTasks.ContainsKey(HeaderTaskHash);
        private bool HasStateRootTask => globalTasks.ContainsKey(StateRootTaskHash);
        private DateTime StateRootSyncTime;

        public TaskManager(NeoSystem system)
        {
            this.system = system;
        }

        private void OnInternalTaskCompleted(UInt256 hash)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            session.Tasks.Remove(hash);
            DecrementGlobalTask(hash);
            RequestTasks(session);
        }

        private void OnNewTasks(InvPayload payload)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            if (payload.Type == InventoryType.TX && (Blockchain.Singleton.Height < Blockchain.Singleton.HeaderHeight || Blockchain.Singleton.ExpectStateRootIndex < Blockchain.Singleton.Height))
            {
                RequestTasks(session);
                return;
            }
            HashSet<UInt256> hashes = new HashSet<UInt256>(payload.Hashes);
            hashes.Remove(knownHashes);
            if (payload.Type == InventoryType.Block)
                session.AvailableTasks.UnionWith(hashes.Where(p => globalTasks.ContainsKey(p)));

            hashes.Remove(globalTasks);
            if (hashes.Count == 0)
            {
                RequestTasks(session);
                return;
            }

            foreach (UInt256 hash in hashes)
            {
                IncrementGlobalTask(hash);
                session.Tasks[hash] = DateTime.UtcNow;
            }

            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, hashes.ToArray()))
                Sender.Tell(Message.Create("getdata", group));
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Register register:
                    OnRegister(register.Version);
                    break;
                case Update update:
                    OnUpdate(update.LastBlockIndex);
                    break;
                case NewTasks tasks:
                    OnNewTasks(tasks.Payload);
                    break;
                case TaskCompleted completed:
                    OnTaskCompleted(completed.Hash);
                    break;
                case HeaderTaskCompleted _:
                    OnInternalTaskCompleted(HeaderTaskHash);
                    break;
                case StateRootTaskCompleted _:
                    OnInternalTaskCompleted(StateRootTaskHash);
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

        private void OnUpdate(uint lastBlockIndex)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            session.LastBlockIndex = lastBlockIndex;
        }

        private void OnRestartTasks(InvPayload payload)
        {
            knownHashes.ExceptWith(payload.Hashes);
            foreach (UInt256 hash in payload.Hashes)
                globalTasks.Remove(hash);
            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, payload.Hashes))
                system.LocalNode.Tell(Message.Create("getdata", group));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementGlobalTask(UInt256 hash)
        {
            if (globalTasks.ContainsKey(hash))
            {
                if (globalTasks[hash] == 1)
                    globalTasks.Remove(hash);
                else
                    globalTasks[hash]--;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IncrementGlobalTask(UInt256 hash)
        {
            if (!globalTasks.ContainsKey(hash))
            {
                globalTasks[hash] = 1;
                return true;
            }
            if (globalTasks[hash] >= MaxConncurrentTasks)
                return false;

            globalTasks[hash]++;

            return true;
        }

        private void OnTerminated(IActorRef actor)
        {
            if (!sessions.TryGetValue(actor, out TaskSession session))
                return;
            sessions.Remove(actor);
            _expiredTimes.TryRemove(session.RemoteNode.Path, out _);
            foreach (UInt256 hash in session.Tasks.Keys)
                DecrementGlobalTask(hash);
        }

        private void OnTimer()
        {
            foreach (TaskSession session in sessions.Values)
                foreach (var task in session.Tasks.ToArray())
                    if (DateTime.UtcNow - task.Value > TaskTimeout)
                    {
                        if (session.Tasks.Remove(task.Key))
                            DecrementGlobalTask(task.Key);
                    }

            if (HasStateRootTask && DateTime.UtcNow - StateRootSyncTime > TaskTimeout)
                DecrementGlobalTask(StateRootTaskHash);

            foreach (TaskSession session in sessions.Values)
                RequestTasks(session);
        }

        protected override void PostStop()
        {
            timer.CancelIfNotNull();
            base.PostStop();
        }

        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new TaskManager(system)).WithMailbox("task-manager-mailbox");
        }

        private void RequestTasks(TaskSession session)
        {
            if (!_expiredTimes.TryGetValue(session.RemoteNode.Path, out var expireTime) || expireTime < DateTime.UtcNow)
            {
                session.RemoteNode.Tell(Message.Create("ping", PingPayload.Create(Blockchain.Singleton.Height)));
                _expiredTimes[session.RemoteNode.Path] = DateTime.UtcNow.AddSeconds(PingCoolingOffPeriod);
            }
            if (session.HasTask) return;
            if (session.AvailableTasks.Count > 0)
            {
                session.AvailableTasks.Remove(knownHashes);
                session.AvailableTasks.RemoveWhere(p => Blockchain.Singleton.ContainsBlock(p));
                HashSet<UInt256> hashes = new HashSet<UInt256>(session.AvailableTasks);
                if (hashes.Count > 0)
                {
                    foreach (UInt256 hash in hashes.ToArray())
                    {
                        if (!IncrementGlobalTask(hash))
                            hashes.Remove(hash);
                    }
                    session.AvailableTasks.Remove(hashes);
                    foreach (UInt256 hash in hashes)
                        session.Tasks[hash] = DateTime.UtcNow;
                    foreach (InvPayload group in InvPayload.CreateGroup(InventoryType.Block, hashes.ToArray()))
                        session.RemoteNode.Tell(Message.Create("getdata", group));
                    return;
                }
            }
            if ((!HasHeaderTask || globalTasks[HeaderTaskHash] < MaxConncurrentTasks) && Blockchain.Singleton.HeaderHeight < session.LastBlockIndex)
            {
                session.Tasks[HeaderTaskHash] = DateTime.UtcNow;
                IncrementGlobalTask(HeaderTaskHash);
                session.RemoteNode.Tell(Message.Create("getheaders", GetBlocksPayload.Create(Blockchain.Singleton.CurrentHeaderHash)));
            }
            else if (Blockchain.Singleton.Height < session.LastBlockIndex)
            {
                UInt256 hash = Blockchain.Singleton.CurrentBlockHash;
                for (uint i = Blockchain.Singleton.Height + 1; i <= Blockchain.Singleton.HeaderHeight; i++)
                {
                    hash = Blockchain.Singleton.GetBlockHash(i);
                    if (!globalTasks.ContainsKey(hash))
                    {
                        hash = Blockchain.Singleton.GetBlockHash(i - 1);
                        break;
                    }
                }
                session.RemoteNode.Tell(Message.Create("getblocks", GetBlocksPayload.Create(hash)));
            }
            if (!HasStateRootTask)
            {
                if (Blockchain.Singleton.ExpectStateRootIndex < Blockchain.Singleton.Height)
                {
                    var start_index = Blockchain.Singleton.ExpectStateRootIndex;
                    var count = Math.Min(Blockchain.Singleton.Height - start_index, StateRootsPayload.MaxStateRootsCount);
                    StateRootSyncTime = DateTime.UtcNow;
                    IncrementGlobalTask(StateRootTaskHash);
                    system.LocalNode.Tell(Message.Create("getroots", GetStateRootsPayload.Create(start_index, count)));
                }
            }
        }
    }

    internal class TaskManagerMailbox : PriorityMailbox
    {
        public TaskManagerMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
        }

        internal protected override bool IsHighPriority(object message)
        {
            switch (message)
            {
                case TaskManager.Register _:
                case TaskManager.Update _:
                case TaskManager.RestartTasks _:
                    return true;
                case TaskManager.NewTasks tasks:
                    if (tasks.Payload.Type == InventoryType.Block || tasks.Payload.Type == InventoryType.Consensus)
                        return true;
                    return false;
                default:
                    return false;
            }
        }

        internal protected override bool ShallDrop(object message, IEnumerable queue)
        {
            if (!(message is TaskManager.NewTasks tasks)) return false;
            // Remove duplicate tasks
            if (queue.OfType<TaskManager.NewTasks>().Any(x => x.Payload.Type == tasks.Payload.Type && x.Payload.Hashes.SequenceEqual(tasks.Payload.Hashes))) return true;
            return false;
        }
    }
}
