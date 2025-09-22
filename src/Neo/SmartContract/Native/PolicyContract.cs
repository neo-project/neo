// Copyright (C) 2015-2025 The Neo Project.
//
// PolicyContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using System;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract that manages the system policies.
    /// </summary>
    public sealed class PolicyContract : NativeContract
    {
        /// <summary>
        /// The default execution fee factor.
        /// </summary>
        public const uint DefaultExecFeeFactor = 30;

        /// <summary>
        /// The default storage price.
        /// </summary>
        public const uint DefaultStoragePrice = 100000;

        /// <summary>
        /// The default memory fee factor used for charging transient memory allocations.
        /// </summary>
        public const uint DefaultMemoryFeeFactor = 30;

        /// <summary>
        /// The default network fee per byte of transactions.
        /// In the unit of datoshi, 1 datoshi = 1e-8 GAS
        /// </summary>
        public const uint DefaultFeePerByte = 1000;

        /// <summary>
        /// The default fee for attribute.
        /// </summary>
        public const uint DefaultAttributeFee = 0;

        /// <summary>
        /// The default fee for NotaryAssisted attribute.
        /// </summary>
        public const uint DefaultNotaryAssistedAttributeFee = 1000_0000;

        /// <summary>
        /// The maximum execution fee factor that the committee can set.
        /// </summary>
        public const uint MaxExecFeeFactor = 100;

        /// <summary>
        /// The maximum fee for attribute that the committee can set.
        /// </summary>
        public const uint MaxAttributeFee = 10_0000_0000;

        /// <summary>
        /// The maximum storage price that the committee can set.
        /// </summary>
        public const uint MaxStoragePrice = 10000000;

        /// <summary>
        /// The maximum memory fee factor that the committee can set.
        /// </summary>
        public const uint MaxMemoryFeeFactor = 1000;

        /// <summary>
        /// The maximum block generation time that the committee can set in milliseconds.
        /// </summary>
        public const uint MaxMillisecondsPerBlock = 30_000;

        /// <summary>
        /// The maximum MaxValidUntilBlockIncrement value that the committee can set.
        /// It is set to be a day of 1-second blocks.
        /// </summary>
        public const uint MaxMaxValidUntilBlockIncrement = 86400;

        /// <summary>
        /// The maximum MaxTraceableBlocks value that the committee can set.
        /// It is set to be a year of 15-second blocks.
        /// </summary>
        public const uint MaxMaxTraceableBlocks = 2102400;

        private const byte Prefix_BlockedAccount = 15;
        private const byte Prefix_FeePerByte = 10;
        private const byte Prefix_ExecFeeFactor = 18;
        private const byte Prefix_StoragePrice = 19;
        private const byte Prefix_MemoryFeeFactor = 24;
        private const byte Prefix_AttributeFee = 20;
        private const byte Prefix_MillisecondsPerBlock = 21;
        private const byte Prefix_MaxValidUntilBlockIncrement = 22;
        private const byte Prefix_MaxTraceableBlocks = 23;

        private readonly StorageKey _feePerByte;
        private readonly StorageKey _execFeeFactor;
        private readonly StorageKey _storagePrice;
        private readonly StorageKey _memoryFeeFactor;
        private readonly StorageKey _millisecondsPerBlock;
        private readonly StorageKey _maxValidUntilBlockIncrement;
        private readonly StorageKey _maxTraceableBlocks;

        /// <summary>
        /// The event name for the block generation time changed.
        /// </summary>
        private const string MillisecondsPerBlockChangedEventName = "MillisecondsPerBlockChanged";

        [ContractEvent(Hardfork.HF_Echidna, 0, name: MillisecondsPerBlockChangedEventName,
            "old", ContractParameterType.Integer,
            "new", ContractParameterType.Integer
        )]
        internal PolicyContract() : base()
        {
            _feePerByte = CreateStorageKey(Prefix_FeePerByte);
            _execFeeFactor = CreateStorageKey(Prefix_ExecFeeFactor);
            _storagePrice = CreateStorageKey(Prefix_StoragePrice);
            _memoryFeeFactor = CreateStorageKey(Prefix_MemoryFeeFactor);
            _millisecondsPerBlock = CreateStorageKey(Prefix_MillisecondsPerBlock);
            _maxValidUntilBlockIncrement = CreateStorageKey(Prefix_MaxValidUntilBlockIncrement);
            _maxTraceableBlocks = CreateStorageKey(Prefix_MaxTraceableBlocks);
        }

        internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
        {
            if (hardfork == ActiveIn)
            {
                engine.SnapshotCache.Add(_feePerByte, new StorageItem(DefaultFeePerByte));
                engine.SnapshotCache.Add(_execFeeFactor, new StorageItem(DefaultExecFeeFactor));
                engine.SnapshotCache.Add(_storagePrice, new StorageItem(DefaultStoragePrice));
                engine.SnapshotCache.Add(_memoryFeeFactor, new StorageItem(DefaultMemoryFeeFactor));
            }
            if (hardfork == Hardfork.HF_Echidna)
            {
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_AttributeFee, (byte)TransactionAttributeType.NotaryAssisted), new StorageItem(DefaultNotaryAssistedAttributeFee));
                engine.SnapshotCache.Add(_millisecondsPerBlock, new StorageItem(engine.ProtocolSettings.MillisecondsPerBlock));
                engine.SnapshotCache.Add(_maxValidUntilBlockIncrement, new StorageItem(engine.ProtocolSettings.MaxValidUntilBlockIncrement));
                engine.SnapshotCache.Add(_maxTraceableBlocks, new StorageItem(engine.ProtocolSettings.MaxTraceableBlocks));
            }
            return ContractTask.CompletedTask;
        }

        /// <summary>
        /// Gets the network fee per transaction byte.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The network fee per transaction byte.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public long GetFeePerByte(IReadOnlyStore snapshot)
        {
            return (long)(BigInteger)snapshot[_feePerByte];
        }

        /// <summary>
        /// Gets the execution fee factor. This is a multiplier that can be adjusted by the committee to adjust the system fees for transactions.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The execution fee factor.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetExecFeeFactor(IReadOnlyStore snapshot)
        {
            return (uint)(BigInteger)snapshot[_execFeeFactor];
        }

        /// <summary>
        /// Gets the storage price.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The storage price.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetStoragePrice(IReadOnlyStore snapshot)
        {
            return (uint)(BigInteger)snapshot[_storagePrice];
        }

        /// <summary>
        /// Gets the memory fee factor.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The memory fee factor.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetMemoryFeeFactor(IReadOnlyStore snapshot)
        {
            return snapshot.TryGet(_memoryFeeFactor, out var item)
                ? (uint)(BigInteger)item
                : DefaultMemoryFeeFactor;
        }

        /// <summary>
        /// Gets the block generation time in milliseconds.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The block generation time in milliseconds.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetMillisecondsPerBlock(IReadOnlyStore snapshot)
        {
            return (uint)(BigInteger)snapshot[_millisecondsPerBlock];
        }

        /// <summary>
        /// Gets the upper increment size of blockchain height (in blocks) exceeding
        /// that a transaction should fail validation.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>MaxValidUntilBlockIncrement value.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetMaxValidUntilBlockIncrement(IReadOnlyStore snapshot)
        {
            return (uint)(BigInteger)snapshot[_maxValidUntilBlockIncrement];
        }

        /// <summary>
        /// Gets the length of the chain accessible to smart contracts.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>MaxTraceableBlocks value.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetMaxTraceableBlocks(IReadOnlyStore snapshot)
        {
            return (uint)(BigInteger)snapshot[_maxTraceableBlocks];
        }

        /// <summary>
        /// Gets the fee for attribute before Echidna hardfork.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="attributeType">Attribute type excluding <see cref="TransactionAttributeType.NotaryAssisted"/></param>
        /// <returns>The fee for attribute.</returns>
        [ContractMethod(true, Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates, Name = "getAttributeFee")]
        public uint GetAttributeFeeV0(IReadOnlyStore snapshot, byte attributeType)
        {
            return GetAttributeFee(snapshot, attributeType, false);
        }

        /// <summary>
        /// Gets the fee for attribute after Echidna hardfork.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="attributeType">Attribute type</param>
        /// <returns>The fee for attribute.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates, Name = "getAttributeFee")]
        public uint GetAttributeFeeV1(IReadOnlyStore snapshot, byte attributeType)
        {
            return GetAttributeFee(snapshot, attributeType, true);
        }

        /// <summary>
        /// Generic handler for GetAttributeFeeV0 and GetAttributeFee that
        /// gets the fee for attribute.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="attributeType">Attribute type</param>
        /// <param name="allowNotaryAssisted">Whether to support <see cref="TransactionAttributeType.NotaryAssisted"/> attribute type.</param>
        /// <returns>The fee for attribute.</returns>
        private uint GetAttributeFee(IReadOnlyStore snapshot, byte attributeType, bool allowNotaryAssisted)
        {
            if (!Enum.IsDefined(typeof(TransactionAttributeType), attributeType) ||
                (!allowNotaryAssisted && attributeType == (byte)(TransactionAttributeType.NotaryAssisted)))
            {
                throw new InvalidOperationException($"Attribute type {attributeType} is not supported.");
            }

            var key = CreateStorageKey(Prefix_AttributeFee, attributeType);
            return snapshot.TryGet(key, out var item) ? (uint)(BigInteger)item : DefaultAttributeFee;
        }

        /// <summary>
        /// Determines whether the specified account is blocked.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="account">The account to be checked.</param>
        /// <returns><see langword="true"/> if the account is blocked; otherwise, <see langword="false"/>.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public bool IsBlocked(IReadOnlyStore snapshot, UInt160 account)
        {
            return snapshot.Contains(CreateStorageKey(Prefix_BlockedAccount, account));
        }

        /// <summary>
        /// Sets the block generation time in milliseconds.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="value">The block generation time in milliseconds. Must be between 1 and MaxBlockGenTime.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided value is outside the allowed range.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the caller is not a committee member.</exception>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        public void SetMillisecondsPerBlock(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxMillisecondsPerBlock)
                throw new ArgumentOutOfRangeException(nameof(value), $"MillisecondsPerBlock must be between 1 and {MaxMillisecondsPerBlock}, got {value}");
            if (!CheckCommittee(engine)) throw new InvalidOperationException("Invalid committee signature");

            var oldTime = GetMillisecondsPerBlock(engine.SnapshotCache);
            engine.SnapshotCache.GetAndChange(_millisecondsPerBlock).Set(value);

            engine.SendNotification(Hash, MillisecondsPerBlockChangedEventName,
                [new VM.Types.Integer(oldTime), new VM.Types.Integer(value)]);
        }

        /// <summary>
        /// Sets the fee for attribute before Echidna hardfork.
        /// </summary>
        /// <param name="engine">The engine used to check committee witness and read data.</param>
        /// <param name="attributeType">Attribute type excluding <see cref="TransactionAttributeType.NotaryAssisted"/></param>
        /// <param name="value">Attribute fee value</param>
        /// <returns>The fee for attribute.</returns>
        [ContractMethod(true, Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States, Name = "setAttributeFee")]
        private void SetAttributeFeeV0(ApplicationEngine engine, byte attributeType, uint value)
        {
            SetAttributeFee(engine, attributeType, value, false);
        }

        /// <summary>
        /// Sets the fee for attribute after Echidna hardfork.
        /// </summary>
        /// <param name="engine">The engine used to check committee witness and read data.</param>
        /// <param name="attributeType">Attribute type excluding <see cref="TransactionAttributeType.NotaryAssisted"/></param>
        /// <param name="value">Attribute fee value</param>
        /// <returns>The fee for attribute.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States, Name = "setAttributeFee")]
        private void SetAttributeFeeV1(ApplicationEngine engine, byte attributeType, uint value)
        {
            SetAttributeFee(engine, attributeType, value, true);
        }

        /// <summary>
        /// Generic handler for SetAttributeFeeV0 and SetAttributeFeeV1 that
        /// gets the fee for attribute.
        /// </summary>
        /// <param name="engine">The engine used to check committee witness and read data.</param>
        /// <param name="attributeType">Attribute type</param>
        /// <param name="value">Attribute fee value</param>
        /// <param name="allowNotaryAssisted">Whether to support <see cref="TransactionAttributeType.NotaryAssisted"/> attribute type.</param>
        /// <returns>The fee for attribute.</returns>
        private void SetAttributeFee(ApplicationEngine engine, byte attributeType, uint value, bool allowNotaryAssisted)
        {
            if (!Enum.IsDefined(typeof(TransactionAttributeType), attributeType) ||
                (!allowNotaryAssisted && attributeType == (byte)(TransactionAttributeType.NotaryAssisted)))
            {
                throw new InvalidOperationException($"Attribute type {attributeType} is not supported.");
            }

            if (value > MaxAttributeFee)
                throw new ArgumentOutOfRangeException(nameof(value), $"AttributeFee must be less than {MaxAttributeFee}");

            if (!CheckCommittee(engine)) throw new InvalidOperationException();

            engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_AttributeFee, attributeType), () => new StorageItem(DefaultAttributeFee)).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetFeePerByte(ApplicationEngine engine, long value)
        {
            if (value < 0 || value > 1_00000000)
                throw new ArgumentOutOfRangeException(nameof(value), $"FeePerByte must be between [0, 100000000], got {value}");
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.SnapshotCache.GetAndChange(_feePerByte).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetExecFeeFactor(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxExecFeeFactor)
                throw new ArgumentOutOfRangeException(nameof(value), $"ExecFeeFactor must be between [1, {MaxExecFeeFactor}], got {value}");
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.SnapshotCache.GetAndChange(_execFeeFactor).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetStoragePrice(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxStoragePrice)
                throw new ArgumentOutOfRangeException(nameof(value), $"StoragePrice must be between [1, {MaxStoragePrice}], got {value}");
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.SnapshotCache.GetAndChange(_storagePrice).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetMemoryFeeFactor(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxMemoryFeeFactor)
                throw new ArgumentOutOfRangeException(nameof(value), $"MemoryFeeFactor must be between [1, {MaxMemoryFeeFactor}], got {value}");
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.SnapshotCache.GetAndChange(_memoryFeeFactor, () => new StorageItem(DefaultMemoryFeeFactor)).Set(value);
        }

        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetMaxValidUntilBlockIncrement(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxMaxValidUntilBlockIncrement)
                throw new ArgumentOutOfRangeException(nameof(value), $"MaxValidUntilBlockIncrement must be between [1, {MaxMaxValidUntilBlockIncrement}], got {value}");
            var mtb = GetMaxTraceableBlocks(engine.SnapshotCache);
            if (value >= mtb)
                throw new InvalidOperationException($"MaxValidUntilBlockIncrement must be lower than MaxTraceableBlocks ({value} vs {mtb})");
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.SnapshotCache.GetAndChange(_maxValidUntilBlockIncrement).Set(value);
        }

        /// <summary>
        /// Sets the length of the chain accessible to smart contracts.
        /// </summary>
        /// <param name="engine">The engine used to check committee witness and read data.</param>
        /// <param name="value">MaxTraceableBlocks value.</param>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetMaxTraceableBlocks(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxMaxTraceableBlocks)
                throw new ArgumentOutOfRangeException(nameof(value), $"MaxTraceableBlocks must be between [1, {MaxMaxTraceableBlocks}], got {value}");
            var oldVal = GetMaxTraceableBlocks(engine.SnapshotCache);
            if (value > oldVal)
                throw new InvalidOperationException($"MaxTraceableBlocks can not be increased (old {oldVal}, new {value})");
            var mVUBIncrement = GetMaxValidUntilBlockIncrement(engine.SnapshotCache);
            if (value <= mVUBIncrement)
                throw new InvalidOperationException($"MaxTraceableBlocks must be larger than MaxValidUntilBlockIncrement ({value} vs {mVUBIncrement})");
            if (!CheckCommittee(engine)) throw new InvalidOperationException("Invalid committee signature");
            engine.SnapshotCache.GetAndChange(_maxTraceableBlocks).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private bool BlockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            return BlockAccount(engine.SnapshotCache, account);
        }

        internal bool BlockAccount(DataCache snapshot, UInt160 account)
        {
            if (IsNative(account)) throw new InvalidOperationException("It's impossible to block a native contract.");

            var key = CreateStorageKey(Prefix_BlockedAccount, account);
            if (snapshot.Contains(key)) return false;

            snapshot.Add(key, new StorageItem(Array.Empty<byte>()));
            return true;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) throw new InvalidOperationException();

            var key = CreateStorageKey(Prefix_BlockedAccount, account);
            if (!engine.SnapshotCache.Contains(key)) return false;

            engine.SnapshotCache.Delete(key);
            return true;
        }

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private StorageIterator GetBlockedAccounts(DataCache snapshot)
        {
            const FindOptions options = FindOptions.RemovePrefix | FindOptions.KeysOnly;
            var enumerator = snapshot
                .Find(CreateStorageKey(Prefix_BlockedAccount), SeekDirection.Forward)
                .GetEnumerator();
            return new StorageIterator(enumerator, 1, options);
        }
    }
}
