// Copyright (C) 2015-2025 The Neo Project.
//
// LedgerDebugger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.IEventHandlers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.Plugins.LedgerDebugger
{
    /// <summary>
    /// Plugin for debugging NEO ledger by recording and replaying block execution states.
    /// Implements <see cref="ICommittingHandler"/> to capture state during block execution.
    /// </summary>
    public abstract class LedgerDebugger : Plugin, ICommittingHandler, IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Storage for block read sets
        /// </summary>
        private BlockReadSetStorage? _blockReadSetStorage;

        /// <summary>
        /// Reference to the NEO system
        /// </summary>
        private NeoSystem? _neoSystem;

        #endregion

        #region Plugin Properties

        /// <inheritdoc/>
        public override string Name => "LedgerDebugger";

        /// <inheritdoc/>
        public override string Description => "Records block readsets for debugging and reproducibility";

        /// <inheritdoc/>
        public override string ConfigFile => System.IO.Path.Combine(RootPath, "LedgerDebugger.json");

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LedgerDebugger"/> class.
        /// </summary>
        protected LedgerDebugger()
        {
            // Subscribe to blockchain committing events
            Blockchain.Committing += ((ICommittingHandler)this).Blockchain_Committing_Handler;
        }

        #endregion

        #region Plugin Lifecycle Methods

        /// <inheritdoc/>
        protected override void Configure()
        {
            // Load settings from configuration
            Settings.Load(GetConfiguration());
        }

        /// <inheritdoc/>
        protected internal override void OnSystemLoaded(NeoSystem system)
        {
            try
            {
                // Store reference to NeoSystem
                _neoSystem = system ?? throw new ArgumentNullException(nameof(system));

                // Initialize block read set storage
                string blockReadSetPath = Settings.Default?.Path ?? throw new InvalidOperationException("Storage path not configured");
                blockReadSetPath = System.IO.Path.GetFullPath(blockReadSetPath);

                _blockReadSetStorage = new BlockReadSetStorage(blockReadSetPath);
                Log($"Block ReadSetStorage initialized at {blockReadSetPath}");

                Log($"LedgerDebugger loaded for system: {system.Settings.Network}");
            }
            catch (Exception ex)
            {
                Log($"Failed to initialize LedgerDebugger: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            try
            {
                // Unsubscribe from blockchain events
                Blockchain.Committing -= ((ICommittingHandler)this).Blockchain_Committing_Handler;

                // Dispose the storage
                (_blockReadSetStorage as IDisposable)?.Dispose();

                _blockReadSetStorage = null;
                _neoSystem = null;
            }
            finally
            {
                base.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region Console Commands

        /// <summary>
        /// Console command to re-execute a block with the previously captured state.
        /// </summary>
        /// <param name="blockIndex">The index of the block to re-execute</param>
        /// <param name="txHash">Optional transaction hash to highlight during execution</param>
        [ConsoleCommand("execute block", Category = "Ledger Debug", Description = "Re-execute a block using captured state")]
        private void ExecuteBlock(uint blockIndex, UInt256? txHash = null)
        {
            try
            {
                if (_blockReadSetStorage == null)
                {
                    Log("Block read set storage not initialized", LogLevel.Error);
                    return;
                }

                if (_neoSystem == null)
                {
                    Log("NEO system not initialized", LogLevel.Error);
                    return;
                }

                if (!_blockReadSetStorage.TryGet(blockIndex, out var readSet))
                {
                    Log($"No read set found for block {blockIndex}", LogLevel.Error);
                    return;
                }

                if (readSet == null || readSet.Count == 0)
                {
                    Log($"Empty read set for block {blockIndex}", LogLevel.Error);
                    return;
                }

                // Create a memory store with the read set data
                var memStore = new MemoryStore();
                foreach (var keyValuePair in readSet)
                {
                    memStore.Put(keyValuePair.Key, keyValuePair.Value);
                }

                // Get the block from the ledger
                var block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, blockIndex);
                if (block == null)
                {
                    Log($"Block {blockIndex} not found in the ledger", LogLevel.Error);
                    return;
                }

                // Create a snapshot from the memory store
                using var snapshot = new StoreCache(memStore.GetSnapshot());

                // Execute OnPersist
                Log($"Executing OnPersist for block {blockIndex}");
                ExecuteOnPersist(snapshot, block);

                // Execute transactions
                Log($"Executing {block.Transactions.Length} transactions for block {blockIndex}");
                ExecuteTransactions(snapshot, block, txHash);

                Log($"Successfully re-executed block {blockIndex}");
            }
            catch (Exception ex)
            {
                Log($"Error executing block {blockIndex}: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Executes the OnPersist phase for a block.
        /// </summary>
        /// <param name="snapshot">The data cache snapshot</param>
        /// <param name="block">The block to execute</param>
        /// <exception cref="InvalidOperationException">Thrown if execution fails</exception>
        private void ExecuteOnPersist(DataCache snapshot, Block block)
        {
            using var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, block, _neoSystem!.Settings, 0);
            var sb = new ScriptBuilder();
            sb.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            engine.LoadScript(sb.ToArray());

            if (engine.Execute() != VMState.HALT)
            {
                if (engine.FaultException != null)
                    throw engine.FaultException;

                throw new InvalidOperationException("OnPersist execution failed");
            }
        }

        /// <summary>
        /// Executes the transactions in a block.
        /// </summary>
        /// <param name="snapshot">The data cache snapshot</param>
        /// <param name="block">The block containing transactions</param>
        /// <param name="txHash">Optional transaction hash to highlight</param>
        private void ExecuteTransactions(DataCache snapshot, Block block, UInt256? txHash)
        {
            var clonedSnapshot = snapshot.CloneCache();

            foreach (var tx in block.Transactions)
            {
                if (tx.Hash == txHash)
                {
                    Log("Start the execution of target transaction", LogLevel.Info);
                }

                using var engine = ApplicationEngine.Create(
                    TriggerType.Application,
                    tx,
                    clonedSnapshot,
                    block,
                    _neoSystem!.Settings,
                    tx.SystemFee
                );

                engine.LoadScript(tx.Script);

                if (engine.Execute() == VMState.HALT)
                {
                    clonedSnapshot.Commit();
                }
                else
                {
                    // If execution fails, reset the snapshot clone
                    clonedSnapshot = snapshot.CloneCache();
                }
            }

            // Commit changes to the snapshot
            snapshot.Commit();
        }

        #endregion

        #region ICommittingHandler Implementation

        /// <summary>
        /// Handler for blockchain committing events. Captures the read set during block execution.
        /// </summary>
        void ICommittingHandler.Blockchain_Committing_Handler(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            try
            {
                // Skip if snapshot has no read set or block has no transactions
                if (snapshot is not { ReadSet.Count: > 0 } || block.Transactions.Length == 0)
                {
                    return;
                }

                try
                {
                    if (_blockReadSetStorage != null)
                    {
                        _blockReadSetStorage.Add(block.Index, snapshot.ReadSet);
                        Log($"Stored readset for block {block.Index} with {snapshot.ReadSet.Count} read operations");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error storing readset for block {block.Index}: {ex.Message}", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"Error in Blockchain_Committing_Handler: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion
    }
}
