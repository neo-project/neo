using Akka.Actor;
using Akka.Configuration;
using Akka.IO;
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
        public class RestartTasks { public InvPayload Payload; }
        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TaskTimeout = TimeSpan.FromMinutes(1);
        private static readonly UInt256 HeaderTaskHash = UInt256.Zero;

        private const int MaxConncurrentTasks = 3;

        private readonly NeoSystem system;
        /// <summary>
        /// A set of known hashes, of inventories or payloads, already received.
        /// </summary>
        private readonly HashSetCache<UInt256> knownHashes;
        private readonly Dictionary<UInt256, int> globalInvTasks = new Dictionary<UInt256, int>();
        private readonly Dictionary<uint, int> globalIndexTasks = new Dictionary<uint, int>();
        private readonly Dictionary<IActorRef, TaskSession> sessions = new Dictionary<IActorRef, TaskSession>();
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);

        private bool HasHeaderTask => globalInvTasks.ContainsKey(HeaderTaskHash);

        public TaskManager(NeoSystem system)
        {
            this.system = system;
            this.knownHashes = new HashSetCache<UInt256>(ProtocolSettings.Default.MemoryPoolMaxTransactions * 2 / 5);
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.PersistCompleted));
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
        }

        private void OnHeaders(Header[] _)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;
            if (session.InvTasks.Remove(HeaderTaskHash))
                DecrementGlobalTask(HeaderTaskHash);
            RequestTasks(Sender, session);
        }

        private void OnInvalidBlock(Block invalidBlock)
        {
            foreach (var (actor, session) in sessions)
                if (session.ReceivedBlock.TryGetValue(invalidBlock.Index, out Block block))
                    if (block.Hash == invalidBlock.Hash)
                        actor.Tell(Tcp.Abort.Instance);
        }

        private void OnNewTasks(InvPayload payload)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
                return;

            // Do not accept payload of type InventoryType.TX if not synced on HeaderHeight
            uint currentHeight = NativeContract.Ledger.CurrentIndex(system.StoreView);
            uint headerHeight = system.HeaderCache.Last?.Index ?? currentHeight;
            if (currentHeight < headerHeight && (payload.Type == InventoryType.TX || (payload.Type == InventoryType.Block && currentHeight < session.LastBlockIndex - InvPayload.MaxHashesCount)))
            {
                RequestTasks(Sender, session);
                return;
            }

            HashSet<UInt256> hashes = new HashSet<UInt256>(payload.Hashes);
            // Remove all previously processed knownHashes from the list that is being requested
            hashes.Remove(knownHashes);
            // Add to AvailableTasks the ones, of type InventoryType.Block, that are global (already under process by other sessions)
            if (payload.Type == InventoryType.Block)
                session.AvailableTasks.UnionWith(hashes.Where(p => globalInvTasks.ContainsKey(p)));

            // Remove those that are already in process by other sessions
            hashes.Remove(globalInvTasks);
            if (hashes.Count == 0)
            {
                RequestTasks(Sender, session);
                return;
            }

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
            foreach (var (actor, session) in sessions)
                if (session.ReceivedBlock.Remove(block.Index, out Block receivedBlock))
                {
                    if (block.Hash == receivedBlock.Hash)
                        RequestTasks(actor, session);
                    else
                        actor.Tell(Tcp.Abort.Instance);
                }
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
                case Header[] headers:
                    OnHeaders(headers);
                    break;
                case IInventory inventory:
                    OnTaskCompleted(inventory);
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
            sessions.Add(Sender, session);
            RequestTasks(Sender, session);
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
                globalInvTasks.Remove(hash);
            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, payload.Hashes))
                system.LocalNode.Tell(Message.Create(MessageCommand.GetData, group));
        }

        private void OnTaskCompleted(IInventory inventory)
        {
            Block block = inventory as Block;
            knownHashes.Add(inventory.Hash);
            globalInvTasks.Remove(inventory.Hash);
            if (block is not null)
                globalIndexTasks.Remove(block.Index);
            foreach (TaskSession ms in sessions.Values)
                ms.AvailableTasks.Remove(inventory.Hash);
            if (sessions.TryGetValue(Sender, out TaskSession session))
            {
                session.InvTasks.Remove(inventory.Hash);
                if (block is not null)
                {
                    session.IndexTasks.Remove(block.Index);
                    if (session.ReceivedBlock.TryGetValue(block.Index, out var block_old))
                    {
                        if (block.Hash != block_old.Hash)
                        {
                            Sender.Tell(Tcp.Abort.Instance);
                            return;
                        }
                    }
                    else
                    {
                        session.ReceivedBlock.Add(block.Index, block);
                    }
                }
                RequestTasks(Sender, session);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementGlobalTask(UInt256 hash)
        {
            if (globalInvTasks.TryGetValue(hash, out var value))
            {
                if (value == 1)
                    globalInvTasks.Remove(hash);
                else
                    globalInvTasks[hash] = value - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementGlobalTask(uint index)
        {
            if (globalIndexTasks.TryGetValue(index, out var value))
            {
                if (value == 1)
                    globalIndexTasks.Remove(index);
                else
                    globalIndexTasks[index] = value - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IncrementGlobalTask(UInt256 hash)
        {
            if (!globalInvTasks.TryGetValue(hash, out var value))
            {
                globalInvTasks[hash] = 1;
                return true;
            }
            if (value >= MaxConncurrentTasks)
                return false;

            globalInvTasks[hash] = value + 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IncrementGlobalTask(uint index)
        {
            if (!globalIndexTasks.TryGetValue(index, out var value))
            {
                globalIndexTasks[index] = 1;
                return true;
            }
            if (value >= MaxConncurrentTasks)
                return false;

            globalIndexTasks[index] = value + 1;
            return true;
        }

        private void OnTerminated(IActorRef actor)
        {
            if (!sessions.TryGetValue(actor, out TaskSession session))
                return;
            foreach (UInt256 hash in session.InvTasks.Keys)
                DecrementGlobalTask(hash);
            foreach (uint index in session.IndexTasks.Keys)
                DecrementGlobalTask(index);
            sessions.Remove(actor);
        }

        private void OnTimer()
        {
            foreach (TaskSession session in sessions.Values)
            {
                foreach (var (hash, time) in session.InvTasks.ToArray())
                    if (TimeProvider.Current.UtcNow - time > TaskTimeout)
                    {
                        if (session.InvTasks.Remove(hash))
                            DecrementGlobalTask(hash);
                    }
                foreach (var (index, time) in session.IndexTasks.ToArray())
                    if (TimeProvider.Current.UtcNow - time > TaskTimeout)
                    {
                        if (session.IndexTasks.Remove(index))
                            DecrementGlobalTask(index);
                    }
            }
            foreach (var (actor, session) in sessions)
                RequestTasks(actor, session);
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

        private void RequestTasks(IActorRef remoteNode, TaskSession session)
        {
            if (session.HasTooManyTasks) return;

            DataCache snapshot = system.StoreView;

            // If there are pending tasks of InventoryType.Block we should process them
            if (session.AvailableTasks.Count > 0)
            {
                session.AvailableTasks.Remove(knownHashes);
                // Search any similar hash that is on Singleton's knowledge, which means, on the way or already processed
                session.AvailableTasks.RemoveWhere(p => NativeContract.Ledger.ContainsBlock(snapshot, p));
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
                        session.InvTasks[hash] = DateTime.UtcNow;
                    foreach (InvPayload group in InvPayload.CreateGroup(InventoryType.Block, hashes.ToArray()))
                        remoteNode.Tell(Message.Create(MessageCommand.GetData, group));
                    return;
                }
            }

            uint currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);
            uint headerHeight = system.HeaderCache.Last?.Index ?? currentHeight;
            // When the number of AvailableTasks is no more than 0, no pending tasks of InventoryType.Block, it should process pending the tasks of headers
            // If not HeaderTask pending to be processed it should ask for more Blocks
            if ((!HasHeaderTask || globalInvTasks[HeaderTaskHash] < MaxConncurrentTasks) && headerHeight < session.LastBlockIndex && !system.HeaderCache.Full)
            {
                session.InvTasks[HeaderTaskHash] = DateTime.UtcNow;
                IncrementGlobalTask(HeaderTaskHash);
                remoteNode.Tell(Message.Create(MessageCommand.GetHeaders, GetBlockByIndexPayload.Create(headerHeight)));
            }
            else if (currentHeight < session.LastBlockIndex)
            {
                uint startHeight = currentHeight;
                while (globalIndexTasks.ContainsKey(++startHeight)) { }
                if (startHeight > session.LastBlockIndex || startHeight >= currentHeight + InvPayload.MaxHashesCount) return;
                uint endHeight = startHeight;
                while (!globalIndexTasks.ContainsKey(++endHeight) && endHeight <= session.LastBlockIndex && endHeight <= currentHeight + InvPayload.MaxHashesCount) { }
                uint count = Math.Min(endHeight - startHeight, InvPayload.MaxHashesCount);
                for (uint i = 0; i < count; i++)
                {
                    session.IndexTasks[startHeight + i] = TimeProvider.Current.UtcNow;
                    IncrementGlobalTask(startHeight + i);
                }
                remoteNode.Tell(Message.Create(MessageCommand.GetBlockByIndex, GetBlockByIndexPayload.Create(startHeight, (short)count)));
            }
            else if (!session.MempoolSent)
            {
                session.MempoolSent = true;
                remoteNode.Tell(Message.Create(MessageCommand.Mempool));
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
