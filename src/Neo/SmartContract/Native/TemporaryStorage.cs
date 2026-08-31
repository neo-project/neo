// Copyright (C) 2015-2026 The Neo Project.
//
// TemporaryStorage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable
#pragma warning disable IDE0051

using Neo.Extensions;
using Neo.Persistence;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Native contract providing time-limited (TTL) temporary key-value storage.
    /// Entries are scoped per calling contract. TTL is specified in blocks and converted to
    /// wall-clock expiration via <see cref="PolicyContract.GetMillisecondsPerBlock"/> and the
    /// persisting block timestamp. Expired entries are garbage-collected during
    /// <see cref="PostPersistAsync"/> with a bounded per-block cleanup limit.
    /// See https://github.com/neo-project/neo/issues/4466.
    /// </summary>
    public sealed class TemporaryStorage : NativeContract
    {
        /// <summary>
        /// Default maximum TTL in blocks (~1 week of 15s blocks).
        /// </summary>
        public const uint DefaultMaxTtlBlocks = 40_320;

        /// <summary>
        /// Default maximum number of expired entries removed per block.
        /// </summary>
        public const uint DefaultCleanupLimit = 256;

        /// <summary>
        /// Maximum allowed key length in bytes.
        /// </summary>
        public const int MaxKeyLength = 64;

        /// <summary>
        /// Maximum allowed value length in bytes.
        /// </summary>
        public const int MaxValueLength = 65_536;

        private const byte Prefix_Data = 1;
        private const byte Prefix_Expire = 2;
        private const byte Prefix_MaxTtl = 10;
        private const byte Prefix_CleanupLimit = 11;

        [ContractEvent(0, name: "Put",
            "contract", ContractParameterType.Hash160,
            "key", ContractParameterType.ByteArray,
            "expireTime", ContractParameterType.Integer)]
        [ContractEvent(1, name: "Delete",
            "contract", ContractParameterType.Hash160,
            "key", ContractParameterType.ByteArray)]
        internal TemporaryStorage() : base() { }

        /// <inheritdoc/>
        public override ImmutableHashSet<Hardfork?> Activations => [Hardfork.HF_Huyao];

        internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
        {
            if (hardfork == ActiveIn)
            {
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_MaxTtl), new StorageItem(DefaultMaxTtlBlocks));
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_CleanupLimit), new StorageItem(DefaultCleanupLimit));
            }
            return ContractTask.CompletedTask;
        }

        /// <summary>
        /// Removes expired entries during PostPersist (bounded per block).
        /// Any entry with <c>expireTime &lt;= current block Timestamp</c> is eligible.
        /// </summary>
        internal override ContractTask PostPersistAsync(ApplicationEngine engine)
        {
            // Only run when HF_Huyao is enabled and this contract was initialized.
            // Note: NativeContract.IsActive treats a missing hardfork as "on from genesis",
            // so without this guard PostPersist would run on chains that never called InitializeAsync
            // (e.g. tests that only enable HF_Gorgon).
            if (!engine.IsHardforkEnabled(Hardfork.HF_Huyao))
                return ContractTask.CompletedTask;

            if (!engine.SnapshotCache.TryGet(CreateStorageKey(Prefix_CleanupLimit), out var limitItem))
                return ContractTask.CompletedTask;

            var now = engine.PersistingBlock!.Timestamp;
            var limit = (uint)(BigInteger)limitItem;
            if (limit == 0) return ContractTask.CompletedTask;

            // Materialize candidates first — never mutate the store while iterating Find/Seek.
            var expired = new List<(StorageKey ExpireKey, UInt160 Contract, byte[] UserKey)>();
            foreach (var (storageKey, _) in engine.SnapshotCache.Find(CreateStorageKey(Prefix_Expire)))
            {
                if (expired.Count >= limit) break;

                var keySpan = storageKey.Key.Span;
                // key: [prefix][expireTime:8 BE][contract:20][userKey...]
                if (keySpan.Length < 1 + sizeof(ulong) + UInt160.Length) continue;
                if (keySpan[0] != Prefix_Expire) break;

                var expireTime = BinaryPrimitives.ReadUInt64BigEndian(keySpan[1..]);
                if (expireTime > now) break;

                var contract = new UInt160(keySpan.Slice(1 + sizeof(ulong), UInt160.Length));
                var userKey = keySpan[(1 + sizeof(ulong) + UInt160.Length)..].ToArray();
                expired.Add((storageKey, contract, userKey));
            }

            foreach (var (expireKey, contract, userKey) in expired)
            {
                engine.SnapshotCache.Delete(CreateDataKey(contract, userKey));
                engine.SnapshotCache.Delete(expireKey);
            }

            return ContractTask.CompletedTask;
        }

        /// <summary>
        /// Stores a temporary value for the calling contract.
        /// Expiration is <c>block.Timestamp + ttlBlocks * Policy.GetMillisecondsPerBlock()</c>.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="key">User key (max <see cref="MaxKeyLength"/> bytes).</param>
        /// <param name="value">Value (max <see cref="MaxValueLength"/> bytes).</param>
        /// <param name="ttlBlocks">Time-to-live in blocks; must be in <c>[1, maxTtl]</c>.
        /// Converted to milliseconds using the current <see cref="PolicyContract.GetMillisecondsPerBlock"/>.</param>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        private void Put(ApplicationEngine engine, byte[] key, byte[] value, uint ttlBlocks)
        {
            var contract = RequireCaller(engine);
            ValidateKeyValue(key, value);
            ValidateTtl(engine, ttlBlocks);

            var expireTime = ComputeExpireTime(engine, ttlBlocks);
            PutInternal(engine, contract, key, value, expireTime);
        }

        /// <summary>
        /// Gets a temporary value for the calling contract. Returns <see langword="null"/> if missing or expired.
        /// Same-block puts are visible immediately.
        /// </summary>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private byte[]? Get(ApplicationEngine engine, byte[] key)
        {
            var contract = RequireCaller(engine);
            ValidateKey(key);
            return GetInternal(engine, contract, key);
        }

        /// <summary>
        /// Gets a temporary value stored by <paramref name="contract"/>. Enables cross-contract sharing.
        /// Returns <see langword="null"/> if missing or expired.
        /// </summary>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private byte[]? Get(ApplicationEngine engine, UInt160 contract, byte[] key)
        {
            ArgumentNullException.ThrowIfNull(contract);
            ValidateKey(key);
            return GetInternal(engine, contract, key);
        }

        /// <summary>
        /// Deletes a temporary entry owned by the calling contract before it expires.
        /// </summary>
        /// <returns><see langword="true"/> if an entry was removed; otherwise <see langword="false"/>.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        private bool Delete(ApplicationEngine engine, byte[] key)
        {
            var contract = RequireCaller(engine);
            ValidateKey(key);
            return DeleteInternal(engine, contract, key);
        }

        /// <summary>
        /// Extends the TTL of an existing entry owned by the calling contract.
        /// New expiration is <c>now + ttlBlocks * Policy.GetMillisecondsPerBlock()</c>.
        /// </summary>
        /// <returns><see langword="true"/> if the entry was renewed; otherwise <see langword="false"/>.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        private bool Renew(ApplicationEngine engine, byte[] key, uint ttlBlocks)
        {
            var contract = RequireCaller(engine);
            ValidateKey(key);
            ValidateTtl(engine, ttlBlocks);

            var dataKey = CreateDataKey(contract, key);
            if (engine.SnapshotCache.TryGet(dataKey) is not StorageItem item)
                return false;

            var (oldExpire, value) = DecodeEntry(item.Value.Span);
            var now = GetCurrentTimestamp(engine);
            if (oldExpire <= now)
            {
                // Lazy-expire: treat as missing.
                engine.SnapshotCache.Delete(dataKey);
                engine.SnapshotCache.Delete(CreateExpireKey(oldExpire, contract, key));
                return false;
            }

            var newExpire = ComputeExpireTime(engine, ttlBlocks);
            engine.SnapshotCache.Delete(CreateExpireKey(oldExpire, contract, key));
            engine.SnapshotCache.GetAndChange(dataKey)!.Value = EncodeEntry(newExpire, value);
            engine.SnapshotCache.Add(CreateExpireKey(newExpire, contract, key), new StorageItem(Array.Empty<byte>()));

            engine.SendNotification(Hash, "Put",
                [contract.ToArray(), key, newExpire]);
            return true;
        }

        /// <summary>
        /// Gets the maximum allowed TTL in blocks.
        /// </summary>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetMaxTtl(IReadOnlyStore snapshot)
        {
            return (uint)(BigInteger)snapshot[CreateStorageKey(Prefix_MaxTtl)];
        }

        /// <summary>
        /// Sets the maximum allowed TTL in blocks. Committee only.
        /// </summary>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetMaxTtl(ApplicationEngine engine, uint value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value), "MaxTtl must be positive.");
            AssertCommittee(engine);
            engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_MaxTtl))!.Set(value);
        }

        /// <summary>
        /// Gets the maximum number of expired entries cleaned per block.
        /// </summary>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetCleanupLimit(IReadOnlyStore snapshot)
        {
            return (uint)(BigInteger)snapshot[CreateStorageKey(Prefix_CleanupLimit)];
        }

        /// <summary>
        /// Sets the maximum number of expired entries cleaned per block. Committee only.
        /// </summary>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetCleanupLimit(ApplicationEngine engine, uint value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value), "CleanupLimit must be positive.");
            AssertCommittee(engine);
            engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_CleanupLimit))!.Set(value);
        }

        private void PutInternal(ApplicationEngine engine, UInt160 contract, byte[] key, byte[] value, ulong expireTime)
        {
            var dataKey = CreateDataKey(contract, key);

            // Replace: drop previous expire index if present.
            if (engine.SnapshotCache.TryGet(dataKey) is StorageItem existing)
            {
                var (oldExpire, _) = DecodeEntry(existing.Value.Span);
                engine.SnapshotCache.Delete(CreateExpireKey(oldExpire, contract, key));
                engine.SnapshotCache.GetAndChange(dataKey)!.Value = EncodeEntry(expireTime, value);
            }
            else
            {
                engine.SnapshotCache.Add(dataKey, new StorageItem(EncodeEntry(expireTime, value)));
            }

            engine.SnapshotCache.Add(CreateExpireKey(expireTime, contract, key), new StorageItem(Array.Empty<byte>()));

            // Storage fee for key + value (+ expire index overhead).
            var storageBytes = key.Length + value.Length + sizeof(ulong) + UInt160.Length + key.Length;
            engine.AddFee(engine.StoragePrice * storageBytes, true);

            engine.SendNotification(Hash, "Put",
                [contract.ToArray(), key, expireTime]);
        }

        private byte[]? GetInternal(ApplicationEngine engine, UInt160 contract, byte[] key)
        {
            var dataKey = CreateDataKey(contract, key);
            if (engine.SnapshotCache.TryGet(dataKey) is not StorageItem item)
                return null;

            var (expireTime, value) = DecodeEntry(item.Value.Span);
            if (expireTime <= GetCurrentTimestamp(engine))
                return null;

            return value;
        }

        private bool DeleteInternal(ApplicationEngine engine, UInt160 contract, byte[] key)
        {
            var dataKey = CreateDataKey(contract, key);
            if (engine.SnapshotCache.TryGet(dataKey) is not StorageItem item)
                return false;

            var (expireTime, _) = DecodeEntry(item.Value.Span);
            engine.SnapshotCache.Delete(dataKey);
            engine.SnapshotCache.Delete(CreateExpireKey(expireTime, contract, key));

            engine.SendNotification(Hash, "Delete",
                [contract.ToArray(), key]);
            return true;
        }

        private void ValidateTtl(ApplicationEngine engine, uint ttlBlocks)
        {
            var maxTtl = GetMaxTtl(engine.SnapshotCache);
            if (ttlBlocks == 0 || ttlBlocks > maxTtl)
                throw new ArgumentOutOfRangeException(nameof(ttlBlocks), $"TTL must be between 1 and {maxTtl}, got {ttlBlocks}.");
        }

        /// <summary>
        /// <c>expireTime = now + ttlBlocks * Policy.GetMillisecondsPerBlock()</c>.
        /// </summary>
        private static ulong ComputeExpireTime(ApplicationEngine engine, uint ttlBlocks)
        {
            var now = GetCurrentTimestamp(engine);
            var msPerBlock = GetMillisecondsPerBlock(engine);
            return checked(now + (ulong)ttlBlocks * msPerBlock);
        }

        private static uint GetMillisecondsPerBlock(ApplicationEngine engine)
        {
            try
            {
                return Policy.GetMillisecondsPerBlock(engine.SnapshotCache);
            }
            catch (KeyNotFoundException)
            {
                // Policy value not yet available (e.g. edge genesis); fall back to protocol settings.
                return engine.ProtocolSettings.MillisecondsPerBlock;
            }
        }

        private static ulong GetCurrentTimestamp(ApplicationEngine engine)
        {
            if (engine.PersistingBlock is not null)
                return engine.PersistingBlock.Timestamp;

            var hash = Ledger.CurrentHash(engine.SnapshotCache);
            var block = Ledger.GetBlock(engine.SnapshotCache, hash);
            return block?.Timestamp ?? 0;
        }

        private static UInt160 RequireCaller(ApplicationEngine engine)
        {
            return engine.CallingScriptHash
                ?? throw new InvalidOperationException("Calling script hash is required for TemporaryStorage.");
        }

        private static void ValidateKey(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (key.Length == 0 || key.Length > MaxKeyLength)
                throw new ArgumentException($"Key length must be between 1 and {MaxKeyLength}, got {key.Length}.", nameof(key));
        }

        private static void ValidateKeyValue(byte[] key, byte[] value)
        {
            ValidateKey(key);
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > MaxValueLength)
                throw new ArgumentException($"Value length must be at most {MaxValueLength}, got {value.Length}.", nameof(value));
        }

        private StorageKey CreateDataKey(UInt160 contract, ReadOnlySpan<byte> key)
        {
            Span<byte> content = stackalloc byte[UInt160.Length + key.Length];
            contract.GetSpan().CopyTo(content);
            key.CopyTo(content[UInt160.Length..]);
            return CreateStorageKey(Prefix_Data, content);
        }

        private StorageKey CreateExpireKey(ulong expireTime, UInt160 contract, ReadOnlySpan<byte> key)
        {
            Span<byte> content = stackalloc byte[sizeof(ulong) + UInt160.Length + key.Length];
            BinaryPrimitives.WriteUInt64BigEndian(content, expireTime);
            contract.GetSpan().CopyTo(content[sizeof(ulong)..]);
            key.CopyTo(content[(sizeof(ulong) + UInt160.Length)..]);
            return CreateStorageKey(Prefix_Expire, content);
        }

        private static byte[] EncodeEntry(ulong expireTime, ReadOnlySpan<byte> value)
        {
            var data = new byte[sizeof(ulong) + value.Length];
            BinaryPrimitives.WriteUInt64LittleEndian(data, expireTime);
            value.CopyTo(data.AsSpan(sizeof(ulong)));
            return data;
        }

        private static (ulong ExpireTime, byte[] Value) DecodeEntry(ReadOnlySpan<byte> data)
        {
            if (data.Length < sizeof(ulong))
                throw new FormatException("Invalid TemporaryStorage entry.");
            var expire = BinaryPrimitives.ReadUInt64LittleEndian(data);
            return (expire, data[sizeof(ulong)..].ToArray());
        }
    }
}

#nullable disable
