// Copyright (C) 2015-2025 The Neo Project.
//
// TaskManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Configuration;
using Akka.IO;
using Neo.Extensions;
using Neo.IO.Actors;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Network.P2P
{
    /// <summary>
    /// Actor used to manage the tasks of inventories.
    /// </summary>
    public class TaskManager : UntypedActor
    {
        internal class Register { public VersionPayload Version; }
        internal class Update { public uint LastBlockIndex; }
        internal class NewTasks { public InvPayload Payload; }

        /// <summary>
        /// Sent to <see cref="TaskManager"/> to restart tasks for inventories.
        /// </summary>
        public class RestartTasks
        {
            /// <summary>
            /// The inventories that need to restart.
            /// </summary>
            public InvPayload Payload { get; init; }
        }

        private class Timer { }

        private static readonly TimeSpan TimerInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TaskTimeout = TimeSpan.FromMinutes(1);
        private static readonly UInt256 HeaderTaskHash = UInt256.Zero;

        private const int MaxConcurrentTasks = 3;

        private readonly NeoSystem system;
        /// <summary>
        /// A set of known hashes, of inventories or payloads, already received.
        /// </summary>
        private readonly HashSetCache<UInt256> knownHashes;
        private readonly Dictionary<UInt256, int> globalInvTasks = new();
        private readonly Dictionary<uint, int> globalIndexTasks = new();
        private readonly Dictionary<IActorRef, TaskSession> sessions = new();
        private readonly ICancelable timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimerInterval, TimerInterval, Context.Self, new Timer(), ActorRefs.NoSender);
        private uint lastSeenPersistedIndex = 0;

        private bool HasHeaderTask => globalInvTasks.ContainsKey(HeaderTaskHash);

        // Serilog logger instance
        private readonly ILogger _log = Log.ForContext<TaskManager>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManager"/> class.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the <see cref="TaskManager"/>.</param>
        public TaskManager(NeoSystem system)
        {
            this.system = system;
            _log.Information("TaskManager starting.");
            knownHashes = new HashSetCache<UInt256>(system.MemPool.Capacity * 2 / 5);
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.PersistCompleted));
            Context.System.EventStream.Subscribe(Self, typeof(Blockchain.RelayResult));
        }

        private void OnHeaders(Header[] headers)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
            {
                _log.Warning("Received Headers from unknown sender {Sender}", Sender);
                return;
            }
            if (session.InvTasks.Remove(HeaderTaskHash))
            {
                _log.Debug("Header task completed for {Sender}", Sender);
                DecrementGlobalTask(HeaderTaskHash);
            }
            else
            {
                _log.Warning("Received unexpected Headers from {Sender} (no pending header task)", Sender);
            }
            RequestTasks(Sender, session);
        }

        private void OnInvalidBlock(Block invalidBlock)
        {
            _log.Warning("Received notification of invalid block {BlockHash} ({BlockIndex})", invalidBlock.Hash, invalidBlock.Index);
            foreach (var (actor, session) in sessions)
            {
                if (session.ReceivedBlock.TryGetValue(invalidBlock.Index, out Block block))
                {
                    if (block.Hash == invalidBlock.Hash)
                    {
                        _log.Warning("Disconnecting peer {Sender} due to providing invalid block {BlockHash}", actor, invalidBlock.Hash);
                        actor.Tell(Tcp.Abort.Instance);
                    }
                }
            }
        }

        private void OnNewTasks(InvPayload payload)
        {
            if (!sessions.TryGetValue(Sender, out TaskSession session))
            {
                _log.Warning("Received NewTasks from unknown sender {Sender}", Sender);
                return;
            }
            _log.Verbose("Received NewTasks ({InvType}) from {Sender} with {HashCount} hashes", payload.Type, Sender, payload.Hashes.Length);

            // Do not accept payload of type InventoryType.TX if not synced on HeaderHeight
            uint currentHeight = Math.Max(NativeContract.Ledger.CurrentIndex(system.StoreView), lastSeenPersistedIndex);
            uint headerHeight = system.HeaderCache.Last?.Index ?? currentHeight;
            if (currentHeight < headerHeight && (payload.Type == InventoryType.TX || (payload.Type == InventoryType.Block && currentHeight < session.LastBlockIndex - InvPayload.MaxHashesCount)))
            {
                _log.Debug("Postponing NewTasks ({InvType}) from {Sender} (node not synced)", payload.Type, Sender);
                RequestTasks(Sender, session);
                return;
            }

            HashSet<UInt256> hashes = new(payload.Hashes);
            var originalCount = hashes.Count;
            // Remove all previously processed knownHashes from the list that is being requested
            hashes.Remove(knownHashes);
            if (hashes.Count < originalCount) _log.Verbose("Filtered {RemovedCount} known hashes from NewTasks", originalCount - hashes.Count);

            // Add to AvailableTasks the ones, of type InventoryType.Block, that are global (already under process by other sessions)
            if (payload.Type == InventoryType.Block)
            {
                var globalHashes = hashes.Where(p => globalInvTasks.ContainsKey(p)).ToList();
                if (globalHashes.Count > 0)
                {
                    _log.Verbose("Adding {GlobalCount} global block tasks to available tasks for {Sender}", globalHashes.Count, Sender);
                    session.AvailableTasks.UnionWith(globalHashes);
                }
            }

            // Remove those that are already in process by other sessions
            originalCount = hashes.Count;
            hashes.Remove(globalInvTasks);
            if (hashes.Count < originalCount) _log.Verbose("Filtered {RemovedCount} globally active tasks from NewTasks", originalCount - hashes.Count);

            if (hashes.Count == 0)
            {
                _log.Verbose("No new tasks to request after filtering for {Sender}", Sender);
                RequestTasks(Sender, session);
                return;
            }

            _log.Debug("Requesting {TaskCount} new tasks ({InvType}) from {Sender}", hashes.Count, payload.Type, Sender);
            // Update globalTasks with the ones that will be requested within this current session
            foreach (UInt256 hash in hashes)
            {
                IncrementGlobalTask(hash);
                session.InvTasks[hash] = TimeProvider.Current.UtcNow;
            }

            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, hashes))
                Sender.Tell(Message.Create(MessageCommand.GetData, group));
        }

        private void OnPersistCompleted(Block block)
        {
            lastSeenPersistedIndex = block.Index;
            _log.Verbose("Received PersistCompleted for block {BlockIndex}", block.Index);

            foreach (var (actor, session) in sessions)
            {
                if (session.ReceivedBlock.Remove(block.Index, out Block receivedBlock))
                {
                    if (block.Hash == receivedBlock.Hash)
                    {
                        _log.Verbose("Confirmed received block {BlockIndex} for {Sender}, requesting more tasks", block.Index, actor);
                        RequestTasks(actor, session);
                    }
                    else
                    {
                        _log.Warning("Disconnecting peer {Sender}: Persisted block {BlockIndex} hash {PersistedHash} != received hash {ReceivedHash}",
                            actor, block.Index, block.Hash, receivedBlock.Hash);
                        actor.Tell(Tcp.Abort.Instance);
                    }
                }
            }
        }

        protected override void OnReceive(object message)
        {
            // Log high-level message reception if needed, or specific messages below
            // _log.Verbose("TaskManager received message: {MessageType}", message.GetType().Name);
            switch (message)
            {
                case Register register:
                    var startHeight = register.Version.Capabilities.OfType<FullNodeCapability>().FirstOrDefault()?.StartHeight ?? 0;
                    _log.Information("Registering new session for {Sender} (Version: StartHeight={StartHeight}, UserAgent: '{UserAgent}')",
                        Sender, startHeight, register.Version.UserAgent);
                    OnRegister(register.Version);
                    break;
                case Update update:
                    _log.Verbose("Updating session for {Sender}, LastBlockIndex={LastBlockIndex}", Sender, update.LastBlockIndex);
                    OnUpdate(update);
                    break;
                case NewTasks tasks:
                    // Logging done within OnNewTasks
                    OnNewTasks(tasks.Payload);
                    break;
                case RestartTasks restart:
                    _log.Information("Restarting tasks for {HashCount} inventories ({InvType})", restart.Payload.Hashes.Length, restart.Payload.Type);
                    OnRestartTasks(restart.Payload);
                    break;
                case Header[] headers:
                    _log.Verbose("Received {HeaderCount} Headers from {Sender}", headers.Length, Sender);
                    OnHeaders(headers);
                    break;
                case IInventory inventory:
                    _log.Verbose("Received completed inventory {InvType} {InvHash} from {Sender}", inventory.InventoryType, inventory.Hash, Sender);
                    OnTaskCompleted(inventory);
                    break;
                case Blockchain.PersistCompleted pc:
                    // Logging done within OnPersistCompleted
                    OnPersistCompleted(pc.Block);
                    break;
                case Blockchain.RelayResult rr:
                    if (rr.Inventory is Block invalidBlock && rr.Result == VerifyResult.Invalid)
                        OnInvalidBlock(invalidBlock);
                    // else: Other relay results ignored by TaskManager
                    break;
                case Timer _:
                    _log.Verbose("TaskManager timer tick");
                    OnTimer();
                    break;
                case Terminated terminated:
                    // Logging done within OnTerminated
                    OnTerminated(terminated.ActorRef);
                    break;
                default:
                    // _log.Warning("TaskManager received unknown message type: {MessageType}", message.GetType().Name);
                    Unhandled(message);
                    break;
            }
        }

        private void OnRegister(VersionPayload version)
        {
            Context.Watch(Sender);
            TaskSession session = new(version);
            sessions.Add(Sender, session);
            _log.Debug("Session registered for {Sender}. Total sessions: {SessionCount}", Sender, sessions.Count);
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
            {
                if (globalInvTasks.Remove(hash))
                    _log.Verbose("Removed global task for hash {InvHash} due to restart", hash);
                // We don't have enough info here to decrement globalIndexTasks correctly if it was a block
            }
            foreach (InvPayload group in InvPayload.CreateGroup(payload.Type, payload.Hashes))
            {
                _log.Debug("Requesting {InvType} group with {HashCount} hashes due to restart", group.Type, group.Hashes.Length);
                system.LocalNode.Tell(Message.Create(MessageCommand.GetData, group));
            }
        }

        private void OnTaskCompleted(IInventory inventory)
        {
            Block block = inventory as Block;
            _log.Verbose("Task completed for inventory {InvType} {InvHash}", inventory.InventoryType, inventory.Hash);
            knownHashes.Add(inventory.Hash);
            if (globalInvTasks.Remove(inventory.Hash))
                _log.Verbose("Removed global task for hash {InvHash}", inventory.Hash);
            // else

                // _log.Warning("Completed task for hash {InvHash} was not in globalInvTasks?", inventory.Hash);

            if (block is not null)
            {
                if (globalIndexTasks.Remove(block.Index))
                    _log.Verbose("Removed global task for index {BlockIndex}", block.Index);
                // else: Index task might not have been created if block was received directly
            }

            foreach (TaskSession ms in sessions.Values)
                ms.AvailableTasks.Remove(inventory.Hash);

            if (sessions.TryGetValue(Sender, out TaskSession session))
            {
                bool taskRemoved = session.InvTasks.Remove(inventory.Hash);
                _log.Verbose("Removed InvTask {InvHash} from sender {Sender} session? {Removed}", inventory.Hash, Sender, taskRemoved);

                if (block is not null)
                {
                    bool indexTaskRemoved = session.IndexTasks.Remove(block.Index);
                    _log.Verbose("Removed IndexTask {BlockIndex} from sender {Sender} session? {Removed}", block.Index, Sender, indexTaskRemoved);

                    if (session.ReceivedBlock.TryGetValue(block.Index, out var block_old))
                    {
                        if (block.Hash != block_old.Hash)
                        {
                            _log.Warning("Disconnecting peer {Sender}: Received block {BlockIndex} hash {ReceivedHash} != previously received hash {OldHash}",
                                Sender, block.Index, block.Hash, block_old.Hash);
                            Sender.Tell(Tcp.Abort.Instance);
                            return;
                        }
                        // else: Already received this block, waiting for persist
                        _log.Verbose("Duplicate block {BlockIndex} received from {Sender}", block.Index, Sender);
                    }
                    else
                    {
                        _log.Debug("Storing received block {BlockIndex} from {Sender}, awaiting persistence", block.Index, Sender);
                        session.ReceivedBlock.Add(block.Index, block);
                    }
                }
                else // Not a block, request more tasks immediately
                {
                    _log.Verbose("Requesting more tasks for {Sender} after non-block task completion", Sender);
                    RequestTasks(Sender, session);
                }
            }
            else
            {
                _log.Warning("Received TaskCompleted from unknown sender {Sender} for inventory {InvHash}", Sender, inventory.Hash);
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
            if (value >= MaxConcurrentTasks)
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
            if (value >= MaxConcurrentTasks)
                return false;

            globalIndexTasks[index] = value + 1;
            return true;
        }

        private void OnTerminated(IActorRef actor)
        {
            if (!sessions.TryGetValue(actor, out TaskSession session))
            {
                _log.Warning("Received Terminated for unknown actor {ActorRef}", actor);
                return;
            }
            _log.Information("Session terminated for {ActorRef}. SessionTaskCount={TaskCount}, IndexTaskCount={IndexCount}, AvailableTaskCount={AvailableCount}",
                actor, session.InvTasks.Count, session.IndexTasks.Count, session.AvailableTasks.Count);

            foreach (UInt256 hash in session.InvTasks.Keys)
                DecrementGlobalTask(hash);
            foreach (uint index in session.IndexTasks.Keys)
                DecrementGlobalTask(index);
            sessions.Remove(actor);
            _log.Debug("Removed session for {ActorRef}. Total sessions: {SessionCount}", actor, sessions.Count);
        }

        private void OnTimer()
        {
            _log.Verbose("TaskManager timer tick");
            var now = TimeProvider.Current.UtcNow;
            int invTasksTimedOut = 0;
            int indexTasksTimedOut = 0;

            foreach (TaskSession session in sessions.Values)
            {
                // Count timed out items before removing
                var timedOutInvTasks = session.InvTasks.Where(p => now - p.Value > TaskTimeout).ToList();
                invTasksTimedOut += timedOutInvTasks.Count;
                foreach (var p in timedOutInvTasks)
                {
                    var actorRef = sessions.FirstOrDefault(kv => kv.Value == session).Key;
                    _log.Warning("Inventory task {InvHash} timed out for peer {Sender}", p.Key, actorRef ?? ActorRefs.Nobody);
                    session.InvTasks.Remove(p.Key);
                    DecrementGlobalTask(p.Key);
                }

                var timedOutIndexTasks = session.IndexTasks.Where(p => now - p.Value > TaskTimeout).ToList();
                indexTasksTimedOut += timedOutIndexTasks.Count;
                foreach (var p in timedOutIndexTasks)
                {
                    var actorRef = sessions.FirstOrDefault(kv => kv.Value == session).Key;
                    _log.Warning("Index task {BlockIndex} timed out for peer {Sender}", p.Key, actorRef ?? ActorRefs.Nobody);
                    session.IndexTasks.Remove(p.Key);
                    DecrementGlobalTask(p.Key);
                }
            }
            if (invTasksTimedOut > 0) _log.Information("Removed {Count} timed out inventory tasks", invTasksTimedOut);
            if (indexTasksTimedOut > 0) _log.Information("Removed {Count} timed out index tasks", indexTasksTimedOut);

            // Request new tasks for all sessions
            _log.Verbose("Timer triggering RequestTasks for all {SessionCount} sessions", sessions.Count);
            foreach (var (actor, session) in sessions)
                RequestTasks(actor, session);
        }

        protected override void PostStop()
        {
            _log.Information("TaskManager stopping...");
            timer?.Cancel(); // Use safe cancel
            _log.Information("TaskManager stopped.");
            timer.CancelIfNotNull();
            base.PostStop();
        }

        /// <summary>
        /// Gets a <see cref="Akka.Actor.Props"/> object used for creating the <see cref="TaskManager"/> actor.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the <see cref="TaskManager"/>.</param>
        /// <returns>The <see cref="Akka.Actor.Props"/> object used for creating the <see cref="TaskManager"/> actor.</returns>
        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new TaskManager(system)).WithMailbox("task-manager-mailbox");
        }

        private void RequestTasks(IActorRef remoteNode, TaskSession session)
        {
            if (session.HasTooManyTasks) return;

            var snapshot = system.StoreView;

            // If there are pending tasks of InventoryType.Block we should process them
            if (session.AvailableTasks.Count > 0)
            {
                session.AvailableTasks.Remove(knownHashes);
                // Search any similar hash that is on Singleton's knowledge, which means, on the way or already processed
                session.AvailableTasks.RemoveWhere(p => NativeContract.Ledger.ContainsBlock(snapshot, p));
                HashSet<UInt256> hashes = new(session.AvailableTasks);
                if (hashes.Count > 0)
                {
                    hashes.RemoveWhere(p => !IncrementGlobalTask(p));
                    session.AvailableTasks.Remove(hashes);

                    foreach (UInt256 hash in hashes)
                        session.InvTasks[hash] = DateTime.UtcNow;

                    foreach (InvPayload group in InvPayload.CreateGroup(InventoryType.Block, hashes))
                        remoteNode.Tell(Message.Create(MessageCommand.GetData, group));
                    return;
                }
            }

            uint currentHeight = Math.Max(NativeContract.Ledger.CurrentIndex(snapshot), lastSeenPersistedIndex);
            uint headerHeight = system.HeaderCache.Last?.Index ?? currentHeight;
            // When the number of AvailableTasks is no more than 0,
            // no pending tasks of InventoryType.Block, it should process pending the tasks of headers
            // If not HeaderTask pending to be processed it should ask for more Blocks
            if ((!HasHeaderTask || globalInvTasks[HeaderTaskHash] < MaxConcurrentTasks) && headerHeight < session.LastBlockIndex && !system.HeaderCache.Full)
            {
                session.InvTasks[HeaderTaskHash] = DateTime.UtcNow;
                IncrementGlobalTask(HeaderTaskHash);
                remoteNode.Tell(Message.Create(MessageCommand.GetHeaders, GetBlockByIndexPayload.Create(headerHeight + 1)));
            }
            else if (currentHeight < session.LastBlockIndex)
            {
                uint startHeight = currentHeight + 1;
                while (globalIndexTasks.ContainsKey(startHeight) || session.ReceivedBlock.ContainsKey(startHeight)) { startHeight++; }
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
        public TaskManagerMailbox(Settings settings, Config config)
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
            if (message is not TaskManager.NewTasks tasks) return false;
            // Remove duplicate tasks
            if (queue.OfType<TaskManager.NewTasks>().Any(x => x.Payload.Type == tasks.Payload.Type && x.Payload.Hashes.SequenceEqual(tasks.Payload.Hashes))) return true;
            return false;
        }
    }
}
