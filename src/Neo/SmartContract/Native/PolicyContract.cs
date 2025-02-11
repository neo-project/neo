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
        /// The default network fee per byte of transactions.
        /// In the unit of datoshi, 1 datoshi = 1e-8 GAS
        /// </summary>
        public const uint DefaultFeePerByte = 1000;

        /// <summary>
        /// The default fee for attribute.
        /// </summary>
        public const uint DefaultAttributeFee = 0;

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
                engine.SnapshotCache.Add(_feePerByte, new StorageItem(DefaultFeePerByte));
                engine.SnapshotCache.Add(_execFeeFactor, new StorageItem(DefaultExecFeeFactor));
                engine.SnapshotCache.Add(_storagePrice, new StorageItem(DefaultStoragePrice));
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
            if (!Enum.IsDefined(typeof(TransactionAttributeType), attributeType)) throw new InvalidOperationException();

            var key = CreateStorageKey(Prefix_AttributeFee).Add(attributeType);
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
            return snapshot.Contains(CreateStorageKey(Prefix_BlockedAccount).Add(account));
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetAttributeFee(ApplicationEngine engine, byte attributeType, uint value)
        {
            if (!Enum.IsDefined(typeof(TransactionAttributeType), attributeType)) throw new InvalidOperationException();
            if (value > MaxAttributeFee) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();

            engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_AttributeFee).Add(attributeType), () => new StorageItem(DefaultAttributeFee)).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetFeePerByte(ApplicationEngine engine, long value)
        {
            if (value < 0 || value > 1_00000000) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.SnapshotCache.GetAndChange(_feePerByte).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetExecFeeFactor(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxExecFeeFactor) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.SnapshotCache.GetAndChange(_execFeeFactor).Set(value);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetStoragePrice(ApplicationEngine engine, uint value)
        {
            if (value == 0 || value > MaxStoragePrice) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.SnapshotCache.GetAndChange(_storagePrice).Set(value);
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

            var key = CreateStorageKey(Prefix_BlockedAccount).Add(account);
            if (snapshot.Contains(key)) return false;

            snapshot.Add(key, new StorageItem(Array.Empty<byte>()));
            return true;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private bool UnblockAccount(ApplicationEngine engine, UInt160 account)
        {
            if (!CheckCommittee(engine)) throw new InvalidOperationException();

            var key = CreateStorageKey(Prefix_BlockedAccount).Add(account);
            if (!engine.SnapshotCache.Contains(key)) return false;

            engine.SnapshotCache.Delete(key);
            return true;
        }
    }
}
