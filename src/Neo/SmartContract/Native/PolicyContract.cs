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

using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        public const ulong MaxExecFeeFactor = 100;

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
        private const byte Prefix_WhitelistedFeeContracts = 16;
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
        private const ulong RequiredTimeForRecoverFund = 365 * 24 * 60 * 60 * 1_000UL; // 1 year in milliseconds

        /// <summary>
        /// The event name for the block generation time changed.
        /// </summary>
        private const string MillisecondsPerBlockChangedEventName = "MillisecondsPerBlockChanged";
        private const string RecoveredFundEventName = "RecoveredFund";
        private const string WhitelistChangedEventName = "WhitelistFeeChanged";

        [ContractEvent(Hardfork.HF_Echidna, 0, name: MillisecondsPerBlockChangedEventName,
            "old", ContractParameterType.Integer,
            "new", ContractParameterType.Integer
        )]
        [ContractEvent(Hardfork.HF_Faun, 1, name: WhitelistChangedEventName,
            "contract", ContractParameterType.Hash160,
            "method", ContractParameterType.String,
            "argCount", ContractParameterType.Integer,
            "fee", ContractParameterType.Any
        )]
        [ContractEvent(Hardfork.HF_Faun, 2, name: RecoveredFundEventName, "account", ContractParameterType.Hash160)]
        internal PolicyContract() : base()
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
            }
            if (hardfork == Hardfork.HF_Echidna)
            {
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_AttributeFee, (byte)TransactionAttributeType.NotaryAssisted), new StorageItem(DefaultNotaryAssistedAttributeFee));
                engine.SnapshotCache.Add(_millisecondsPerBlock, new StorageItem(engine.ProtocolSettings.MillisecondsPerBlock));
                engine.SnapshotCache.Add(_maxValidUntilBlockIncrement, new StorageItem(engine.ProtocolSettings.MaxValidUntilBlockIncrement));
                engine.SnapshotCache.Add(_maxTraceableBlocks, new StorageItem(engine.ProtocolSettings.MaxTraceableBlocks));
            }

            if (hardfork == Hardfork.HF_Faun)
            {
                // Add decimals to exec fee factor: after Faun Hardfork the unit is pico-gas, before it was datoshi.
                var item = engine.SnapshotCache.GetAndChange(_execFeeFactor) ??
                    throw new InvalidOperationException("Policy was not initialized");
                item.Set((uint)(BigInteger)item * ApplicationEngine.FeeFactor);

                // Add timestamp of the current block to blocked acconuts.
                var time = engine.GetTime();
                foreach (var (key, _) in engine.SnapshotCache.Find(CreateStorageKey(Prefix_BlockedAccount), SeekDirection.Forward))
                {
                    var blockedAcc = engine.SnapshotCache.GetAndChange(key)!;
                    blockedAcc.Set(time);
                }
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
        /// <param name="engine">The execution engine.</param>
        /// <returns>The execution fee factor.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint GetExecFeeFactor(ApplicationEngine engine)
        {
            if (engine.IsHardforkEnabled(Hardfork.HF_Faun))
                return (uint)((BigInteger)engine.SnapshotCache[_execFeeFactor] / ApplicationEngine.FeeFactor);

            return (uint)(BigInteger)engine.SnapshotCache[_execFeeFactor];
        }

        public long GetExecFeeFactor(ProtocolSettings settings, IReadOnlyStore snapshot, uint index)
        {
            if (settings.IsHardforkEnabled(Hardfork.HF_Faun, index))
                return (long)((BigInteger)snapshot[_execFeeFactor] / ApplicationEngine.FeeFactor);

            return (long)(BigInteger)snapshot[_execFeeFactor];
        }

        /// <summary>
        /// Gets the execution fee factor. This is a multiplier that can be adjusted by the committee to adjust the system fees for transactions.
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <returns>The execution fee factor in the unit of pico Gas. 1 picoGAS = 1e-12 GAS</returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public BigInteger GetExecPicoFeeFactor(ApplicationEngine engine)
        {
            return (BigInteger)engine.SnapshotCache[_execFeeFactor];
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
        /// Gets the fee for attribute before Echidna hardfork. NotaryAssisted attribute type not supported.
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
        /// Gets the fee for attribute after Echidna hardfork. NotaryAssisted attribute type supported.
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

        internal bool IsWhitelistFeeContract(DataCache snapshot, UInt160 contractHash, ContractMethodDescriptor method, [NotNullWhen(true)] out long? fixedFee)
        {
            // Check contract existence

            var currentContract = ContractManagement.GetContract(snapshot, contractHash);

            if (currentContract != null)
            {
                // Check state existence

                var item = snapshot.TryGet(CreateStorageKey(Prefix_WhitelistedFeeContracts, contractHash, method.Offset))?.GetInteroperable<WhitelistedContract>();

                if (item != null)
                {
                    fixedFee = item.FixedFee;
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
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
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
            engine.SendNotification(Hash, WhitelistChangedEventName,
                [new ByteString(contractHash.ToArray()), method, argCount, StackItem.Null]);
        }

        internal int CleanWhitelist(ApplicationEngine engine, ContractState contract)
        {
            var count = 0;
            var searchKey = CreateStorageKey(Prefix_WhitelistedFeeContracts, contract.Hash);

            foreach (var (key, value) in engine.SnapshotCache.Find(searchKey, SeekDirection.Forward))
            {
                engine.SnapshotCache.Delete(key);
                count++;

                var data = value.GetInteroperable<WhitelistedContract>();

                engine.SendNotification(Hash, WhitelistChangedEventName,
                    [
                    new ByteString(contract.Hash.ToArray()),
                    data.Method,
                    data.ArgCount,
                    StackItem.Null
                    ]);
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
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        internal void SetWhitelistFeeContract(ApplicationEngine engine, UInt160 contractHash, string method, int argCount, long fixedFee)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(fixedFee, nameof(fixedFee));

            if (!CheckCommittee(engine)) throw new InvalidOperationException("Invalid committee signature");

            // Validate methods
            var contract = ContractManagement.GetContract(engine.SnapshotCache, contractHash)
                    ?? throw new InvalidOperationException("Is not a valid contract");

            // If exists multiple instance a exception is throwed
            var methodDescriptor = contract.Manifest.Abi.Methods.SingleOrDefault(u => u.Name == method && u.Parameters.Length == argCount) ??
                throw new InvalidOperationException($"Method {method} with {argCount} args was not found in {contractHash}");
            var key = CreateStorageKey(Prefix_WhitelistedFeeContracts, contractHash, methodDescriptor.Offset);

            // Set
            var entry = engine.SnapshotCache.GetAndChange(key, () => new StorageItem(new WhitelistedContract()
            {
                ContractHash = contractHash,
                Method = method,
                ArgCount = argCount,
                FixedFee = fixedFee
            }));
            entry.GetInteroperable<WhitelistedContract>().FixedFee = fixedFee;
            entry.Seal();

            // Emit event
            engine.SendNotification(Hash, WhitelistChangedEventName, [new VM.Types.ByteString(contractHash.ToArray()), method, argCount, fixedFee]);
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
                throw new ArgumentOutOfRangeException(nameof(value), $"MillisecondsPerBlock must be between [1, {MaxMillisecondsPerBlock}], got {value}");
            AssertCommittee(engine);

            var oldTime = GetMillisecondsPerBlock(engine.SnapshotCache);
            engine.SnapshotCache.GetAndChange(_millisecondsPerBlock)!.Set(value);

            engine.SendNotification(Hash, MillisecondsPerBlockChangedEventName,
                [new VM.Types.Integer(oldTime), new VM.Types.Integer(value)]);
        }

        /// <summary>
        /// Sets the fee for attribute before Echidna hardfork. NotaryAssisted attribute type not supported.
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
        /// Sets the fee for attribute after Echidna hardfork. NotaryAssisted attribute type supported.
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
        private void SetExecFeeFactor(ApplicationEngine engine, ulong value)
        {
            // After FAUN hardfork, the max exec fee factor is with decimals defined in ApplicationEngine.FeeFactor
            var maxValue = engine.IsHardforkEnabled(Hardfork.HF_Faun) ? ApplicationEngine.FeeFactor * MaxExecFeeFactor : MaxExecFeeFactor;

            if (value == 0 || value > maxValue)
                throw new ArgumentOutOfRangeException(nameof(value), $"ExecFeeFactor must be between [1, {maxValue}], got {value}");

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

        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
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

            AssertCommittee(engine);

            engine.SnapshotCache.GetAndChange(_maxTraceableBlocks)!.Set(value);
        }

        [ContractMethod(true, Hardfork.HF_Faun, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States, Name = "blockAccount")]
        private async ContractTask<bool> BlockAccountV0(ApplicationEngine engine, UInt160 account)
        {
            AssertCommittee(engine);

            return await BlockAccountInternal(engine, account);
        }

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify, Name = "blockAccount")]
        private async ContractTask<bool> BlockAccountV1(ApplicationEngine engine, UInt160 account)
        {
            AssertCommittee(engine);

            return await BlockAccountInternal(engine, account);
        }

        internal async ContractTask<bool> BlockAccountInternal(ApplicationEngine engine, UInt160 account)
        {
            if (IsNative(account)) throw new InvalidOperationException("Cannot block a native contract.");

            var key = CreateStorageKey(Prefix_BlockedAccount, account);

            if (engine.SnapshotCache.Contains(key)) return false;

            if (engine.IsHardforkEnabled(Hardfork.HF_Faun))
                await NEO.VoteInternal(engine, account, null);

            engine.SnapshotCache.Add(key,
                // Set request time for recover funds
                engine.IsHardforkEnabled(Hardfork.HF_Faun) ? new StorageItem(engine.GetTime())
                : new StorageItem([]));

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

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private StorageIterator GetBlockedAccounts(DataCache snapshot)
        {
            const FindOptions options = FindOptions.RemovePrefix | FindOptions.KeysOnly;
            var enumerator = snapshot
                .Find(CreateStorageKey(Prefix_BlockedAccount), SeekDirection.Forward)
                .GetEnumerator();
            return new StorageIterator(enumerator, 1, options);
        }

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        internal async ContractTask<bool> RecoverFund(ApplicationEngine engine, UInt160 account, UInt160 token)
        {
            var committeeMultiSigAddr = AssertAlmostFullCommittee(engine);

            // Set request time

            var key = CreateStorageKey(Prefix_BlockedAccount, account);
            var entry = engine.SnapshotCache.GetAndChange(key, null)
                ?? throw new InvalidOperationException("Request not found.");
            var elapsedTime = engine.GetTime() - (BigInteger)entry;
            if (elapsedTime < RequiredTimeForRecoverFund)
            {
                var remaining = (BigInteger)RequiredTimeForRecoverFund - elapsedTime;
                var days = remaining / 86_400_000;
                var hours = (remaining % 86_400_000) / 3_600_000;
                var minutes = (remaining % 3_600_000) / 60_000;
                var seconds = (remaining % 60_000) / 1_000;
                var timeMsg = days > 0 ? $"{days}d {hours}h {minutes}m"
                    : hours > 0 ? $"{hours}h {minutes}m {seconds}s"
                    : minutes > 0 ? $"{minutes}m {seconds}s"
                    : $"{seconds}s";
                throw new InvalidOperationException($"Request must be signed at least 1 year ago. Remaining time: {timeMsg}.");
            }

            // Validate contract exists
            var contract = ContractManagement.GetContract(engine.SnapshotCache, token)
                ?? throw new InvalidOperationException($"Contract {token} does not exist.");

            // Validate contract implements NEP-17 standard
            if (!contract.Manifest.SupportedStandards.Contains("NEP-17"))
                throw new InvalidOperationException($"Contract {token} does not implement NEP-17 standard.");

            // Check balance
            var balance = await engine.CallFromNativeContractAsync<BigInteger>(account, token, "balanceOf", account.ToArray());

            if (balance > 0)
            {
                // Transfer
                var result = await engine.CallFromNativeContractAsync<bool>(account, token, "transfer",
                    account.ToArray(), NativeContract.Treasury.Hash.ToArray(), balance, StackItem.Null);

                if (!result)
                    throw new InvalidOperationException($"Transfer of {balance} from {account} to {NativeContract.Treasury.Hash} failed in contract {token}.");

                // notify
                engine.SendNotification(Hash, RecoveredFundEventName, [new ByteString(account.ToArray())]);
                return true;
            }

            return false;
        }

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        internal StorageIterator GetWhitelistFeeContracts(DataCache snapshot)
        {
            const FindOptions options = FindOptions.RemovePrefix | FindOptions.ValuesOnly | FindOptions.DeserializeValues;
            var enumerator = snapshot
                .Find(CreateStorageKey(Prefix_WhitelistedFeeContracts), SeekDirection.Forward)
                .GetEnumerator();

            return new StorageIterator(enumerator, 1, options);
        }
    }
}
