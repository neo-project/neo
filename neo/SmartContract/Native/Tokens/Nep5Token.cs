using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using System;
using System.Numerics;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public abstract class Nep5Token<TState> : NativeContract
        where TState : Nep5AccountState, new()
    {
        public override ContractPropertyState Properties => ContractPropertyState.HasStorage;
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
        }

        protected StorageKey CreateAccountKey(UInt160 account)
        {
            return CreateStorageKey(Prefix_Account, account);
        }

        protected override StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            switch (operation)
            {
                case "name":
                    return Name;
                case "symbol":
                    return Symbol;
                case "decimals":
                    return (uint)Decimals;
                case "totalSupply":
                    return TotalSupply(engine.Snapshot);
                case "balanceOf":
                    return BalanceOf(engine.Snapshot, new UInt160(args[0].GetByteArray()));
                case "transfer":
                    return Transfer(engine, new UInt160(args[0].GetByteArray()), new UInt160(args[1].GetByteArray()), args[2].GetBigInteger());
                default:
                    return base.Main(engine, operation, args);
            }
        }

        internal protected virtual void Mint(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateAccountKey(account), () => new StorageItem
            {
                Value = new TState().ToByteArray()
            });
            TState state = new TState();
            state.FromByteArray(storage.Value);
            OnBalanceChanging(engine, account, state, amount);
            state.Balance += amount;
            storage.Value = state.ToByteArray();
            storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem
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
            StorageKey key = CreateAccountKey(account);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
            TState state = new TState();
            state.FromByteArray(storage.Value);
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
            storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply));
            BigInteger totalSupply = new BigInteger(storage.Value);
            totalSupply -= amount;
            storage.Value = totalSupply.ToByteArray();
            engine.SendNotification(Hash, new StackItem[] { "Transfer", account.ToArray(), StackItem.Null, amount });
        }

        public virtual BigInteger TotalSupply(Snapshot snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_TotalSupply));
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        public virtual BigInteger BalanceOf(Snapshot snapshot, UInt160 account)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateAccountKey(account));
            if (storage is null) return BigInteger.Zero;
            Nep5AccountState state = new Nep5AccountState(storage.Value);
            return state.Balance;
        }

        protected virtual bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount)
        {
            if (engine.Trigger != TriggerType.Application) throw new InvalidOperationException();
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!from.Equals(engine.CallingScriptHash) && !InteropService.CheckWitness(engine, from))
                return false;
            ContractState contract_to = engine.Snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;
            StorageKey key_from = CreateAccountKey(from);
            StorageItem storage_from = engine.Snapshot.Storages.TryGet(key_from);
            if (amount.IsZero)
            {
                if (storage_from != null)
                {
                    TState state_from = new TState();
                    state_from.FromByteArray(storage_from.Value);
                    OnBalanceChanging(engine, from, state_from, amount);
                }
            }
            else
            {
                if (storage_from is null) return false;
                TState state_from = new TState();
                state_from.FromByteArray(storage_from.Value);
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
                        engine.Snapshot.Storages.Delete(key_from);
                    }
                    else
                    {
                        state_from.Balance -= amount;
                        storage_from = engine.Snapshot.Storages.GetAndChange(key_from);
                        storage_from.Value = state_from.ToByteArray();
                    }
                    StorageKey key_to = CreateAccountKey(to);
                    StorageItem storage_to = engine.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem
                    {
                        Value = new TState().ToByteArray()
                    });
                    TState state_to = new TState();
                    state_to.FromByteArray(storage_to.Value);
                    OnBalanceChanging(engine, to, state_to, amount);
                    state_to.Balance += amount;
                    storage_to.Value = state_to.ToByteArray();
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
