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

#pragma warning disable IDE0051

using Neo.Persistence;
using Neo.SmartContract.Iterators;
using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Linq;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract for temporary key-value storage.
    /// </summary>
    public sealed class TemporaryStorage : NativeContract
    {
        /// <summary>
        /// Stores temporary key-value pairs where the storage item key consists of:
        /// Prefix_Record + contractID (4 bytes of int32 LE) + key bytes
        /// </summary>
        private const byte Prefix_Record = 0x01;

        /// <summary>
        /// Stores the timestamp (in milliseconds) when a particular temporary key-value
        /// pair can be reached for the last time. The storage item key has the form:
	    /// Prefix_ValidTill + validTillTimestamp (8 bytes of uint64 BE) + contract ID (4 bytes of int32 LE) + key bytes
        /// </summary>
        private const byte Prefix_ValidTill = 0x02;

        /// <summary>
        /// The maximum size of temporary key-value records that can be removed per a single PostPersist.
        /// </summary>
        private const int MaxCleanupBatchSize = 10_000;

        /// <summary>
        /// The number of milliseconds in one year.
        /// </summary>
        private const ulong MsPerYear = 365 * 24 * 60 * 60 * 1_000UL;

        private readonly StorageKey _validTill;

        internal TemporaryStorage() : base()
        {
            _validTill = CreateStorageKey(Prefix_ValidTill);
        }

        public override ImmutableHashSet<Hardfork?> Activations => [Hardfork.HF_Huyao];

        internal override ContractTask PostPersistAsync(ApplicationEngine engine)
        {
            int count = 0;
            var timestamp = engine.PersistingBlock!.Timestamp;
            foreach (var (key, _) in engine.SnapshotCache.Find(_validTill, SeekDirection.Forward))
            {
                var keySpan = key.Key.Span;
                ulong validTill = BinaryPrimitives.ReadUInt64BigEndian(keySpan[1..9]);
                if (validTill >= timestamp)
                    break;

                engine.SnapshotCache.Delete(key);
                engine.SnapshotCache.Delete(CreateStorageKey(Prefix_Record, keySpan[9..]));

                count++;
                if (count >= MaxCleanupBatchSize)
                    break;
            }

            return ContractTask.CompletedTask;
        }

        /// <summary>
        /// Puts key-value pair to the temporary storage.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="key">The data key.</param>
        /// <param name="value">The data value.</param>
        /// <param name="validTill">The timestamp (in milliseconds) after which the key-value pair will be removed from the temporary storage.</param>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.WriteStates)]
        private void Put(ApplicationEngine engine, [MaxLength(ApplicationEngine.MaxStorageKeySize)] byte[] key, [MaxLength(ApplicationEngine.MaxStorageValueSize)] byte[] value, ulong validTill)
        {
            ulong currTimestamp = engine.PersistingBlock!.Timestamp;
            ValidateValidTill(engine, validTill, currTimestamp);

            ContractState callingContract = GetContractState(engine.SnapshotCache, engine.CallingScriptHash!);
            StorageKey recordKey = MakeRecordStorageKey(callingContract.Id, key);
            long lifetime = (long)(validTill - currTimestamp);
            engine.AddFee(CalculateStoragePrice(engine, recordKey.Key, value, lifetime, out var old), true);
            if (old is not null)
            {
                engine.SnapshotCache.Delete(MakeValidTillStorageKey(old.Value[..8].Span, recordKey.Key.Span));
            }

            PutRecord(engine.SnapshotCache, recordKey, value, validTill);
        }

        /// <summary>
        /// Returns value stored by the given key in the temporary storage of the calling contract.
        /// </summary>
        /// <param name="engine">The executionn engine.</param>
        /// <param name="key">The key used to retrieve data.</param>
        /// <returns>The requested item if exists and not yet expired, otherwise <see langword="null"/>.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private byte[]? Get(ApplicationEngine engine, [MaxLength(ApplicationEngine.MaxStorageKeySize)] byte[] key)
        {
            return GetInternal(engine, engine.CallingScriptHash!, key).Value;
        }

        /// <summary>
        /// Returns value stored by the given key in the temporary storage of the requested contract.
        /// </summary>
        /// <param name="engine">The executionn engine.</param>
        /// <param name="hash">The hash of the contract owning a temporary key-value pair.</param>
        /// <param name="key">The key used to retrieve data.</param>
        /// <returns>The requested item if exists and not yet expired, otherwise <see langword="null"/>.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private byte[]? Get(ApplicationEngine engine, UInt160 hash, [MaxLength(ApplicationEngine.MaxStorageKeySize)] byte[] key)
        {
            return GetInternal(engine, hash, key).Value;
        }

        /// <summary>
        /// Returns the end-of-life timestamp for the given key-value pair of the calling contract.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="key">The key used to retrieve data.</param>
        /// <returns>The end-of-life timestamp (in milliseconds) of the given key-value pair if it exists and not expired yet.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private ulong GetExpiration(ApplicationEngine engine, [MaxLength(ApplicationEngine.MaxStorageKeySize)] byte[] key)
        {
            return GetInternal(engine, engine.CallingScriptHash!, key).ValidTill;
        }

        /// <summary>
        /// Returns the end-of-life timestamp for the given key-value pair of the requested contract.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="hash">The hash of the contract owning a temporary key-value pair.</param>
        /// <param name="key">The key used to retrieve data.</param>
        /// <returns>The end-of-life timestamp (in milliseconds) of the given key-value pair if it exists and not expired yet.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private ulong GetExpiration(ApplicationEngine engine, UInt160 hash, [MaxLength(ApplicationEngine.MaxStorageKeySize)] byte[] key)
        {
            return GetInternal(engine, hash, key).ValidTill;
        }

        /// <summary>
        /// An internal helper used to unify 'get' contract method overloads.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="hash">The hash of the contract owning a temporary key-value pair.</param>
        /// <param name="key">The key used to retrieve data.</param>
        /// <returns>A tuple containing the requested item (if exists and not yet expired, otherwise <see langword="null"/>)
        /// and its expiration timestamp (in milliseconds).</returns>
        private (byte[]? Value, ulong ValidTill) GetInternal(ApplicationEngine engine, UInt160 hash, byte[] key)
        {
            ContractState contract = GetContractState(engine.SnapshotCache, hash);
            StorageKey recordKey = MakeRecordStorageKey(contract.Id, key);
            if (!engine.SnapshotCache.TryGet(recordKey, out var record))
                return (null, 0);

            if (!IsTraceable(engine, record, out var validTill))
                return (null, 0);

            return (record.Value[8..].ToArray(), validTill);
        }

        /// <summary>
        /// Removes the specified key-value pair of the calling contract from the storage.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="key">The key used to remove data.</param>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.WriteStates)]
        private void Delete(ApplicationEngine engine, [MaxLength(ApplicationEngine.MaxStorageKeySize)] byte[] key)
        {
            ContractState callingContract = GetContractState(engine.SnapshotCache, engine.CallingScriptHash!);
            StorageKey recordKey = MakeRecordStorageKey(callingContract.Id, key);
            if (!engine.SnapshotCache.TryGet(recordKey, out var record))
                return;

            var validTill = record.Value[..8].Span;
            engine.SnapshotCache.Delete(recordKey);
            engine.SnapshotCache.Delete(MakeValidTillStorageKey(validTill, recordKey.Key.Span));
        }

        /// <summary>
        /// Finds a set of key-value pairs matching the find options in the temporary storage of the calling contract.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="prefix">The prefix used to find data.</param>
        /// <param name="options">Iterator options customizing the result iteration behaviour.</param>
        /// <returns>Iterator over the matching key-value pairs.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private StorageIterator Find(ApplicationEngine engine, [MaxLength(ApplicationEngine.MaxStorageKeySize)] byte[] prefix, FindOptions options)
        {
            return FindInternal(engine, engine.CallingScriptHash!, prefix, options);
        }

        /// <summary>
        /// Finds a set of key-value pairs matching the find options in the temporary storage of the requested contract.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="hash">The hash of the of the contract owning the requested temporary key-value pairs.</param>
        /// <param name="prefix">The prefix used to find data.</param>
        /// <param name="options">Iterator options customizing the result iteration behaviour.</param>
        /// <returns>Iterator over the matching key-value pairs.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private StorageIterator Find(ApplicationEngine engine, UInt160 hash, [MaxLength(ApplicationEngine.MaxStorageKeySize)] byte[] prefix, FindOptions options)
        {
            return FindInternal(engine, hash, prefix, options);
        }

        /// <summary>
        /// An internal helper used to unify 'find' contract method overloads.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="hash">The hash of the of the contract owning the requested temporary key-value pairs.</param>
        /// <param name="prefix">The prefix used to find data.</param>
        /// <param name="options">Iterator options customizing the result iteration behaviour.</param>
        /// <returns>Iterator over the matching key-value pairs.</returns>
        private StorageIterator FindInternal(ApplicationEngine engine, UInt160 hash, byte[] prefix, FindOptions options)
        {
            var direction = ApplicationEngine.ValidateFindOptions(options);

            ContractState contract = GetContractState(engine.SnapshotCache, hash);
            var enumerator = engine.SnapshotCache
                .Find(MakeRecordStorageKey(contract.Id, prefix), direction)
                .Where(kvp => IsTraceable(engine, kvp.Value, out var _))
                .Select(kvp => (kvp.Key, new StorageItem(kvp.Value.Value[8..].ToArray())))
                .GetEnumerator();

            var iter = new StorageIterator(enumerator, prefix.Length, options);

            return iter;
        }

        /// <summary>
        /// Updates the expiration value of the given temporary key-value pair if it exists in the storage of the calling contract.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="key">The key used to retrieve data.</param>
        /// <param name="validTill">New expiration timestamp (in milliseconds).</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.WriteStates)]
        private void Renew(ApplicationEngine engine, [MaxLength(ApplicationEngine.MaxStorageKeySize)] byte[] key, ulong validTill)
        {
            ulong currTimestamp = engine.PersistingBlock!.Timestamp;
            ValidateValidTill(engine, validTill, currTimestamp);

            ContractState callingContract = GetContractState(engine.SnapshotCache, engine.CallingScriptHash!);
            StorageKey recordKey = MakeRecordStorageKey(callingContract.Id, key);
            var oldRecord = engine.SnapshotCache.TryGet(recordKey) ?? throw new InvalidOperationException("old record not found");

            if (!IsTraceable(engine, oldRecord, out var oldValidTill))
                throw new InvalidOperationException("old record is expired");
            if (validTill <= oldValidTill)
                throw new ArgumentOutOfRangeException(nameof(validTill), $"new expiration point should be newer than the old one: {validTill} vs {oldValidTill}");

            byte[] value = oldRecord.Value[8..].ToArray();
            long lifetime = checked((long)(validTill - oldValidTill));
            engine.AddFee(CalculateStoragePrice(engine, recordKey.Key, value, lifetime, out var _), true);

            engine.SnapshotCache.Delete(MakeValidTillStorageKey(oldRecord.Value[..8].Span, recordKey.Key.Span));
            PutRecord(engine.SnapshotCache, recordKey, value, validTill);
        }

        /// <summary>
        /// Tells whether the specified temporary key-value record is still not expired and can be reached by the callers.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="record">The temporary record value.</param>
        /// <param name="validTill">The expiration timestamp of the provided record if found in the storage and not expired yet.</param>
        /// <returns>Whether the record is reachable.</returns>
        private static bool IsTraceable(ApplicationEngine engine, StorageItem record, out ulong validTill)
        {
            validTill = BinaryPrimitives.ReadUInt64BigEndian(record.Value.Span[..8]);
            if (validTill < engine.PersistingBlock!.Timestamp)
            {
                validTill = 0;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Creates two entries in the TemporaryStorage contract: key-value pair and its expiration metadata.
        /// </summary>
        /// <param name="snapshot">The snapshot used to write the entries.</param>
        /// <param name="recordKey">The key-value record key.</param>
        /// <param name="value">The value that should be stored in the temporary storage.</param>
        /// <param name="validTill">The expiration timestamp of the provided key-value pair.</param>
        private void PutRecord(DataCache snapshot, StorageKey recordKey, ReadOnlySpan<byte> value, ulong validTill)
        {
            byte[] recordValue = new byte[8 + value.Length];
            BinaryPrimitives.WriteUInt64BigEndian(recordValue, validTill);
            value.CopyTo(recordValue.AsSpan(8));

            snapshot.GetAndChange(recordKey, () => new StorageItem())!.Value = recordValue;
            snapshot.GetAndChange(MakeValidTillStorageKey(validTill, recordKey.Key.Span), () => new StorageItem([]));
        }

        /// <summary>
        /// Creates Prefix_Record-prefixed storage key used to store key-value par.
        /// </summary>
        /// <param name="contractId">The ID of the contract that owns the specified key-value pair.</param>
        /// <param name="key">The key to be stored.</param>
        /// <returns>Corresponding storage key.</returns>
        private StorageKey MakeRecordStorageKey(int contractId, ReadOnlySpan<byte> key)
        {
            return new KeyBuilder(Id, Prefix_Record).AddLittleEndian(contractId).Add(key);
        }

        /// <summary>
        /// Creates Prefix_ValidTill-prefixed storage key used to store the expiration details of the specified key-value pair.
        /// </summary>
        /// <param name="validTill">The expiration timestamp.</param>
        /// <param name="recordKey">Prefix_Record-prefixed key of the specified record.</param>
        /// <returns>Corresponding storage key.</returns>
        private StorageKey MakeValidTillStorageKey(ulong validTill, ReadOnlySpan<byte> recordKey)
        {
            return new KeyBuilder(Id, Prefix_ValidTill).AddBigEndian(validTill).Add(recordKey[1..]);
        }

        /// <summary>
        /// Creates Prefix_ValidTill-prefixed storage key used to store the expiration details of the specified key-value pair.
        /// </summary>
        /// <param name="validTill">The expiration timestamp in bytes (8 bytes in BE form).</param>
        /// <param name="recordKey">Prefix_Record-prefixed key of the specified record.</param>
        /// <returns>Corresponding storage key.</returns>
        private StorageKey MakeValidTillStorageKey(ReadOnlySpan<byte> validTill, ReadOnlySpan<byte> recordKey)
        {
            return new KeyBuilder(Id, Prefix_ValidTill).Add(validTill).Add(recordKey[1..]);
        }

        /// <summary>
        /// Retrieves the contract from the storage and throws exception in case of missing contract.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data from.</param>
        /// <param name="hash">The hash of the requested contract.</param>
        /// <returns>The contract state.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static ContractState GetContractState(DataCache snapshot, UInt160 hash)
        {
            return ContractManagement.GetContract(snapshot, hash)
                ?? throw new InvalidOperationException($"calling contract not found: {hash}");
        }

        /// <summary>
        /// Calculates the price of storing the specified key-value pair for the desired amount of time.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="key">The key used to store data.</param>
        /// <param name="value">The stored value (without any prefix). </param>
        /// <param name="lifetime">The lifetime of the key-value pair in milliseconds.</param>
        /// <param name="item">The retrieved storage item (if already exists in the storage).</param>
        /// <returns>The storage price (need to apply FeeFactor to the return value).</returns>
        private long CalculateStoragePrice(ApplicationEngine engine, ReadOnlyMemory<byte> key, byte[] value, long lifetime, out StorageItem? item)
        {
            StorageKey skey = new()
            {
                Id = Id,
                Key = key
            };
            var permanentPrice = engine.CalculateStoragePrice(skey, value, out item);

            return (long)((ulong)(permanentPrice * engine.StoragePrice) / Math.Min((ulong)lifetime, MsPerYear) * MsPerYear);
        }

        /// <summary>
        /// Ensures the provided validTill value fits the limit of [timestamp + 2*MSPerBlock, timestamp + MaxTTL].
        /// </summary>
        /// <param name="engine">The engine used to read data.</param>
        /// <param name="validTill">The expiration timestamp in milliseconds.</param>
        /// <param name="timestamp">The timestamp of the current block.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void ValidateValidTill(ApplicationEngine engine, ulong validTill, ulong timestamp)
        {
            ulong maxValidTill = checked(timestamp + Policy.GetTemporaryStorageMaxTTL(engine.SnapshotCache));
            if (validTill > maxValidTill)
                throw new ArgumentOutOfRangeException(nameof(validTill), $"validTill exceeds max limit: {validTill} vs {maxValidTill}");

            ulong minValidTill = checked(timestamp + 2 * Policy.GetMillisecondsPerBlock(engine.SnapshotCache));
            if (validTill < minValidTill)
                throw new ArgumentOutOfRangeException(nameof(validTill), $"item is valid for less than 2*msPerBlock: {validTill} vs {minValidTill}");
        }
    }
}
