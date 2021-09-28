// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
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
        protected const byte Prefix_Account = 20;

        /// <summary>
        /// Initializes a new instance of the <see cref="FungibleToken{TState}"/> class.
        /// </summary>
        protected FungibleToken()
        {
            this.Factor = BigInteger.Pow(10, Decimals);

            Manifest.SupportedStandards = new[] { "NEP-17" };

            var events = new List<ContractEventDescriptor>(Manifest.Abi.Events)
            {
                new ContractEventDescriptor
                {
                    Name = "Transfer",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "from",
                            Type = ContractParameterType.Hash160
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "to",
                            Type = ContractParameterType.Hash160
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "amount",
                            Type = ContractParameterType.Integer
                        }
                    }
                }
            };

            Manifest.Abi.Events = events.ToArray();
        }

        internal async ContractTask Mint(ApplicationEngine engine, UInt160 account, BigInteger amount, bool callOnPayment)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageItem storage = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Account).Add(account), () => new StorageItem(new TState()));
            TState state = storage.GetInteroperable<TState>();
            await OnBalanceChanging(engine, account, state, amount);
            state.Balance += amount;
            storage = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem(BigInteger.Zero));
            storage.Add(amount);
            await PostTransfer(engine, null, account, amount, StackItem.Null, callOnPayment);
        }

        internal async ContractTask Burn(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageKey key = CreateStorageKey(Prefix_Account).Add(account);
            StorageItem storage = engine.Snapshot.GetAndChange(key);
            TState state = storage.GetInteroperable<TState>();
            if (state.Balance < amount) throw new InvalidOperationException();
            await OnBalanceChanging(engine, account, state, -amount);
            if (state.Balance == amount)
                engine.Snapshot.Delete(key);
            else
                state.Balance -= amount;
            storage = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_TotalSupply));
            storage.Add(-amount);
            await PostTransfer(engine, account, null, amount, StackItem.Null, false);
        }

        /// <summary>
        /// Gets the total supply of the token.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The total supply of the token.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public virtual BigInteger TotalSupply(DataCache snapshot)
        {
            StorageItem storage = snapshot.TryGet(CreateStorageKey(Prefix_TotalSupply));
            if (storage is null) return BigInteger.Zero;
            return storage;
        }

        /// <summary>
        /// Gets the balance of the specified account.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="account">The owner of the account.</param>
        /// <returns>The balance of the account. Or 0 if the account doesn't exist.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public virtual BigInteger BalanceOf(DataCache snapshot, UInt160 account)
        {
            StorageItem storage = snapshot.TryGet(CreateStorageKey(Prefix_Account).Add(account));
            if (storage is null) return BigInteger.Zero;
            return storage.GetInteroperable<TState>().Balance;
        }

        [ContractMethod(CpuFee = 1 << 17, StorageFee = 50, RequiredCallFlags = CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify)]
        private protected async ContractTask<bool> Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, StackItem data)
        {
            if (from is null) throw new ArgumentNullException(nameof(from));
            if (to is null) throw new ArgumentNullException(nameof(to));
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!from.Equals(engine.CallingScriptHash) && !engine.CheckWitnessInternal(from))
                return false;
            StorageKey key_from = CreateStorageKey(Prefix_Account).Add(from);
            StorageItem storage_from = engine.Snapshot.GetAndChange(key_from);
            if (amount.IsZero)
            {
                if (storage_from != null)
                {
                    TState state_from = storage_from.GetInteroperable<TState>();
                    await OnBalanceChanging(engine, from, state_from, amount);
                }
            }
            else
            {
                if (storage_from is null) return false;
                TState state_from = storage_from.GetInteroperable<TState>();
                if (state_from.Balance < amount) return false;
                if (from.Equals(to))
                {
                    await OnBalanceChanging(engine, from, state_from, BigInteger.Zero);
                }
                else
                {
                    await OnBalanceChanging(engine, from, state_from, -amount);
                    if (state_from.Balance == amount)
                        engine.Snapshot.Delete(key_from);
                    else
                        state_from.Balance -= amount;
                    StorageKey key_to = CreateStorageKey(Prefix_Account).Add(to);
                    StorageItem storage_to = engine.Snapshot.GetAndChange(key_to, () => new StorageItem(new TState()));
                    TState state_to = storage_to.GetInteroperable<TState>();
                    await OnBalanceChanging(engine, to, state_to, amount);
                    state_to.Balance += amount;
                }
            }
            await PostTransfer(engine, from, to, amount, data, true);
            return true;
        }

        internal virtual ContractTask OnBalanceChanging(ApplicationEngine engine, UInt160 account, TState state, BigInteger amount)
        {
            return ContractTask.CompletedTask;
        }

        private async ContractTask PostTransfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, StackItem data, bool callOnPayment)
        {
            // Send notification

            engine.SendNotification(Hash, "Transfer",
                new Array { from?.ToArray() ?? StackItem.Null, to?.ToArray() ?? StackItem.Null, amount });

            // Check if it's a wallet or smart contract

            if (!callOnPayment || to is null || ContractManagement.GetContract(engine.Snapshot, to) is null) return;

            // Call onNEP17Payment method

            await engine.CallFromNativeContract(Hash, to, "onNEP17Payment", from?.ToArray() ?? StackItem.Null, amount, data);
        }
    }
}
