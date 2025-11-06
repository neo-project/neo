// Copyright (C) 2015-2025 The Neo Project.
//
// PolicyContract.DynamicBlock.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Buffers.Binary;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    public sealed partial class PolicyContract : NativeContract
    {
        // TODO: MaxTransactionPerBlock must be moved from config to Policy
        // TODO: MillisecondPerBlock must be moved from config to Policy
        // TODO: The config value is the default value rigthnow (no longer read except to initialize)

        /// <summary>
        /// The default maximum transactions per block.
        /// </summary>
        public const uint DefaultMillisecondsPerBlock = 15_000;

        /// <summary>
        /// The default maximum transactions per block.
        /// </summary>
        public const uint DefaultMaxTransactionPerBlock = 512;

        /// <summary>
        /// The maximum number of dynamic levels that the committee can set.
        /// </summary>
        public const int MaxDynamicLevels = 5;

        private const byte Prefix_DBI_LevelsMsPerBlock = 24;
        private const byte Prefix_DBI_MaxTransactionsPerBlock = 25;

        private readonly StorageKey _levelsMsPerBlock;
        private readonly StorageKey _maxTransactionsPerBlock;

        // TODO: Check what HF
        // TODO: Check rigth value of cpufee
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 5, RequiredCallFlags = CallFlags.States)]
        public void SetDynamicBlockSettings(ApplicationEngine engine, Array levelsMsPerBlock, Array maxTransactionsPerBlock)
        {
            AssertCommittee(engine);
            ArgumentNullException.ThrowIfNull(levelsMsPerBlock, nameof(levelsMsPerBlock));
            ArgumentNullException.ThrowIfNull(maxTransactionsPerBlock, nameof(maxTransactionsPerBlock));
            ArgumentOutOfRangeException.ThrowIfZero(levelsMsPerBlock.Count, nameof(levelsMsPerBlock));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(levelsMsPerBlock.Count, MaxDynamicLevels, nameof(levelsMsPerBlock));

            if (levelsMsPerBlock.Count != maxTransactionsPerBlock.Count) throw new ArgumentException("levelsMsPerBlock.Length is not equal than maxTransactionsPerBlock.Length");
            if (!IsAscending(levelsMsPerBlock)) throw new ArgumentException("levelsMsPerBlock must be in ascending order");

            var itemLevelsMsPerBlock = engine.SnapshotCache.GetAndChange(_levelsMsPerBlock);
            itemLevelsMsPerBlock.Value = Serialize(levelsMsPerBlock);

            var itemMaxTransactionPerBlock = engine.SnapshotCache.GetAndChange(_maxTransactionsPerBlock);
            itemMaxTransactionPerBlock.Value = Serialize(maxTransactionsPerBlock);
        }

        // TODO: check what HF
        // TODO: Check rigth value of cpufee
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 5, RequiredCallFlags = CallFlags.States)]
        public Array GetLevelsMsPerBlock(ApplicationEngine engine, IReadOnlyStore snapshot)
        {
            Array levelsMsPerBlock = [DefaultMillisecondsPerBlock];
            if (snapshot.TryGet(_levelsMsPerBlock, out var levels) && levels.Value is { Length: > 0 })
            {
                levelsMsPerBlock = Deserialize(levels.Value.Span, engine.ReferenceCounter);
            }
            return levelsMsPerBlock;
        }

        // TODO: check what HF
        // TODO: Check rigth value of cpufee
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 5, RequiredCallFlags = CallFlags.States)]
        public Array GetMaxTransactionsPerBlock(ApplicationEngine engine, IReadOnlyStore snapshot)
        {
            Array maxTxPerBlock = [DefaultMaxTransactionPerBlock];
            if (snapshot.TryGet(_maxTransactionsPerBlock, out var maxTxs) && maxTxs.Value is { Length: > 0 })
            {
                maxTxPerBlock = Deserialize(maxTxs.Value.Span, engine.ReferenceCounter);
            }
            return maxTxPerBlock;
        }

        internal static byte[] Serialize(Array items)
        {
            checked
            {
                var len = items.Count;
                var byteSize = 4 + (len * 4);
                var buffer = new byte[byteSize];
                var pointer = 0;
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), (uint)len);
                pointer += 4;
                for (var i = 0; i < len; i++)
                {
                    if (items[i] is not Integer it)
                        throw new ArgumentException("Items must be Integer");

                    var uintValue = it.GetInteger();

                    ArgumentOutOfRangeException.ThrowIfNegative(uintValue, nameof(items));
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(uintValue, uint.MaxValue, nameof(items));

                    var value = (uint)uintValue;
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pointer, 4), value);
                    pointer += 4;
                }
                return buffer;
            }
        }

        internal static Array Deserialize(ReadOnlySpan<byte> items, IReferenceCounter rc)
        {
            var pointer = 0;
            if (items.Length < 4) throw new FormatException("Invalid array bytes");
            var len = BinaryPrimitives.ReadUInt32LittleEndian(items[..4]);
            pointer += 4;
            if (items.Length != 4 + len * 4) throw new FormatException("Invalid array size");

            var buffer = new Array(rc);
            for (var i = 0; i < len; i++)
            {
                var value = BinaryPrimitives.ReadUInt32LittleEndian(items.Slice(pointer, 4));
                pointer += 4;
                buffer.Add(value);
            }
            return buffer;
        }

        internal static bool IsAscending(Array items)
        {
            var lenItems = items.Count;

            if (lenItems <= 1) return true;

            if (items[0] is not Integer firstItem)
                throw new ArgumentException("Items must be Integer", nameof(items));

            var prev = firstItem.GetInteger();

            for (var i = 1; i < lenItems; i++)
            {
                if (items[i] is not Integer it)
                    throw new ArgumentException("Items must be Integer", nameof(items));
                var value = it.GetInteger();
                if (value < prev) return false;
                prev = value;
            }

            return true;
        }
    }
}
