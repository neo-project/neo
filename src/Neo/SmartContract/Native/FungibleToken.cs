// Copyright (C) 2015-2025 The Neo Project.
//
// FungibleToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// The base class of all native tokens that are compatible with NEP-17.
    /// </summary>
    /// <typeparam name="TState">The type of account state.</typeparam>
    public abstract class FungibleToken<TState> : NativeContract
        where TState : AccountState, new()
    {
        /// <summary>
        /// The symbol of the token.
        /// </summary>
        [ContractMethod]
        public abstract string Symbol { get; }

        /// <summary>
        /// The number of decimal places of the token.
        /// </summary>
        [ContractMethod]
        public abstract byte Decimals { get; }

        /// <summary>
        /// The factor used when calculating the displayed value of the token value.
        /// </summary>
        public BigInteger Factor { get; }

        /// <summary>
        /// The prefix for storing total supply.
        /// </summary>
        protected const byte Prefix_TotalSupply = 11;

        /// <summary>
        /// The prefix for storing account states.
        /// </summary>
        protected internal const byte Prefix_Account = 20;

        /// <summary>
        /// Initializes a new instance of the <see cref="FungibleToken{TState}"/> class.
        /// </summary>
        [ContractEvent(0, name: "Transfer",
           "from", ContractParameterType.Hash160,
           "to", ContractParameterType.Hash160,
           "amount", ContractParameterType.Integer)]
        protected FungibleToken() : base()
        {
            Factor = BigInteger.Pow(10, Decimals);
        }

        protected override void OnManifestCompose(IsHardforkEnabledDelegate hfChecker, uint blockHeight, ContractManifest manifest)
        {
            manifest.SupportedStandards = new[] { "NEP-17" };
        }

        internal async ContractTask Mint(ApplicationEngine engine, UInt160 account, BigInteger amount, bool callOnPayment)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageItem storage = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Account, account), () => new StorageItem(new TState()));
            TState state = storage.GetInteroperable<TState>();
            OnBalanceChanging(engine, account, state, amount);
            state.Balance += amount;
            storage = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem(BigInteger.Zero));
            storage.Add(amount);
            await PostTransferAsync(engine, null, account, amount, StackItem.Null, callOnPayment);
        }

        internal async ContractTask Burn(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageKey key = CreateStorageKey(Prefix_Account, account);
            StorageItem storage = engine.SnapshotCache.GetAndChange(key);
            TState state = storage.GetInteroperable<TState>();
            if (state.Balance < amount) throw new InvalidOperationException();
            OnBalanceChanging(engine, account, state, -amount);
            if (state.Balance == amount)
                engine.SnapshotCache.Delete(key);
            else
                state.Balance -= amount;
            storage = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_TotalSupply));
            storage.Add(-amount);
            await PostTransferAsync(engine, account, null, amount, StackItem.Null, false);
        }

        /// <summary>
        /// Gets the total supply of the token.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The total supply of the token.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public virtual BigInteger TotalSupply(IReadOnlyStore snapshot)
        {
            var key = CreateStorageKey(Prefix_TotalSupply);
            return snapshot.TryGet(key, out var item) ? item : BigInteger.Zero;
        }

        /// <summary>
        /// Gets the balance of the specified account.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="account">The owner of the account.</param>
        /// <returns>The balance of the account. Or 0 if the account doesn't exist.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public virtual BigInteger BalanceOf(IReadOnlyStore snapshot, UInt160 account)
        {
            var key = CreateStorageKey(Prefix_Account, account);
            if (snapshot.TryGet(key, out var item))
                return item.GetInteroperable<TState>().Balance;
            return BigInteger.Zero;
        }

        [ContractMethod(CpuFee = 1 << 17, StorageFee = 50, RequiredCallFlags = CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify)]
        private protected async ContractTask<bool> Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, StackItem data)
        {
            if (from is null) throw new ArgumentNullException(nameof(from));
            if (to is null) throw new ArgumentNullException(nameof(to));
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!from.Equals(engine.CallingScriptHash) && !engine.CheckWitnessInternal(from))
                return false;
            StorageKey key_from = CreateStorageKey(Prefix_Account, from);
            StorageItem storage_from = engine.SnapshotCache.GetAndChange(key_from);
            if (amount.IsZero)
            {
                if (storage_from != null)
                {
                    TState state_from = storage_from.GetInteroperable<TState>();
                    OnBalanceChanging(engine, from, state_from, amount);
                }
            }
            else
            {
                if (storage_from is null) return false;
                TState state_from = storage_from.GetInteroperable<TState>();
                if (state_from.Balance < amount) return false;
                if (from.Equals(to))
                {
                    OnBalanceChanging(engine, from, state_from, BigInteger.Zero);
                }
                else
                {
                    OnBalanceChanging(engine, from, state_from, -amount);
                    if (state_from.Balance == amount)
                        engine.SnapshotCache.Delete(key_from);
                    else
                        state_from.Balance -= amount;
                    StorageKey key_to = CreateStorageKey(Prefix_Account, to);
                    StorageItem storage_to = engine.SnapshotCache.GetAndChange(key_to, () => new StorageItem(new TState()));
                    TState state_to = storage_to.GetInteroperable<TState>();
                    OnBalanceChanging(engine, to, state_to, amount);
                    state_to.Balance += amount;
                }
            }
            await PostTransferAsync(engine, from, to, amount, data, true);
            return true;
        }

        internal virtual void OnBalanceChanging(ApplicationEngine engine, UInt160 account, TState state, BigInteger amount)
        {
        }

        private protected virtual async ContractTask PostTransferAsync(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, StackItem data, bool callOnPayment)
        {
            // Send notification

            engine.SendNotification(Hash, "Transfer",
                new Array(engine.ReferenceCounter) { from?.ToArray() ?? StackItem.Null, to?.ToArray() ?? StackItem.Null, amount });

            // Check if it's a wallet or smart contract

            if (!callOnPayment || to is null || !ContractManagement.IsContract(engine.SnapshotCache, to)) return;

            // Call onNEP17Payment method

            await engine.CallFromNativeContractAsync(Hash, to, "onNEP17Payment", from?.ToArray() ?? StackItem.Null, amount, data);
        }
    }
}
