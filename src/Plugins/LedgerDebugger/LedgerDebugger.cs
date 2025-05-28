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
    public class LedgerDebugger : Plugin, ICommittingHandler, IDisposable
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
        public LedgerDebugger()
        {
            try
            {
                // Subscribe to blockchain committing events
                Blockchain.Committing += ((ICommittingHandler)this).Blockchain_Committing_Handler;

                Log("LedgerDebugger plugin initialized successfully");
            }
            catch (Exception ex)
            {
                Log($"Failed to initialize LedgerDebugger: {ex.Message}", LogLevel.Error);
                throw;
            }
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

                // Validate settings
                ValidateSettings();

                // Initialize block read set storage
                string blockReadSetPath = Settings.Default?.Path ?? throw new InvalidOperationException("Storage path not configured");
                blockReadSetPath = System.IO.Path.GetFullPath(blockReadSetPath);

                // Ensure storage directory exists
                var storageDir = System.IO.Path.GetDirectoryName(blockReadSetPath);
                if (!string.IsNullOrEmpty(storageDir) && !Directory.Exists(storageDir))
                {
                    Directory.CreateDirectory(storageDir);
                    Log($"Created storage directory: {storageDir}");
                }

                _blockReadSetStorage = new BlockReadSetStorage(
                    blockReadSetPath,
                    Settings.Default.StoreProvider,
                    Settings.Default.MaxReadSetsToKeep);
                Log($"Block ReadSetStorage initialized at {blockReadSetPath}");
                Log($"Max read sets to keep: {Settings.Default.MaxReadSetsToKeep}");

                Log($"LedgerDebugger loaded for system: {system.Settings.Network}");
            }
            catch (Exception ex)
            {
                Log($"Failed to initialize LedgerDebugger: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Validates the plugin settings.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if settings are invalid</exception>
        private static void ValidateSettings()
        {
            if (Settings.Default == null)
                throw new InvalidOperationException("Settings not loaded");

            if (string.IsNullOrWhiteSpace(Settings.Default.Path))
                throw new InvalidOperationException("Storage path cannot be null or empty");

            if (Settings.Default.MaxReadSetsToKeep < 0)
                throw new InvalidOperationException("MaxReadSetsToKeep cannot be negative");
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
                // Validate prerequisites
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

                // Validate block index
                var currentHeight = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);
                if (blockIndex > currentHeight)
                {
                    Log($"Block index {blockIndex} is beyond current blockchain height {currentHeight}", LogLevel.Error);
                    return;
                }

                Log($"Attempting to re-execute block {blockIndex}...");

                if (!_blockReadSetStorage.TryGet(blockIndex, out var readSet))
                {
                    Log($"No read set found for block {blockIndex}. Block may not have been captured during execution.", LogLevel.Error);
                    return;
                }

                if (readSet == null || readSet.Count == 0)
                {
                    Log($"Empty read set for block {blockIndex}. Block may have had no state reads.", LogLevel.Warning);
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

        /// <summary>
        /// Console command to show storage statistics and health information.
        /// </summary>
        [ConsoleCommand("ledger debug info", Category = "Ledger Debug", Description = "Show LedgerDebugger storage statistics and efficiency metrics")]
        private void ShowDebugInfo()
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

                var currentHeight = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);
                var metrics = _blockReadSetStorage.GetMetrics();

                Log("=== LedgerDebugger Information ===");
                Log($"Storage Path: {Settings.Default?.Path ?? "Not configured"}");
                Log($"Store Provider: {Settings.Default?.StoreProvider ?? "Not configured"}");
                Log($"Max Read Sets to Keep: {Settings.Default?.MaxReadSetsToKeep ?? 0}");
                Log($"Current Blockchain Height: {currentHeight}");

                Log("\n=== Storage Efficiency Metrics ===");
                Log($"Store Attempts: {metrics.StoreAttempts:N0}");
                Log($"Retrieve Attempts: {metrics.RetrieveAttempts:N0}");
                Log($"Cache Hit Rate: {metrics.CacheHitRate:P2}");
                Log($"Compression Rate: {metrics.CompressionRate:P2}");
                Log($"Deduplication Rate: {metrics.DeduplicationRate:P2}");
                Log($"Small Values: {metrics.SmallValues:N0}");
                Log($"Stored Values: {metrics.StoredValues:N0}");
                Log($"Compressed Values: {metrics.CompressedValues:N0}");
                Log($"Deduplicated Values: {metrics.DeduplicatedValues:N0}");
                Log($"Total Storage Bytes: {FormatBytes(metrics.TotalStorageBytes)}");

                // Count available read sets
                uint availableReadSets = 0;
                uint oldestBlock = currentHeight > 100 ? currentHeight - 100 : 0; // Check last 100 blocks

                for (uint i = oldestBlock; i <= currentHeight; i++)
                {
                    if (_blockReadSetStorage.TryGet(i, out var readSet) && readSet != null && readSet.Count > 0)
                    {
                        availableReadSets++;
                    }
                }

                Log($"\n=== Read Set Availability ===");
                Log($"Available Read Sets (last 100 blocks): {availableReadSets}");
                Log($"Coverage: {(double)availableReadSets / Math.Min(100, currentHeight + 1):P1}");
                Log("=====================================");
            }
            catch (Exception ex)
            {
                Log($"Error retrieving debug info: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Console command to perform storage maintenance and optimization.
        /// </summary>
        [ConsoleCommand("ledger debug maintenance", Category = "Ledger Debug", Description = "Perform storage maintenance and optimization")]
        private void PerformMaintenance()
        {
            try
            {
                if (_blockReadSetStorage == null)
                {
                    Log("Block read set storage not initialized", LogLevel.Error);
                    return;
                }

                Log("Performing storage maintenance...");
                _blockReadSetStorage.PerformMaintenance();
                Log("Storage maintenance completed successfully");
            }
            catch (Exception ex)
            {
                Log($"Error during maintenance: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Formats bytes into human-readable format.
        /// </summary>
        /// <param name="bytes">Number of bytes</param>
        /// <returns>Formatted string</returns>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:N1} {suffixes[counter]}";
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
                // Validate inputs
                if (system == null || block == null || snapshot == null)
                {
                    Log("Invalid parameters in Blockchain_Committing_Handler", LogLevel.Warning);
                    return;
                }

                // Skip if storage is not initialized
                if (_blockReadSetStorage == null)
                {
                    // Only log this once to avoid spam
                    return;
                }

                // Skip if snapshot has no read set
                if (snapshot.ReadSet == null || snapshot.ReadSet.Count == 0)
                {
                    // This is normal for blocks with no state reads
                    return;
                }

                // Skip if block has no transactions (genesis block, etc.)
                if (block.Transactions.Length == 0)
                {
                    return;
                }

                // Store the read set
                var success = _blockReadSetStorage.Add(block.Index, snapshot.ReadSet);
                if (success)
                {
                    Log($"Stored read set for block {block.Index} with {snapshot.ReadSet.Count} read operations");
                }
                else
                {
                    Log($"Failed to store read set for block {block.Index}", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                Log($"Error in Blockchain_Committing_Handler for block {block?.Index}: {ex.Message}", LogLevel.Error);
                // Don't rethrow - we don't want to break the blockchain commit process
            }
        }

        #endregion
    }
}
