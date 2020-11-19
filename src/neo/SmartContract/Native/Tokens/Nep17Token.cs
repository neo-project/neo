using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public abstract class Nep17Token<TState> : NativeContract
        where TState : AccountState, new()
    {
        [ContractMethod(0, CallFlags.None)]
        public abstract string Symbol { get; }
        [ContractMethod(0, CallFlags.None)]
        public abstract byte Decimals { get; }
        public BigInteger Factor { get; }

        protected const byte Prefix_TotalSupply = 11;
        protected const byte Prefix_Account = 20;

        protected Nep17Token()
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

        internal protected virtual void Mint(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Account).Add(account), () => new StorageItem(new TState()));
            TState state = storage.GetInteroperable<TState>();
            OnBalanceChanging(engine, account, state, amount);
            state.Balance += amount;
            storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem(BigInteger.Zero));
            storage.Add(amount);
            PostTransfer(engine, null, account, amount);
        }

        internal protected virtual void Burn(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageKey key = CreateStorageKey(Prefix_Account).Add(account);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
            TState state = storage.GetInteroperable<TState>();
            if (state.Balance < amount) throw new InvalidOperationException();
            OnBalanceChanging(engine, account, state, -amount);
            if (state.Balance == amount)
                engine.Snapshot.Storages.Delete(key);
            else
                state.Balance -= amount;
            storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply));
            storage.Add(-amount);
            PostTransfer(engine, account, null, amount);
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public virtual BigInteger TotalSupply(StoreView snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_TotalSupply));
            if (storage is null) return BigInteger.Zero;
            return storage;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public virtual BigInteger BalanceOf(StoreView snapshot, UInt160 account)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_Account).Add(account));
            if (storage is null) return BigInteger.Zero;
            return storage.GetInteroperable<TState>().Balance;
        }

        [ContractMethod(0_09000000, CallFlags.AllowModifyStates)]
        protected virtual bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!from.Equals(engine.CallingScriptHash) && !engine.CheckWitnessInternal(from))
                return false;
            StorageKey key_from = CreateStorageKey(Prefix_Account).Add(from);
            StorageItem storage_from = engine.Snapshot.Storages.GetAndChange(key_from);
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
                        engine.Snapshot.Storages.Delete(key_from);
                    else
                        state_from.Balance -= amount;
                    StorageKey key_to = CreateStorageKey(Prefix_Account).Add(to);
                    StorageItem storage_to = engine.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem(new TState()));
                    TState state_to = storage_to.GetInteroperable<TState>();
                    OnBalanceChanging(engine, to, state_to, amount);
                    state_to.Balance += amount;
                }
            }
            PostTransfer(engine, from, to, amount);
            return true;
        }

        protected virtual void OnBalanceChanging(ApplicationEngine engine, UInt160 account, TState state, BigInteger amount)
        {
        }

        private void PostTransfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount)
        {
            // Send notification

            engine.SendNotification(Hash, "Transfer",
                new Array { from?.ToArray() ?? StackItem.Null, to?.ToArray() ?? StackItem.Null, amount });

            // Check if it's a wallet or smart contract

            if (to is null || engine.Snapshot.Contracts.TryGet(to) is null) return;

            // Call onPayment method (NEP-17)

            engine.CallFromNativeContract(null, to, "onPayment", from.ToArray(), amount);
        }
    }
}
