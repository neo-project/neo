using Akka.Actor;
using Akka.Configuration;
using Neo.IO.Actors;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Network.P2P
{
    internal class TaskManager : UntypedActor
    {
        public class Register { public VersionPayload Version; }
        public class Update { public uint LastBlockIndex; public bool RequestTasks; }
        public class NewTasks { public InvPayload Payload; }
        public class RestartTasks { public InvPayload Payload; }
        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TaskTimeout = TimeSpan.FromMinutes(1);
        private static readonly UInt256 MemPoolTaskHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000001");

        private const int MaxConncurrentTasks = 3;
        private const int MaxSyncTasksCount = 50;
        private const int PingCoolingOffPeriod = 60_000; // in ms.

        private readonly NeoSystem system;
        /// <summary>
        /// A set of known hashes, of inventories or payloads, already received.
        /// </summary>
        private readonly HashSetCache<UInt256> knownHashes;
        private readonly Dictionary<UInt256, int> globalTasks = new Dictionary<UInt256, int>();
        private readonly Dictionary<uint, TaskSession> receivedBlockIndex = new Dictionary<uint, TaskSession>();
        private readonly HashSet<uint> failedSyncTasks = new HashSet<uint>();
        private readonly Dictionary<IActorRef, TaskSession> sessions = new Dictionary<IActorRef, TaskSession>();
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);
        private uint lastTaskIndex = 0;

        public TaskManager(NeoSystem system)
        {
            this.system = system;
            this.knownHashes = new HashSetCache<UInt256>(Blockchain.Singleton.MemPool.Capacity * 2 / 5);
            this.lastTaskIndex = Blockchain.Singleton.Height;
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.PersistCompleted));
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
        }

        private bool AssignSyncTask(uint index, TaskSession filterSession = null)
        {
            if (index <= Blockchain.Singleton.Height || sessions.Values.Any(p => p != filterSession && p.IndexTasks.ContainsKey(index)))
                return true;
            Random rand = new Random();
            KeyValuePair<IActorRef, TaskSession> remoteNode = sessions.Where(p => p.Value != filterSession && p.Value.LastBlockIndex >= index)
                .OrderBy(p => p.Value.IndexTasks.Count)
                .ThenBy(s => rand.Next())
                .FirstOrDefault();
            if (remoteNode.Value == null)
            {
                failedSyncTasks.Add(index);
                return false;
            }
            TaskSession session = remoteNode.Value;
            session.IndexTasks.TryAdd(index, TimeProvider.Current.UtcNow);
            remoteNode.Key.Tell(Message.Create(MessageCommand.GetBlockByIndex, GetBlockByIndexPayload.Create(index, 1)));
            failedSyncTasks.Remove(index);
            return true;
        }

        private void OnBlock(Block block)
        {
            var session = sessions.Values.FirstOrDefault(p => p.IndexTasks.ContainsKey(block.Index));
            if (session is null) return;
            session.IndexTasks.Remove(block.Index);
            receivedBlockIndex.TryAdd(block.Index, session);
            RequestTasks(false);
        }

        private void OnInvalidBlock(Block invalidBlock)
        {
            receivedBlockIndex.TryGetValue(invalidBlock.Index, out TaskSession session);
            if (session is null) return;
            session.InvalidBlockCount++;
            session.IndexTasks.Remove(invalidBlock.Index);
            receivedBlockIndex.Remove(invalidBlock.Index);
            AssignSyncTask(invalidBlock.Index, session);
        }

        private void OnNewTasks(InvPayload payload)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            // Do not accept payload of type InventoryType.TX if not synced on best known HeaderHeight
            if (payload.Type == InventoryType.TX && sessions.Values.Where(p => p.LastBlockIndex > Blockchain.Singleton.Height + 12).Count() > sessions.Count / 2)
                return;
            HashSet<UInt256> hashes = new HashSet<UInt256>(payload.Hashes);
            // Remove all previously processed knownHashes from the list that is being requested
            hashes.Remove(knownHashes);

            // Remove those that are already in process by other sessions
            hashes.Remove(globalTasks);
            if (hashes.Count == 0)
                return;

            // Update globalTasks with the ones that will be requested within this current session
            foreach (UInt256 hash in hashes)
            {
                IncrementGlobalTask(hash);
                session.InvTasks[hash] = TimeProvider.Current.UtcNow;
            }

            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, hashes.ToArray()))
                Sender.Tell(Message.Create(MessageCommand.GetData, group));
        }

        private void OnPersistCompleted(Block block)
        {
            receivedBlockIndex.Remove(block.Index);
            RequestTasks(false);
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Register register:
                    OnRegister(register.Version);
                    break;
                case Update update:
                    OnUpdate(update);
                    break;
                case NewTasks tasks:
                    OnNewTasks(tasks.Payload);
                    break;
                case RestartTasks restart:
                    OnRestartTasks(restart.Payload);
                    break;
                case Block block:
                    OnBlock(block);
                    break;
                case IInventory inventory:
                    OnTaskCompleted(inventory.Hash);
                    break;
                case Blockchain.PersistCompleted pc:
                    OnPersistCompleted(pc.Block);
                    break;
                case Blockchain.RelayResult rr:
                    if (rr.Inventory is Block invalidBlock && rr.Result == VerifyResult.Invalid)
                        OnInvalidBlock(invalidBlock);
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
            TaskSession session = new TaskSession(version);
            if (session.IsFullNode)
                session.InvTasks.TryAdd(MemPoolTaskHash, TimeProvider.Current.UtcNow);
            sessions.TryAdd(Sender, session);
            RequestTasks(true);
        }

        private void OnUpdate(Update update)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            session.LastBlockIndex = update.LastBlockIndex;
            session.ExpireTime = TimeProvider.Current.UtcNow.AddMilliseconds(PingCoolingOffPeriod);
            if (update.RequestTasks) RequestTasks(true);
        }

        private void OnRestartTasks(InvPayload payload)
        {
            knownHashes.ExceptWith(payload.Hashes);
            foreach (UInt256 hash in payload.Hashes)
                globalTasks.Remove(hash);
            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, payload.Hashes))
                system.LocalNode.Tell(Message.Create(MessageCommand.GetData, group));
        }

        private void OnTaskCompleted(UInt256 hash)
        {
            knownHashes.Add(hash);
            globalTasks.Remove(hash);
            if (sessions.TryGetValue(Sender, out TaskSession session))
                session.InvTasks.Remove(hash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementGlobalTask(UInt256 hash)
        {
            if (globalTasks.TryGetValue(hash, out var value))
            {
                if (value == 1)
                    globalTasks.Remove(hash);
                else
                    globalTasks[hash] = value - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IncrementGlobalTask(UInt256 hash)
        {
            if (!globalTasks.TryGetValue(hash, out var value))
            {
                globalTasks[hash] = 1;
                return true;
            }
            if (value >= MaxConncurrentTasks)
                return false;

            globalTasks[hash] = value + 1;
            return true;
        }

        private void OnTerminated(IActorRef actor)
        {
            if (!sessions.TryGetValue(actor, out TaskSession session))
                return;
            foreach (uint index in session.IndexTasks.Keys)
                AssignSyncTask(index, session);

            foreach (UInt256 hash in session.InvTasks.Keys)
                DecrementGlobalTask(hash);
            sessions.Remove(actor);
        }

        private void OnTimer()
        {
            foreach (TaskSession session in sessions.Values)
            {
                foreach (KeyValuePair<uint, DateTime> kvp in session.IndexTasks)
                {
                    if (TimeProvider.Current.UtcNow - kvp.Value > TaskTimeout)
                    {
                        session.IndexTasks.Remove(kvp.Key);
                        session.TimeoutTimes++;
                        AssignSyncTask(kvp.Key, session);
                    }
                }

                foreach (var task in session.InvTasks.ToArray())
                {
                    if (TimeProvider.Current.UtcNow - task.Value > TaskTimeout)
                    {
                        if (session.InvTasks.Remove(task.Key))
                            DecrementGlobalTask(task.Key);
                    }
                }
            }
            RequestTasks(true);
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

        private void RequestTasks(bool sendPing)
        {
            if (sessions.Count() == 0) return;

            if (sendPing) SendPingMessage();

            while (failedSyncTasks.Count() > 0)
            {
                var failedTask = failedSyncTasks.First();
                if (failedTask <= Blockchain.Singleton.Height)
                {
                    failedSyncTasks.Remove(failedTask);
                    continue;
                }
                if (!AssignSyncTask(failedTask)) return;
            }

            int taskCounts = sessions.Values.Sum(p => p.IndexTasks.Count);
            var highestBlockIndex = sessions.Values.Max(p => p.LastBlockIndex);
            for (; taskCounts < MaxSyncTasksCount; taskCounts++)
            {
                if (lastTaskIndex >= highestBlockIndex || lastTaskIndex >= Blockchain.Singleton.Height + InvPayload.MaxHashesCount) break;
                if (!AssignSyncTask(++lastTaskIndex)) break;
            }
        }

        private void SendPingMessage()
        {
            TrimmedBlock block;
            using (SnapshotView snapshot = Blockchain.Singleton.GetSnapshot())
            {
                block = snapshot.Blocks[snapshot.CurrentBlockHash];
            }

            foreach (KeyValuePair<IActorRef, TaskSession> item in sessions)
            {
                var node = item.Key;
                var session = item.Value;

                if (session.ExpireTime < TimeProvider.Current.UtcNow ||
                     (block.Index >= session.LastBlockIndex &&
                     TimeProvider.Current.UtcNow.ToTimestampMS() - PingCoolingOffPeriod >= block.Timestamp))
                {
                    if (session.InvTasks.Remove(MemPoolTaskHash))
                    {
                        node.Tell(Message.Create(MessageCommand.Mempool));
                    }
                    node.Tell(Message.Create(MessageCommand.Ping, PingPayload.Create(Blockchain.Singleton.Height)));
                    session.ExpireTime = TimeProvider.Current.UtcNow.AddMilliseconds(PingCoolingOffPeriod);
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
