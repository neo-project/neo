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
using System.Numerics;

namespace Neo.SmartContract.Native;

/// <summary>
/// A native contract that manages the system policies.
/// </summary>
[ContractEvent(0, name: MillisecondsPerBlockChangedEventName,
    "old", ContractParameterType.Integer,
    "new", ContractParameterType.Integer
)]
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
    private const byte Prefix_AttributeFee = 20;
    private const byte Prefix_MillisecondsPerBlock = 21;
    private const byte Prefix_MaxValidUntilBlockIncrement = 22;
    private const byte Prefix_MaxTraceableBlocks = 23;

    private readonly StorageKey _feePerByte;
    private readonly StorageKey _execFeeFactor;
    private readonly StorageKey _storagePrice;
    private readonly StorageKey _millisecondsPerBlock;
    private readonly StorageKey _maxValidUntilBlockIncrement;
    private readonly StorageKey _maxTraceableBlocks;

    /// <summary>
    /// The event name for the block generation time changed.
    /// </summary>
    private const string MillisecondsPerBlockChangedEventName = "MillisecondsPerBlockChanged";

    internal PolicyContract()
    {
        _feePerByte = CreateStorageKey(Prefix_FeePerByte);
        _execFeeFactor = CreateStorageKey(Prefix_ExecFeeFactor);
        _storagePrice = CreateStorageKey(Prefix_StoragePrice);
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
    /// Gets the block generation time in milliseconds.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>The block generation time in milliseconds.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
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
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public uint GetMaxValidUntilBlockIncrement(IReadOnlyStore snapshot)
    {
        return (uint)(BigInteger)snapshot[_maxValidUntilBlockIncrement];
    }

    /// <summary>
    /// Gets the length of the chain accessible to smart contracts.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>MaxTraceableBlocks value.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public uint GetMaxTraceableBlocks(IReadOnlyStore snapshot)
    {
        return (uint)(BigInteger)snapshot[_maxTraceableBlocks];
    }

    /// <summary>
    /// Gets the fee for attribute after Echidna hardfork. NotaryAssisted attribute type supported.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="attributeType">Attribute type</param>
    /// <returns>The fee for attribute.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public uint GetAttributeFee(IReadOnlyStore snapshot, byte attributeType)
    {
        if (!Enum.IsDefined(typeof(TransactionAttributeType), attributeType))
            throw new InvalidOperationException($"Attribute type {attributeType} is not supported.");
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
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    public void SetMillisecondsPerBlock(ApplicationEngine engine, uint value)
    {
        if (value == 0 || value > MaxMillisecondsPerBlock)
            throw new ArgumentOutOfRangeException(nameof(value), $"MillisecondsPerBlock must be between [1, {MaxMillisecondsPerBlock}], got {value}");
        AssertCommittee(engine);

        var oldTime = GetMillisecondsPerBlock(engine.SnapshotCache);
        engine.SnapshotCache.GetAndChange(_millisecondsPerBlock)!.Set(value);

        Notify(engine, MillisecondsPerBlockChangedEventName, oldTime, value);
    }

    /// <summary>
    /// Sets the fee for attribute after Echidna hardfork. NotaryAssisted attribute type supported.
    /// </summary>
    /// <param name="engine">The engine used to check committee witness and read data.</param>
    /// <param name="attributeType">Attribute type excluding <see cref="TransactionAttributeType.NotaryAssisted"/></param>
    /// <param name="value">Attribute fee value</param>
    /// <returns>The fee for attribute.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void SetAttributeFee(ApplicationEngine engine, byte attributeType, uint value)
    {
        if (!Enum.IsDefined(typeof(TransactionAttributeType), attributeType))
            throw new InvalidOperationException($"Attribute type {attributeType} is not supported.");

        if (value > MaxAttributeFee)
            throw new ArgumentOutOfRangeException(nameof(value), $"AttributeFee must be less than {MaxAttributeFee}");

        AssertCommittee(engine);

        engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_AttributeFee, attributeType), () => new StorageItem(DefaultAttributeFee)).Set(value);
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void SetFeePerByte(ApplicationEngine engine, long value)
    {
        if (value < 0 || value > 1_00000000)
            throw new ArgumentOutOfRangeException(nameof(value), $"FeePerByte must be between [0, 100000000], got {value}");
        AssertCommittee(engine);

        engine.SnapshotCache.GetAndChange(_feePerByte)!.Set(value);
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void SetExecFeeFactor(ApplicationEngine engine, uint value)
    {
        if (value == 0 || value > MaxExecFeeFactor)
            throw new ArgumentOutOfRangeException(nameof(value), $"ExecFeeFactor must be between [1, {MaxExecFeeFactor}], got {value}");
        AssertCommittee(engine);

        engine.SnapshotCache.GetAndChange(_execFeeFactor)!.Set(value);
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void SetStoragePrice(ApplicationEngine engine, uint value)
    {
        if (value == 0 || value > MaxStoragePrice)
            throw new ArgumentOutOfRangeException(nameof(value), $"StoragePrice must be between [1, {MaxStoragePrice}], got {value}");
        AssertCommittee(engine);

        engine.SnapshotCache.GetAndChange(_storagePrice)!.Set(value);
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void SetMaxValidUntilBlockIncrement(ApplicationEngine engine, uint value)
    {
        if (value == 0 || value > MaxMaxValidUntilBlockIncrement)
            throw new ArgumentOutOfRangeException(nameof(value), $"MaxValidUntilBlockIncrement must be between [1, {MaxMaxValidUntilBlockIncrement}], got {value}");
        var mtb = GetMaxTraceableBlocks(engine.SnapshotCache);
        if (value >= mtb)
            throw new InvalidOperationException($"MaxValidUntilBlockIncrement must be lower than MaxTraceableBlocks ({value} vs {mtb})");
        AssertCommittee(engine);

        engine.SnapshotCache.GetAndChange(_maxValidUntilBlockIncrement)!.Set(value);
    }

    /// <summary>
    /// Sets the length of the chain accessible to smart contracts.
    /// </summary>
    /// <param name="engine">The engine used to check committee witness and read data.</param>
    /// <param name="value">MaxTraceableBlocks value.</param>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
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

        AssertCommittee(engine);

        engine.SnapshotCache.GetAndChange(_maxTraceableBlocks)!.Set(value);
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private async ContractTask<bool> BlockAccount(ApplicationEngine engine, UInt160 account)
    {
        AssertCommittee(engine);
        return await BlockAccountInternal(engine, account);
    }

    internal async ContractTask<bool> BlockAccountInternal(ApplicationEngine engine, UInt160 account)
    {
        if (IsNative(account)) throw new InvalidOperationException("Cannot block a native contract.");

        var key = CreateStorageKey(Prefix_BlockedAccount, account);
        if (engine.SnapshotCache.Contains(key)) return false;

        await NEO.VoteInternal(engine, account, null);

        engine.SnapshotCache.Add(key, new StorageItem(Array.Empty<byte>()));
        return true;
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
    {
        AssertCommittee(engine);


        var key = CreateStorageKey(Prefix_BlockedAccount, account);
        if (!engine.SnapshotCache.Contains(key)) return false;

        engine.SnapshotCache.Delete(key);
        return true;
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    private StorageIterator GetBlockedAccounts(DataCache snapshot)
    {
        const FindOptions options = FindOptions.RemovePrefix | FindOptions.KeysOnly;
        var enumerator = snapshot
            .Find(CreateStorageKey(Prefix_BlockedAccount), SeekDirection.Forward)
            .GetEnumerator();
        return new StorageIterator(enumerator, 1, options);
    }
}
