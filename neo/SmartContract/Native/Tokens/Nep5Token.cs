#pragma warning disable IDE0060

using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Numerics;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public abstract class Nep5Token<TState> : NativeContract
        where TState : Nep5AccountState, new()
    {
        public override string[] SupportedStandards { get; } = { "NEP-5", "NEP-10" };
        public abstract string Name { get; }
        public abstract string Symbol { get; }
        public abstract byte Decimals { get; }
        public BigInteger Factor { get; }

        protected const byte Prefix_TotalSupply = 11;
        protected const byte Prefix_Account = 20;

        protected Nep5Token()
        {
            this.Factor = BigInteger.Pow(10, Decimals);

            Manifest.Features = ContractFeatures.HasStorage;

            var events = new List<ContractEventDescriptor>(Manifest.Abi.Events)
            {
                new ContractMethodDescriptor()
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
                    },
                    ReturnType = ContractParameterType.Boolean
                }
            };

            Manifest.Abi.Events = events.ToArray();
        }

        internal protected virtual void Mint(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;

            TState state = new TState();
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(state.CreateAccountBalanceKey(this.Hash, Prefix_Account, account), () => new StorageItem
            {
                Value = new byte[0]
            });
            
            state.Balance = new BigInteger(storage.Value);
            OnBalanceChanging(engine, account, state, amount);
            state.Balance += amount;
            storage.Value = state.ToByteArray();
            storage = engine.Snapshot.Storages.GetAndChange(NativeContract.CreateStorageKey(this.Hash, Prefix_TotalSupply), () => new StorageItem
            {
                Value = BigInteger.Zero.ToByteArray()
            });
            BigInteger totalSupply = new BigInteger(storage.Value);
            totalSupply += amount;
            storage.Value = totalSupply.ToByteArray();
            engine.SendNotification(Hash, new StackItem[] { "Transfer", StackItem.Null, account.ToArray(), amount });
        }

        internal protected virtual void Burn(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;

            TState state = new TState();
            StorageKey key = state.CreateAccountBalanceKey(this.Hash, Prefix_Account, account);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
            
            state.Balance = new BigInteger(storage.Value);
            if (state.Balance < amount) throw new InvalidOperationException();
            OnBalanceChanging(engine, account, state, -amount);
            if (state.Balance == amount)
            {
                engine.Snapshot.Storages.Delete(key);
            }
            else
            {
                state.Balance -= amount;
                storage.Value = state.ToByteArray();
            }
            storage = engine.Snapshot.Storages.GetAndChange(NativeContract.CreateStorageKey(this.Hash, Prefix_TotalSupply));
            BigInteger totalSupply = new BigInteger(storage.Value);
            totalSupply -= amount;
            storage.Value = totalSupply.ToByteArray();
            engine.SendNotification(Hash, new StackItem[] { "Transfer", account.ToArray(), StackItem.Null, amount });
        }

        [ContractMethod(0, ContractParameterType.String, Name = "name", SafeMethod = true)]
        protected StackItem NameMethod(ApplicationEngine engine, VMArray args)
        {
            return Name;
        }

        [ContractMethod(0, ContractParameterType.String, Name = "symbol", SafeMethod = true)]
        protected StackItem SymbolMethod(ApplicationEngine engine, VMArray args)
        {
            return Symbol;
        }

        [ContractMethod(0, ContractParameterType.Integer, Name = "decimals", SafeMethod = true)]
        protected StackItem DecimalsMethod(ApplicationEngine engine, VMArray args)
        {
            return (uint)Decimals;
        }

        [ContractMethod(0_01000000, ContractParameterType.Integer, SafeMethod = true)]
        protected StackItem TotalSupply(ApplicationEngine engine, VMArray args)
        {
            return TotalSupply(engine.Snapshot);
        }

        public virtual BigInteger TotalSupply(Snapshot snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(NativeContract.CreateStorageKey(this.Hash, Prefix_TotalSupply));
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        [ContractMethod(0_01000000, ContractParameterType.Integer, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "account" }, SafeMethod = true)]
        protected StackItem BalanceOf(ApplicationEngine engine, VMArray args)
        {
            return BalanceOf(engine.Snapshot, new UInt160(args[0].GetByteArray()));
        }

        public virtual BigInteger BalanceOf(Snapshot snapshot, UInt160 account)
        {
            TState state = new TState();
            StorageItem storage = snapshot.Storages.TryGet(state.CreateAccountBalanceKey(this.Hash, Prefix_Account, account));
            if (storage is null) return BigInteger.Zero;
            state.Balance = new BigInteger(storage.Value);
            return state.Balance;
        }

        [ContractMethod(0_08000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer }, ParameterNames = new[] { "from", "to", "amount" }, AllowedTriggers = TriggerType.Application)]
        protected StackItem Transfer(ApplicationEngine engine, VMArray args)
        {
            UInt160 from = new UInt160(args[0].GetByteArray());
            UInt160 to = new UInt160(args[1].GetByteArray());
            BigInteger amount = args[2].GetBigInteger();
            return Transfer(engine, from, to, amount);
        }

        protected virtual bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!from.Equals(engine.CallingScriptHash) && !InteropService.CheckWitness(engine, from))
                return false;
            ContractState contract_to = engine.Snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;

            TState state_from = new TState();
            StorageKey key_from = state_from.CreateAccountBalanceKey(this.Hash, Prefix_Account, from);
            StorageItem storage_from = engine.Snapshot.Storages.TryGet(key_from);
            if (amount.IsZero)
            {
                if (storage_from != null)
                {
                    state_from.Balance = new BigInteger(storage_from.Value);
                    OnBalanceChanging(engine, from, state_from, amount);
                }
            }
            else
            {
                if (storage_from is null) return false;
                state_from.Balance = new BigInteger(storage_from.Value);
                if (state_from.Balance < amount) return false;
                if (from.Equals(to))
                {
                    OnBalanceChanging(engine, from, state_from, BigInteger.Zero);
                }
                else
                {
                    OnBalanceChanging(engine, from, state_from, -amount);
                    if (state_from.Balance == amount)
                    {
                        // TODO: this will be performed automatically if accepts: PR 824 and vm PR empty 0x00 bigint bytearray
                        engine.Snapshot.Storages.Delete(key_from);
                    }
                    else
                    {
                        state_from.Balance -= amount;
                        storage_from = engine.Snapshot.Storages.GetAndChange(key_from);
                        storage_from.Value = state_from.ToByteArray();
                    }

                    TState state_to = new TState();
                    StorageKey key_to = state_to.CreateAccountBalanceKey(this.Hash, Prefix_Account, to);
                    StorageItem storage_to = engine.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem
                    {
                        Value = new byte[0]{}
                    });
                    state_to.Balance = new BigInteger(storage_to.Value);
                    OnBalanceChanging(engine, to, state_to, amount);
                    state_to.Balance += amount;
                    storage_to.Value = state_to.Balance.ToByteArray();
                }
            }
            engine.SendNotification(Hash, new StackItem[] { "Transfer", from.ToArray(), to.ToArray(), amount });
            return true;
        }

        protected virtual void OnBalanceChanging(ApplicationEngine engine, UInt160 account, TState state, BigInteger amount)
        {
        }
    }
}
