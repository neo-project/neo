using Akka.Actor;
using Akka.Configuration;
using Neo.IO.Actors;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Network.P2P
{
    public class TaskManager : UntypedActor
    {
        internal class Register { public VersionPayload Version; }
        internal class Update { public uint LastBlockIndex; }
        internal class NewTasks { public InvPayload Payload; }
        public class TaskCompleted { public UInt256 Hash; }
        public class HeaderTaskCompleted { }
        public class RestartTasks { public InvPayload Payload; }
        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TaskTimeout = TimeSpan.FromMinutes(1);
        private static readonly UInt256 MemPoolTaskHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000001");
        private static readonly UInt256 HeaderTaskHash = UInt256.Zero;

        private const int MaxConncurrentTasks = 3;
        private const int PingCoolingOffPeriod = 60_000; // in ms.

        private readonly NeoSystem system;
        /// <summary>
        /// A set of known hashes, of inventories or payloads, already received.
        /// </summary>
        private readonly HashSetCache<UInt256> knownHashes;
        private readonly Dictionary<UInt256, int> globalTasks = new Dictionary<UInt256, int>();
        private readonly Dictionary<IActorRef, TaskSession> sessions = new Dictionary<IActorRef, TaskSession>();
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);
        private bool HasHeaderTask => globalTasks.ContainsKey(HeaderTaskHash);

        public TaskManager(NeoSystem system)
        {
            this.system = system;
            this.knownHashes = new HashSetCache<UInt256>(Blockchain.Singleton.MemPool.Capacity * 2 / 5);
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.PersistCompleted));
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
        }

        private void OnHeaderTaskCompleted()
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            session.Tasks.Remove(HeaderTaskHash);
            DecrementGlobalTask(HeaderTaskHash);
            RequestTasks(session);
        }

        private void OnNewTasks(InvPayload payload)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            // Do not accept payload of type InventoryType.TX if not synced on HeaderHeight
            uint currentHeight = NativeContract.Ledger.CurrentIndex(Blockchain.Singleton.View);
            uint headerHeight = Blockchain.Singleton.HeaderCache.Last?.Index ?? currentHeight;
            if (payload.Type == InventoryType.TX && currentHeight < headerHeight)
            {
                RequestTasks(session);
                return;
            }
            HashSet<UInt256> hashes = new HashSet<UInt256>(payload.Hashes);
            // Remove all previously processed knownHashes from the list that is being requested
            hashes.Remove(knownHashes);
            // Add to AvailableTasks the ones, of type InventoryType.Block, that are global (already under process by other sessions)
            if (payload.Type == InventoryType.Block)
                session.AvailableTasks.UnionWith(hashes.Where(p => globalTasks.ContainsKey(p)));

            // Remove those that are already in process by other sessions
            hashes.Remove(globalTasks);
            if (hashes.Count == 0)
            {
                RequestTasks(session);
                return;
            }

            // Update globalTasks with the ones that will be requested within this current session
            foreach (UInt256 hash in hashes)
            {
                IncrementGlobalTask(hash);
                session.Tasks[hash] = DateTime.UtcNow;
            }

            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, hashes.ToArray()))
                Sender.Tell(Message.Create(MessageCommand.GetData, group));
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
                case TaskCompleted completed:
                    OnTaskCompleted(completed.Hash);
                    break;
                case HeaderTaskCompleted _:
                    OnHeaderTaskCompleted();
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
            if (session.IsFullNode)
                session.AvailableTasks.Add(TaskManager.MemPoolTaskHash);
            sessions.Add(Sender, session);
            RequestTasks(session);
        }

        private void OnUpdate(Update update)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            session.LastBlockIndex = update.LastBlockIndex;
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
            foreach (UInt256 hash in session.Tasks.Keys)
                DecrementGlobalTask(hash);
            sessions.Remove(actor);
        }

        private void OnTimer()
        {
            foreach (TaskSession session in sessions.Values)
                foreach (var task in session.Tasks.ToArray())
                    if (TimeProvider.Current.UtcNow - task.Value > TaskTimeout)
                    {
                        if (session.Tasks.Remove(task.Key))
                            DecrementGlobalTask(task.Key);
                    }
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
            if (session.HasTask) return;

            DataCache snapshot = Blockchain.Singleton.View;

            // If there are pending tasks of InventoryType.Block we should process them
            if (session.AvailableTasks.Count > 0)
            {
                session.AvailableTasks.Remove(knownHashes);
                // Search any similar hash that is on Singleton's knowledge, which means, on the way or already processed
                session.AvailableTasks.RemoveWhere(p => NativeContract.Ledger.ContainsBlock(snapshot, p));
                HashSet<UInt256> hashes = new HashSet<UInt256>(session.AvailableTasks);
                hashes.Remove(MemPoolTaskHash);
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
                        session.RemoteNode.Tell(Message.Create(MessageCommand.GetData, group));
                    return;
                }
            }

            Header[] headers = Blockchain.Singleton.HeaderCache.GetSnapshot();
            Header last = headers == null ? null : (headers.Length > 0 ? headers[headers.Length - 1] : null);
            uint currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);
            uint headerHeight = last?.Index ?? currentHeight;
            UInt256 headerHash = last?.Hash ?? NativeContract.Ledger.CurrentHash(snapshot);
            // When the number of AvailableTasks is no more than 0, no pending tasks of InventoryType.Block, it should process pending the tasks of headers
            // If not HeaderTask pending to be processed it should ask for more Blocks
            if ((!HasHeaderTask || globalTasks[HeaderTaskHash] < MaxConncurrentTasks) && headerHeight < session.LastBlockIndex && !Blockchain.Singleton.HeaderCache.Full)
            {
                session.Tasks[HeaderTaskHash] = DateTime.UtcNow;
                IncrementGlobalTask(HeaderTaskHash);
                session.RemoteNode.Tell(Message.Create(MessageCommand.GetHeaders, GetBlockByIndexPayload.Create(headerHeight)));
            }
            else if (currentHeight < session.LastBlockIndex)
            {
                uint startHeight = currentHeight;
                for (uint i = 0; i < headers.Length; i++)
                {
                    if (headers[i].Index <= currentHeight) continue;
                    UInt256 nextHash = headers[i].Hash;
                    if (!globalTasks.ContainsKey(nextHash)) break;
                    startHeight = i;
                }
                session.RemoteNode.Tell(Message.Create(MessageCommand.GetBlockByIndex, GetBlockByIndexPayload.Create(startHeight)));
            }
            else if (headerHeight >= session.LastBlockIndex
                    && TimeProvider.Current.UtcNow.ToTimestampMS() - PingCoolingOffPeriod >= NativeContract.Ledger.GetBlock(snapshot, headerHash)?.Timestamp)
            {
                if (session.AvailableTasks.Remove(MemPoolTaskHash))
                {
                    session.RemoteNode.Tell(Message.Create(MessageCommand.Mempool));
                }

                session.RemoteNode.Tell(Message.Create(MessageCommand.Ping, PingPayload.Create(currentHeight)));
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
                    if (tasks.Payload.Type == InventoryType.Block || tasks.Payload.Type == InventoryType.Extensible)
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
