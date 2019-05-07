using Neo.Ledger;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;
using System.Text;

namespace Neo.SmartContract.Native.Tokens
{
    public sealed class GasToken : Nep5Token
    {
        public override string ServiceName => "Neo.Native.Tokens.GAS";
        public override string Name => "GAS";
        public override string Symbol => "gas";
        public override int Decimals => 8;

        private GasToken()
        {
        }

        protected override BigInteger TotalSupply(ApplicationEngine engine)
        {
            StorageItem storage = engine.Service.Snapshot.Storages.TryGet(new StorageKey
            {
                ScriptHash = ScriptHash,
                Key = Encoding.ASCII.GetBytes("totalSupply")
            });
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        protected override bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount)
        {
            if (engine.Service.Trigger != TriggerType.Application) throw new InvalidOperationException();
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!from.Equals(new UInt160(engine.CurrentContext.CallingScriptHash)) && !engine.Service.CheckWitness(engine, from))
                return false;
            ContractState contract_to = engine.Service.Snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;
            if (amount.Sign > 0)
            {
                StorageKey key_from = CreateStorageKey(Prefix_Account, from);
                StorageItem storage_from = engine.Service.Snapshot.Storages.TryGet(key_from);
                if (storage_from is null) return false;
                AccountState state_from = AccountState.FromByteArray(storage_from.Value);
                if (state_from.Balance < amount) return false;
                if (!from.Equals(to))
                {
                    if (state_from.Balance == amount)
                    {
                        engine.Service.Snapshot.Storages.Delete(key_from);
                    }
                    else
                    {
                        state_from.Balance -= amount;
                        storage_from = engine.Service.Snapshot.Storages.GetAndChange(key_from);
                        storage_from.Value = state_from.ToByteArray();
                    }
                    StorageKey key_to = CreateStorageKey(Prefix_Account, to);
                    StorageItem storage_to = engine.Service.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem
                    {
                        Value = new AccountState().ToByteArray()
                    });
                    AccountState state_to = AccountState.FromByteArray(storage_to.Value);
                    state_to.Balance += amount;
                    storage_to.Value = state_to.ToByteArray();
                }
            }
            engine.Service.SendNotification(engine, ScriptHash, new StackItem[] { "Transfer", from.ToArray(), to.ToArray(), amount });
            return true;
        }

        internal void DistributeGas(ApplicationEngine engine, UInt160 account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageKey key = CreateStorageKey(Prefix_Account, account);
            StorageItem storage = engine.Service.Snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new AccountState().ToByteArray()
            });
            AccountState state = AccountState.FromByteArray(storage.Value);
            state.Balance += amount;
            storage.Value = state.ToByteArray();
            engine.Service.SendNotification(engine, ScriptHash, new StackItem[] { "Transfer", StackItem.Null, account.ToArray(), amount });
        }

        internal class AccountState
        {
            public BigInteger Balance;

            public static AccountState FromByteArray(byte[] data)
            {
                Struct @struct = (Struct)data.DeserializeStackItem(1);
                return new AccountState
                {
                    Balance = @struct[0].GetBigInteger()
                };
            }

            public byte[] ToByteArray()
            {
                return new Struct(new StackItem[] { Balance }).Serialize();
            }
        }
    }
}
