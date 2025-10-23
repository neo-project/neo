// Copyright (C) 2015-2025 The Neo Project.
//
// Blockchain.cs file belongs to the neo project and is free
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
using Neo.IO.Actors;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Ledger
{
    public delegate void CommittingHandler(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList);
    public delegate void CommittedHandler(NeoSystem system, Block block);

    /// <summary>
    /// Actor used to verify and relay <see cref="IInventory"/>.
    /// </summary>
    public sealed partial class Blockchain : UntypedActor
    {
        /// <summary>
        /// Sent by the <see cref="Blockchain"/> when a smart contract is executed.
        /// </summary>
        public partial class ApplicationExecuted { }

        /// <summary>
        /// Sent by the <see cref="Blockchain"/> when a <see cref="Network.P2P.Payloads.Block"/> is persisted.
        /// </summary>
        public class PersistCompleted
        {
            /// <summary>
            /// The <see cref="Network.P2P.Payloads.Block"/> that is persisted.
            /// </summary>
            public Block Block { get; init; }
        }

        /// <summary>
        /// Sent to the <see cref="Blockchain"/> when importing blocks.
        /// </summary>
        public class Import
        {
            /// <summary>
            /// The blocks to be imported.
            /// </summary>
            public IEnumerable<Block> Blocks { get; init; }

            /// <summary>
            /// Indicates whether the blocks need to be verified when importing.
            /// </summary>
            public bool Verify { get; init; } = true;
        }

        /// <summary>
        /// Sent by the <see cref="Blockchain"/> when the import is complete.
        /// </summary>
        public class ImportCompleted { }

        /// <summary>
        /// Sent to the <see cref="Blockchain"/> when the consensus is filling the memory pool.
        /// </summary>
        public class FillMemoryPool
        {
            /// <summary>
            /// The transactions to be sent.
            /// </summary>
            public IEnumerable<Transaction> Transactions { get; init; }
        }

        /// <summary>
        /// Sent by the <see cref="Blockchain"/> when the memory pool is filled.
        /// </summary>
        public class FillCompleted { }

        /// <summary>
        /// Sent to the <see cref="Blockchain"/> when inventories need to be re-verified.
        /// </summary>
        public class Reverify
        {
            /// <summary>
            /// The inventories to be re-verified.
            /// </summary>
            public IReadOnlyList<IInventory> Inventories { get; init; }
        }

        /// <summary>
        /// Sent by the <see cref="Blockchain"/> when an <see cref="IInventory"/> is relayed.
        /// </summary>
        public class RelayResult
        {
            /// <summary>
            /// The <see cref="IInventory"/> that is relayed.
            /// </summary>
            public IInventory Inventory { get; init; }
            /// <summary>
            /// The result.
            /// </summary>
            public VerifyResult Result { get; init; }
        }

        internal class Initialize { }
        private class UnverifiedBlocksList
        {
            public List<Block> Blocks { get; } = [];
            public HashSet<IActorRef> Nodes { get; } = [];
        }

        public static event CommittingHandler Committing;
        public static event CommittedHandler Committed;

        private static readonly Script s_onPersistScript, s_postPersistScript;
        private const int MaxTxToReverifyPerIdle = 10;
        private readonly NeoSystem _system;
        private readonly Dictionary<UInt256, Block> _blockCache = [];
        private readonly Dictionary<uint, UnverifiedBlocksList> _blockCacheUnverified = [];
        private ImmutableHashSet<UInt160> _extensibleWitnessWhiteList;

        static Blockchain()
        {
            using (ScriptBuilder sb = new())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
                s_onPersistScript = sb.ToArray();
            }
            using (ScriptBuilder sb = new())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
                s_postPersistScript = sb.ToArray();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Blockchain"/> class.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the <see cref="Blockchain"/>.</param>
        public Blockchain(NeoSystem system)
        {
            _system = system;
        }

        private void OnImport(IEnumerable<Block> blocks, bool verify)
        {
            var currentHeight = NativeContract.Ledger.CurrentIndex(_system.StoreView);
            foreach (var block in blocks)
            {
                if (block.Index <= currentHeight) continue;
                if (block.Index != currentHeight + 1)
                    throw new InvalidOperationException();
                if (verify && !block.Verify(_system.Settings, _system.StoreView))
                    throw new InvalidOperationException();
                Persist(block);
                ++currentHeight;
            }
            Sender.Tell(new ImportCompleted());
        }

        private void AddUnverifiedBlockToCache(Block block)
        {
            // Check if any block proposal for height `block.Index` exists
            if (!_blockCacheUnverified.TryGetValue(block.Index, out var list))
            {
                // There are no blocks, a new UnverifiedBlocksList is created and, consequently, the current block is added to the list
                list = new UnverifiedBlocksList();
                _blockCacheUnverified.Add(block.Index, list);
            }
            else
            {
                // Check if any block with the hash being added already exists on possible candidates to be processed
                foreach (var unverifiedBlock in list.Blocks)
                {
                    if (block.Hash == unverifiedBlock.Hash)
                        return;
                }

                if (!list.Nodes.Add(Sender))
                {
                    // Same index with different hash
                    Sender.Tell(Tcp.Abort.Instance);
                    return;
                }
            }

            list.Blocks.Add(block);
        }

        private void OnFillMemoryPool(IEnumerable<Transaction> transactions)
        {
            // Invalidate all the transactions in the memory pool, to avoid any failures when adding new transactions.
            _system.MemPool.InvalidateAllTransactions();

            var snapshot = _system.StoreView;
            var mtb = _system.GetMaxTraceableBlocks();

            // Add the transactions to the memory pool
            foreach (var tx in transactions)
            {
                if (NativeContract.Ledger.ContainsTransaction(snapshot, tx.Hash))
                    continue;
                if (NativeContract.Ledger.ContainsConflictHash(snapshot, tx.Hash, tx.Signers.Select(s => s.Account), mtb))
                    continue;
                // First remove the tx if it is unverified in the pool.
                _system.MemPool.TryRemoveUnVerified(tx.Hash, out _);
                // Add to the memory pool
                _system.MemPool.TryAdd(tx, snapshot);
            }
            // Transactions originally in the pool will automatically be reverified based on their priority.

            Sender.Tell(new FillCompleted());
        }

        private void OnInitialize()
        {
            if (!NativeContract.Ledger.Initialized(_system.StoreView))
                Persist(_system.GenesisBlock);
            Sender.Tell(new object());
        }

        private void OnInventory(IInventory inventory, bool relay = true)
        {
            var result = inventory switch
            {
                Block block => OnNewBlock(block),
                Transaction transaction => OnNewTransaction(transaction),
                ExtensiblePayload payload => OnNewExtensiblePayload(payload),
                _ => throw new NotSupportedException()
            };
            if (result == VerifyResult.Succeed && relay)
            {
                _system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = inventory });
            }
            SendRelayResult(inventory, result);
        }

        private VerifyResult OnNewBlock(Block block)
        {
            if (!block.TryGetHash(out var blockHash)) return VerifyResult.Invalid;

            var snapshot = _system.StoreView;
            var currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);
            var headerHeight = _system.HeaderCache.Last?.Index ?? currentHeight;
            if (block.Index <= currentHeight)
                return VerifyResult.AlreadyExists;
            if (block.Index - 1 > headerHeight)
            {
                AddUnverifiedBlockToCache(block);
                return VerifyResult.UnableToVerify;
            }
            if (block.Index == headerHeight + 1)
            {
                if (!block.Verify(_system.Settings, snapshot, _system.HeaderCache))
                    return VerifyResult.Invalid;
            }
            else
            {
                var header = _system.HeaderCache[block.Index];
                if (header == null || !blockHash.Equals(header.Hash))
                    return VerifyResult.Invalid;
            }
            _blockCache.TryAdd(blockHash, block);
            if (block.Index == currentHeight + 1)
            {
                var blockPersist = block;
                var blocksToPersistList = new List<Block>();
                while (true)
                {
                    blocksToPersistList.Add(blockPersist);
                    if (blockPersist.Index + 1 > headerHeight) break;
                    var header = _system.HeaderCache[blockPersist.Index + 1];
                    if (header == null) break;
                    if (!_blockCache.TryGetValue(header.Hash, out blockPersist)) break;
                }

                var blocksPersisted = 0;
                var timePerBlock = _system.GetTimePerBlock();
                var extraRelayingBlocks = timePerBlock.TotalMilliseconds < ProtocolSettings.Default.MillisecondsPerBlock
                    ? (ProtocolSettings.Default.MillisecondsPerBlock - (uint)timePerBlock.TotalMilliseconds) / 1000
                    : 0;
                foreach (var blockToPersist in blocksToPersistList)
                {
                    _blockCacheUnverified.Remove(blockToPersist.Index);
                    Persist(blockToPersist);

                    if (blocksPersisted++ < blocksToPersistList.Count - (2 + extraRelayingBlocks)) continue;
                    // Empirically calibrated for relaying the most recent 2 blocks persisted with 15s network
                    // Increase in the rate of 1 block per second in configurations with faster blocks

                    if (blockToPersist.Index + 99 >= headerHeight)
                        _system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = blockToPersist });
                }
                if (_blockCacheUnverified.TryGetValue(currentHeight + 1, out var unverifiedBlocks))
                {
                    foreach (var unverifiedBlock in unverifiedBlocks.Blocks)
                        Self.Tell(unverifiedBlock, ActorRefs.NoSender);
                    _blockCacheUnverified.Remove(block.Index + 1);
                }
            }
            else
            {
                if (block.Index + 99 >= headerHeight)
                    _system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = block });
                if (block.Index == headerHeight + 1)
                    _system.HeaderCache.Add(block.Header);
            }
            return VerifyResult.Succeed;
        }

        private void OnNewHeaders(Header[] headers)
        {
            if (!_system.HeaderCache.Full)
            {
                var snapshot = _system.StoreView;
                var headerHeight = _system.HeaderCache.Last?.Index ?? NativeContract.Ledger.CurrentIndex(snapshot);
                foreach (var header in headers)
                {
                    if (!header.TryGetHash(out _)) continue;
                    if (header.Index > headerHeight + 1) break;
                    if (header.Index < headerHeight + 1) continue;
                    if (!header.Verify(_system.Settings, snapshot, _system.HeaderCache)) break;
                    if (!_system.HeaderCache.Add(header)) break;
                    ++headerHeight;
                }
            }
            _system.TaskManager.Tell(headers, Sender);
        }

        private VerifyResult OnNewExtensiblePayload(ExtensiblePayload payload)
        {
            if (!payload.TryGetHash(out _)) return VerifyResult.Invalid;

            var snapshot = _system.StoreView;
            _extensibleWitnessWhiteList ??= UpdateExtensibleWitnessWhiteList(_system.Settings, snapshot);
            if (!payload.Verify(_system.Settings, snapshot, _extensibleWitnessWhiteList)) return VerifyResult.Invalid;
            _system.RelayCache.Add(payload);
            return VerifyResult.Succeed;
        }

        private VerifyResult OnNewTransaction(Transaction transaction)
        {
            if (!transaction.TryGetHash(out var hash)) return VerifyResult.Invalid;

            switch (_system.ContainsTransaction(hash))
            {
                case ContainsTransactionType.ExistsInPool: return VerifyResult.AlreadyInPool;
                case ContainsTransactionType.ExistsInLedger: return VerifyResult.AlreadyExists;
            }

            if (_system.ContainsConflictHash(hash, transaction.Signers.Select(s => s.Account))) return VerifyResult.HasConflicts;
            return _system.MemPool.TryAdd(transaction, _system.StoreView);
        }

        private void OnPreverifyCompleted(TransactionRouter.PreverifyCompleted task)
        {
            if (task.Result == VerifyResult.Succeed)
                OnInventory(task.Transaction, task.Relay);
            else
                SendRelayResult(task.Transaction, task.Result);
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Initialize:
                    OnInitialize();
                    break;
                case Import import:
                    OnImport(import.Blocks, import.Verify);
                    break;
                case FillMemoryPool fill:
                    OnFillMemoryPool(fill.Transactions);
                    break;
                case Header[] headers:
                    OnNewHeaders(headers);
                    break;
                case Block block:
                    OnInventory(block, false);
                    break;
                case Transaction tx:
                    OnTransaction(tx);
                    break;
                case IInventory inventory:
                    OnInventory(inventory);
                    break;
                case TransactionRouter.PreverifyCompleted task:
                    OnPreverifyCompleted(task);
                    break;
                case Reverify reverify:
                    foreach (var inventory in reverify.Inventories)
                        OnInventory(inventory, false);
                    break;
                case Idle _:
                    if (_system.MemPool.ReVerifyTopUnverifiedTransactionsIfNeeded(MaxTxToReverifyPerIdle, _system.StoreView))
                        Self.Tell(Idle.Instance, ActorRefs.NoSender);
                    break;
            }
        }

        private void OnTransaction(Transaction tx)
        {
            if (!tx.TryGetHash(out var hash))
            {
                SendRelayResult(tx, VerifyResult.Invalid);
                return;
            }

            switch (_system.ContainsTransaction(hash))
            {
                case ContainsTransactionType.ExistsInPool:
                    SendRelayResult(tx, VerifyResult.AlreadyInPool);
                    break;
                case ContainsTransactionType.ExistsInLedger:
                    SendRelayResult(tx, VerifyResult.AlreadyExists);
                    break;
                default:
                    {
                        if (_system.ContainsConflictHash(hash, tx.Signers.Select(s => s.Account)))
                            SendRelayResult(tx, VerifyResult.HasConflicts);
                        else _system.TxRouter.Forward(new TransactionRouter.Preverify(tx, true));
                        break;
                    }
            }
        }

        private void Persist(Block block)
        {
            using (var snapshot = _system.GetSnapshotCache())
            {
                var allApplicationExecuted = new List<ApplicationExecuted>();
                TransactionState[] transactionStates;
                using (var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, block, _system.Settings, 0))
                {
                    engine.LoadScript(s_onPersistScript);
                    if (engine.Execute() != VMState.HALT)
                    {
                        if (engine.FaultException != null)
                            throw engine.FaultException;
                        throw new InvalidOperationException();
                    }

                    var applicationExecuted = new ApplicationExecuted(engine);
                    Context.System.EventStream.Publish(applicationExecuted);

                    allApplicationExecuted.Add(applicationExecuted);
                    transactionStates = engine.GetState<TransactionState[]>();
                }

                var clonedSnapshot = snapshot.CloneCache();
                // Warning: Do not write into variable snapshot directly. Write into variable clonedSnapshot and commit instead.
                foreach (var transactionState in transactionStates)
                {
                    var tx = transactionState.Transaction;
                    using var engine = ApplicationEngine.Create(TriggerType.Application, tx, clonedSnapshot, block, _system.Settings, tx.SystemFee);
                    engine.LoadScript(tx.Script);
                    transactionState.State = engine.Execute();
                    if (transactionState.State == VMState.HALT)
                    {
                        clonedSnapshot.Commit();
                    }
                    else
                    {
                        clonedSnapshot = snapshot.CloneCache();
                    }

                    var applicationExecuted = new ApplicationExecuted(engine);
                    Context.System.EventStream.Publish(applicationExecuted);
                    allApplicationExecuted.Add(applicationExecuted);
                }

                using (var engine = ApplicationEngine.Create(TriggerType.PostPersist, null, snapshot, block, _system.Settings, 0))
                {
                    engine.LoadScript(s_postPersistScript);
                    if (engine.Execute() != VMState.HALT)
                    {
                        if (engine.FaultException != null)
                            throw engine.FaultException;
                        throw new InvalidOperationException();
                    }

                    var applicationExecuted = new ApplicationExecuted(engine);
                    Context.System.EventStream.Publish(applicationExecuted);
                    allApplicationExecuted.Add(applicationExecuted);
                }

                InvokeCommitting(_system, block, snapshot, allApplicationExecuted);
                snapshot.Commit();
            }

            InvokeCommitted(_system, block);
            _system.MemPool.UpdatePoolForBlockPersisted(block, _system.StoreView);
            _extensibleWitnessWhiteList = null;
            _blockCache.Remove(block.PrevHash);
            Context.System.EventStream.Publish(new PersistCompleted { Block = block });
            if (_system.HeaderCache.TryRemoveFirst(out var header))
                Debug.Assert(header.Index == block.Index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InvokeCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<ApplicationExecuted> applicationExecutedList)
        {
            InvokeHandlers(Committing?.GetInvocationList(), h => ((CommittingHandler)h)(system, block, snapshot, applicationExecutedList));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InvokeCommitted(NeoSystem system, Block block)
        {
            InvokeHandlers(Committed?.GetInvocationList(), h => ((CommittedHandler)h)(system, block));
        }

        private static void InvokeHandlers(Delegate[] handlers, Action<Delegate> handlerAction)
        {
            if (handlers == null) return;

            foreach (var handler in handlers)
            {
                try
                {
                    // skip stopped plugin.
                    if (handler.Target is Plugin { IsStopped: true })
                    {
                        continue;
                    }

                    handlerAction(handler);
                }
                catch (Exception ex) when (handler.Target is Plugin plugin)
                {
                    Utility.Log(nameof(plugin.Name), LogLevel.Error, $"{plugin.Name} exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                    switch (plugin.ExceptionPolicy)
                    {
                        case UnhandledExceptionPolicy.StopNode:
                            throw;
                        case UnhandledExceptionPolicy.StopPlugin:
                            //Stop plugin on exception
                            plugin.IsStopped = true;
                            break;
                        case UnhandledExceptionPolicy.Ignore:
                            // Log the exception and continue with the next handler
                            break;
                        default:
                            throw new InvalidCastException($"The exception policy {plugin.ExceptionPolicy} is not valid.");
                    }
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="Akka.Actor.Props"/> object used for creating the <see cref="Blockchain"/> actor.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the <see cref="Blockchain"/>.</param>
        /// <returns>The <see cref="Akka.Actor.Props"/> object used for creating the <see cref="Blockchain"/> actor.</returns>
        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new Blockchain(system)).WithMailbox("blockchain-mailbox");
        }

        private void SendRelayResult(IInventory inventory, VerifyResult result)
        {
            RelayResult rr = new()
            {
                Inventory = inventory,
                Result = result
            };
            Sender.Tell(rr);
            Context.System.EventStream.Publish(rr);
        }

        private static ImmutableHashSet<UInt160> UpdateExtensibleWitnessWhiteList(ProtocolSettings settings, DataCache snapshot)
        {
            var currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);
            var builder = ImmutableHashSet.CreateBuilder<UInt160>();
            builder.Add(NativeContract.NEO.GetCommitteeAddress(snapshot));
            var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, settings.ValidatorsCount);
            builder.Add(Contract.GetBFTAddress(validators));
            builder.UnionWith(validators.Select(u => Contract.CreateSignatureRedeemScript(u).ToScriptHash()));
            var stateValidators = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.StateValidator, currentHeight);
            if (stateValidators.Length > 0)
            {
                builder.Add(Contract.GetBFTAddress(stateValidators));
                builder.UnionWith(stateValidators.Select(u => Contract.CreateSignatureRedeemScript(u).ToScriptHash()));
            }
            return builder.ToImmutable();
        }
    }

    internal class BlockchainMailbox : PriorityMailbox
    {
        public BlockchainMailbox(Settings settings, Config config)
            : base(settings, config)
        {
        }

        internal protected override bool IsHighPriority(object message)
        {
            return message switch
            {
                Header[] or Block or ExtensiblePayload or Terminated => true,
                _ => false,
            };
        }
    }
}
