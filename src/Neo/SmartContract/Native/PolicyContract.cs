// Copyright (C) 2015-2026 The Neo Project.
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
using Neo.SmartContract.Manifest;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Neo.SmartContract.Native;

/// <summary>
/// A native contract that manages the system policies.
/// </summary>
[ContractEvent(0, name: WhitelistChangedEventName,
    "contract", ContractParameterType.Hash160,
    "method", ContractParameterType.String,
    "argCount", ContractParameterType.Integer,
    "fee", ContractParameterType.Any
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

    private const byte Prefix_FeePerByte = 10;
    private const byte Prefix_BlockedAccount = 15;
    private const byte Prefix_WhitelistedFeeContracts = 16;
    private const byte Prefix_ExecFeeFactor = 18;
    private const byte Prefix_StoragePrice = 19;
    private const byte Prefix_AttributeFee = 20;

    private readonly StorageKey _feePerByte;
    private readonly StorageKey _execFeeFactor;
    private readonly StorageKey _storagePrice;

    private const string WhitelistChangedEventName = "WhitelistFeeChanged";

    internal PolicyContract() : base(-7)
    {
        _feePerByte = CreateStorageKey(Prefix_FeePerByte);
        _execFeeFactor = CreateStorageKey(Prefix_ExecFeeFactor);
        _storagePrice = CreateStorageKey(Prefix_StoragePrice);
    }

    internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
    {
        if (hardfork == ActiveIn)
        {
            engine.SnapshotCache.Add(_feePerByte, new StorageItem(DefaultFeePerByte));
            engine.SnapshotCache.Add(_execFeeFactor, new StorageItem(DefaultExecFeeFactor));
            engine.SnapshotCache.Add(_storagePrice, new StorageItem(DefaultStoragePrice));
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_AttributeFee, (byte)TransactionAttributeType.NotaryAssisted), new StorageItem(DefaultNotaryAssistedAttributeFee));
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
    /// Gets the fee for attribute.
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
    /// Sets the fee for attribute.
    /// </summary>
    /// <param name="engine">The engine used to check committee witness and read data.</param>
    /// <param name="attributeType">Attribute type</param>
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

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
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

        await Governance.VoteInternal(engine, account, null);

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

    internal bool IsWhitelistFeeContract(IReadOnlyStore snapshot, UInt160 contractHash, ContractMethodDescriptor method, [NotNullWhen(true)] out long? fixedFee)
    {
        // Check contract existence

        var currentContract = ContractManagement.GetContract(snapshot, contractHash);

        if (currentContract != null)
        {
            // Check state existence

            var item = snapshot.TryGet(CreateStorageKey(Prefix_WhitelistedFeeContracts, contractHash, method.Offset));

            if (item != null)
            {
                fixedFee = (long)(BigInteger)item;
                return true;
            }
        }

        fixedFee = null;
        return false;
    }

    /// <summary>
    /// Remove whitelisted Fee contracts
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="contractHash">The contract to set the whitelist</param>
    /// <param name="method">Method</param>
    /// <param name="argCount">Argument count</param>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    private void RemoveWhitelistFeeContract(ApplicationEngine engine, UInt160 contractHash, string method, int argCount)
    {
        if (!CheckCommittee(engine)) throw new InvalidOperationException("Invalid committee signature");

        // Validate methods
        var contract = ContractManagement.GetContract(engine.SnapshotCache, contractHash)
                ?? throw new InvalidOperationException("Is not a valid contract");

        // If exists multiple instance a exception is throwed
        var methodDescriptor = contract.Manifest.Abi.Methods.SingleOrDefault(u => u.Name == method && u.Parameters.Length == argCount) ??
            throw new InvalidOperationException($"Method {method} with {argCount} args was not found in {contractHash}");
        var key = CreateStorageKey(Prefix_WhitelistedFeeContracts, contractHash, methodDescriptor.Offset);

        if (!engine.SnapshotCache.Contains(key)) throw new InvalidOperationException("Whitelist not found");

        engine.SnapshotCache.Delete(key);

        // Emit event
        Notify(engine, WhitelistChangedEventName, contractHash, method, argCount, null);
    }

    internal int CleanWhitelist(ApplicationEngine engine, ContractState contract)
    {
        var count = 0;
        var searchKey = CreateStorageKey(Prefix_WhitelistedFeeContracts, contract.Hash);

        foreach ((var key, _) in engine.SnapshotCache.Find(searchKey, SeekDirection.Forward))
        {
            engine.SnapshotCache.Delete(key);
            count++;

            // Emit event recovering the values from the Key

            var keyData = key.ToArray().AsSpan();
            var methodOffset = BinaryPrimitives.ReadInt32BigEndian(keyData.Slice(sizeof(int) + sizeof(byte) + UInt160.Length, sizeof(int)));

            // Get method for event
            var method = contract.Manifest.Abi.Methods.FirstOrDefault(m => m.Offset == methodOffset);

            Notify(engine, WhitelistChangedEventName, contract.Hash, method?.Name, method?.Parameters.Length, null);
        }

        return count;
    }

    /// <summary>
    /// Set whitelisted Fee contracts
    /// </summary>
    /// <param name="engine">The execution engine.</param>
    /// <param name="contractHash">The contract to set the whitelist</param>
    /// <param name="method">Method</param>
    /// <param name="argCount">Argument count</param>
    /// <param name="fixedFee">Fixed execution fee</param>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    internal void SetWhitelistFeeContract(ApplicationEngine engine, UInt160 contractHash, string method, int argCount, long fixedFee)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fixedFee, nameof(fixedFee));

        if (!CheckCommittee(engine)) throw new InvalidOperationException("Invalid committee signature");

        // Validate methods
        var contract = ContractManagement.GetContract(engine.SnapshotCache, contractHash)
                ?? throw new InvalidOperationException("Is not a valid contract");

        if (contract.Manifest.Abi.GetMethod(method, argCount) is null)
            throw new InvalidOperationException($"{method} with {argCount} args is not a valid method of {contractHash}");

        // If exists multiple instance a exception is throwed
        var methodDescriptor = contract.Manifest.Abi.Methods.SingleOrDefault(u => u.Name == method && u.Parameters.Length == argCount) ??
            throw new InvalidOperationException($"Method {method} with {argCount} args was not found in {contractHash}");
        var key = CreateStorageKey(Prefix_WhitelistedFeeContracts, contractHash, methodDescriptor.Offset);

        // Set
        var entry = engine.SnapshotCache
                .GetAndChange(key, () => new StorageItem(fixedFee));

        entry.Set(fixedFee);

        // Emit event

        Notify(engine, WhitelistChangedEventName, contractHash, method, argCount, fixedFee);
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    internal StorageIterator GetWhitelistFeeContracts(IReadOnlyStore snapshot)
    {
        const FindOptions options = FindOptions.RemovePrefix | FindOptions.KeysOnly;
        var enumerator = snapshot
            .Find(CreateStorageKey(Prefix_WhitelistedFeeContracts), SeekDirection.Forward)
            .GetEnumerator();

        return new StorageIterator(enumerator, 1, options);
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    private StorageIterator GetBlockedAccounts(IReadOnlyStore snapshot)
    {
        const FindOptions options = FindOptions.RemovePrefix | FindOptions.KeysOnly;
        var enumerator = snapshot
            .Find(CreateStorageKey(Prefix_BlockedAccount), SeekDirection.Forward)
            .GetEnumerator();
        return new StorageIterator(enumerator, 1, options);
    }
}
