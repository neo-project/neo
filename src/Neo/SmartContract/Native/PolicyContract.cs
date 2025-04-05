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
using Serilog;
using System;
using System.Diagnostics;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract that manages the system policies.
    /// </summary>
    public sealed class PolicyContract : NativeContract
    {
        /// <summary>
        /// Serilog logger instance
        /// </summary>
        private static readonly ILogger _log = Log.ForContext<PolicyContract>();

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

        private const byte Prefix_BlockedAccount = 15;
        private const byte Prefix_FeePerByte = 10;
        private const byte Prefix_ExecFeeFactor = 18;
        private const byte Prefix_StoragePrice = 19;
        private const byte Prefix_AttributeFee = 20;

        private readonly StorageKey _feePerByte;
        private readonly StorageKey _execFeeFactor;
        private readonly StorageKey _storagePrice;


        internal PolicyContract() : base()
        {
            _feePerByte = CreateStorageKey(Prefix_FeePerByte);
            _execFeeFactor = CreateStorageKey(Prefix_ExecFeeFactor);
            _storagePrice = CreateStorageKey(Prefix_StoragePrice);
        }

        internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
        {
            if (hardfork == ActiveIn)
            {
                _log.Information("Initializing PolicyContract state...");
                var sw = Stopwatch.StartNew();
                engine.SnapshotCache.Add(_feePerByte, new StorageItem(DefaultFeePerByte));
                engine.SnapshotCache.Add(_execFeeFactor, new StorageItem(DefaultExecFeeFactor));
                engine.SnapshotCache.Add(_storagePrice, new StorageItem(DefaultStoragePrice));
                sw.Stop();
                _log.Information("PolicyContract initial state (FeePerByte, ExecFeeFactor, StoragePrice) set in {DurationMs} ms", sw.ElapsedMilliseconds);
            }
            if (hardfork == Hardfork.HF_Echidna)
            {
                _log.Information("Initializing PolicyContract state for HF_Echidna (NotaryAssistedFee)...");
                var sw = Stopwatch.StartNew();
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_AttributeFee, (byte)TransactionAttributeType.NotaryAssisted), new StorageItem(DefaultNotaryAssistedAttributeFee));
                sw.Stop();
                _log.Information("PolicyContract HF_Echidna state set in {DurationMs} ms", sw.ElapsedMilliseconds);
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
            if (!Enum.IsDefined(typeof(TransactionAttributeType), attributeType) || (!allowNotaryAssisted && attributeType == (byte)(TransactionAttributeType.NotaryAssisted)))
                throw new InvalidOperationException($"Unsupported value {attributeType} of {nameof(attributeType)}");

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
            if (!Enum.IsDefined(typeof(TransactionAttributeType), attributeType) || (!allowNotaryAssisted && attributeType == (byte)(TransactionAttributeType.NotaryAssisted)))
                throw new InvalidOperationException($"Unsupported value {attributeType} of {nameof(attributeType)}");
            if (value > MaxAttributeFee) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            _log.Information("Setting attribute fee: Type={AttributeType}, Value={Value}", (TransactionAttributeType)attributeType, value);
            engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_AttributeFee, attributeType), () => new StorageItem(DefaultAttributeFee)).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetFeePerByte(ApplicationEngine engine, long value)
        {
            if (value < 0 || value > 1_00000000) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            _log.Information("Setting fee per byte to {FeePerByte}", value);
            engine.SnapshotCache.GetAndChange(_feePerByte).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetExecFeeFactor(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxExecFeeFactor) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            _log.Information("Setting execution fee factor to {ExecFeeFactor}", value);
            engine.SnapshotCache.GetAndChange(_execFeeFactor).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetStoragePrice(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxStoragePrice) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            _log.Information("Setting storage price to {StoragePrice}", value);
            engine.SnapshotCache.GetAndChange(_storagePrice).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private bool BlockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            _log.Warning("Attempting to block account {Account}", account);
            bool result = BlockAccount(engine.SnapshotCache, account);
            if (result)
                _log.Warning("Account {Account} blocked successfully", account);
            else
                _log.Warning("Account {Account} was already blocked or is native", account);
            return result;
        }

        internal bool BlockAccount(DataCache snapshot, UInt160 account)
        {
            if (IsNative(account))
            {
                _log.Warning("Attempt to block native contract {Account} denied", account);
                throw new InvalidOperationException("It's impossible to block a native contract.");
            }

            var key = CreateStorageKey(Prefix_BlockedAccount, account);
            if (snapshot.Contains(key))
            {
                _log.Verbose("Account {Account} is already in the blocked list", account);
                return false;
            }

            _log.Verbose("Adding account {Account} to the blocked list", account);
            snapshot.Add(key, new StorageItem(Array.Empty<byte>()));
            return true;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            _log.Warning("Attempting to unblock account {Account}", account);
            var key = CreateStorageKey(Prefix_BlockedAccount, account);
            if (!engine.SnapshotCache.Contains(key))
            {
                _log.Warning("Account {Account} is not currently blocked", account);
                return false;
            }

            engine.SnapshotCache.Delete(key);
            _log.Warning("Account {Account} unblocked successfully", account);
            return true;
        }
    }
}
