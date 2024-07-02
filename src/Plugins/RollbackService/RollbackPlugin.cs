// Copyright (C) 2015-2024 The Neo Project.
//
// RollbackPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Neo.Plugins.RollbackService
{

    /// <summary>
    /// Plugin to allow Neo node to rollback to any specified block height.
    /// </summary>
    public class RollbackPlugin : Plugin
    {
        public const string RollbackPayloadCategory = "RollbackService";
        public override string Name => "RollbackService";
        public override string Description => "Allows the node to rollback to any specified height";
        public override string ConfigFile => System.IO.Path.Combine(RootPath, "config.json");

        internal static NeoSystem _system;

        public RollbackPlugin()
        {
            Blockchain.Committing += OnCommitting;
        }

        protected override void Configure()
        {
            try
            {
                Settings.Load(GetConfiguration());
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error($"Failed to load configuration: {ex.Message}");
            }
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            _system = system;
        }

        public override void Dispose()
        {
            base.Dispose();
            Blockchain.Committing -= OnCommitting;
        }

        /// <summary>
        /// Handles the committing event to save state changes for rollback.
        /// </summary>
        /// <param name="system">The NeoSystem instance.</param>
        /// <param name="block">The current block being committed.</param>
        /// <param name="snapshot">The data cache snapshot.</param>
        /// <param name="applicationExecutedList">The list of executed applications.</param>
        private void OnCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            if (system.Settings.Network != Settings.Default!.Network) return;
            SaveFallback(snapshot, block.Index);
        }

        /// <summary>
        /// Command to rollback the ledger to a specified block height.
        /// </summary>
        /// <param name="target">The target block height to rollback to.</param>
        [ConsoleCommand("rollback ledger", Category = "Blockchain Commands")]
        private void OnBlockFallbackCommand(uint targetHeight)
        {
            var height = NativeContract.Ledger.CurrentIndex(_system.StoreView);
            if (height < targetHeight)
            {
                ConsoleHelper.Error("Invalid fallback target height.");
                return;
            }

            OnBlockFallback(_system, targetHeight);
        }

        /// <summary>
        /// Saves the state changes to a persistent store for fallback.
        /// </summary>
        /// <param name="snapshot">The data cache snapshot.</param>
        /// <param name="blockId">The block index for which changes are being saved.</param>
        private static void SaveFallback(DataCache snapshot, uint blockId)
        {
            var changeSet = snapshot.GetChangeSet();
            using var memoryStream = new MemoryStream();
            foreach (var item in changeSet)
            {
                FallbackOperation operation = item.State switch
                {
                    TrackState.Deleted => FallbackOperation.Deleted,
                    TrackState.Added => FallbackOperation.Added,
                    TrackState.Changed => FallbackOperation.Changed,
                    _ => throw new InvalidOperationException("Invalid fallback operation")
                };

                var value = snapshot.TryGet(item.Key, true)?.Value.ToArray() ?? Array.Empty<byte>();

                var encoded = ChangeEncode(
                    blockId,
                    operation,
                    item.Key.Key.ToArray(),
                    operation != FallbackOperation.Added ? value : Array.Empty<byte>()
                );
                memoryStream.Write(BitConverter.GetBytes(encoded.Length), 0, 4);
                memoryStream.Write(encoded, 0, encoded.Length);
            }
            snapshot.Add(new StorageKey(Encoding.UTF8.GetBytes($"fallback{blockId}")), new StorageItem(memoryStream.ToArray()));
        }

        /// <summary>
        /// Rolls back the blockchain state to a specified height.
        /// </summary>
        /// <param name="system">The NeoSystem instance.</param>
        /// <param name="height">The target block height to rollback to.</param>
        private static void OnBlockFallback(NeoSystem system, uint height)
        {
            var snapshot = system.GetSnapshot();
            var currentIndex = NativeContract.Ledger.CurrentIndex(snapshot);
            if (currentIndex <= height) return;

            for (var i = currentIndex; i > height; i--)
            {
                using var snapshotFallback = system.GetSnapshot();
                var fallbackKey = new StorageKey(Encoding.UTF8.GetBytes($"fallback{i}"));
                var fallbackItem = snapshotFallback.TryGet(fallbackKey)?.Value.ToArray();
                if (fallbackItem == null) continue;

                using (var memoryStream = new MemoryStream(fallbackItem))
                using (var reader = new BinaryReader(memoryStream))
                {
                    while (memoryStream.Position < memoryStream.Length)
                    {
                        var length = reader.ReadInt32();
                        if (length < 0 || length > memoryStream.Length - memoryStream.Position)
                        {
                            throw new InvalidDataException("Invalid length value.");
                        }
                        byte[] encodedItem = reader.ReadBytes(length);
                        var decoded = ChangeDecode(encodedItem);
                        var key = new StorageKey(decoded.key);
                        var value = new StorageItem(decoded.value);
                        try
                        {
                            switch (decoded.operation)
                            {
                                case FallbackOperation.Deleted:
                                    snapshot.Add(key, value);
                                    break;
                                case FallbackOperation.Added:
                                    snapshot.Delete(key);
                                    break;
                                case FallbackOperation.Changed:
                                    snapshot.GetAndChange(key).FromReplica(value);
                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid fallback operation");
                            }
                        }
                        catch (Exception e)
                        {
                            ConsoleHelper.Warning($"Exception during fallback: {e.Message}");
                        }
                    }
                    snapshotFallback.Delete(fallbackKey);
                    snapshotFallback.Commit();
                    ConsoleHelper.Info($"Fallback to block {i}");
                }
            }
        }

        /// <summary>
        /// Encodes the change data for persistent storage.
        /// </summary>
        /// <param name="blockId">The block index.</param>
        /// <param name="operation">The operation type.</param>
        /// <param name="key">The storage key.</param>
        /// <param name="value">The storage value.</param>
        /// <returns>The encoded byte array.</returns>
        private static byte[] ChangeEncode(uint blockId, FallbackOperation operation, byte[] key, byte[] value)
        {
            var change = new
            {
                BlockId = blockId,
                Operation = operation,
                Key = key,
                Value = value
            };
            return JsonSerializer.SerializeToUtf8Bytes(change);
        }

        /// <summary>
        /// Decodes the change data from persistent storage.
        /// </summary>
        /// <param name="data">The encoded byte array.</param>
        /// <returns>The decoded change data.</returns>
        private static (uint blockId, FallbackOperation operation, byte[] key, byte[] value) ChangeDecode(byte[] data)
        {
            var change = JsonSerializer.Deserialize<Dictionary<string, object>>(data);
            var blockId = Convert.ToUInt32(change["BlockId"]);
            var operation = (FallbackOperation)Convert.ToByte(change["Operation"]);
            var key = JsonSerializer.Deserialize<byte[]>((string)change["Key"]);
            var value = JsonSerializer.Deserialize<byte[]>((string)change["Value"]);
            return (blockId, operation, key, value);
        }

        /// <summary>
        /// Defines the operations for fallback actions.
        /// </summary>
        private enum FallbackOperation : byte
        {
            /// <summary>
            /// Indicates the state was deleted.
            /// </summary>
            Deleted = 0,
            /// <summary>
            /// Indicates the state was added.
            /// </summary>
            Added = 1,
            /// <summary>
            /// Indicates the state was changed.
            /// </summary>
            Changed = 2
        }
    }
}

