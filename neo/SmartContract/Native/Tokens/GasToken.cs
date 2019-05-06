using Neo.Ledger;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using System.Text;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public class GasToken : NativeContractBase
    {
        public const string ServiceName = "Neo.Native.Tokens.GAS";
        public static readonly byte[] Script = CreateNativeScript(ServiceName);
        public static readonly UInt160 ScriptHash = Script.ToScriptHash();
        public static readonly string[] SupportedStandards = { "NEP-5", "NEP-10" };
        public const string Name = "GAS";
        public const string Symbol = "gas";
        public const int Decimals = 8;
        public static readonly BigInteger DecimalsFactor = BigInteger.Pow(10, Decimals);

        internal static bool Main(ApplicationEngine engine)
        {
            if (!new UInt160(engine.CurrentContext.ScriptHash).Equals(ScriptHash))
                return false;
            string operation = engine.CurrentContext.EvaluationStack.Pop().GetString();
            VMArray args = (VMArray)engine.CurrentContext.EvaluationStack.Pop();
            StackItem result;
            switch (operation)
            {
                case "supportedStandards":
                    result = SupportedStandards.Select(p => (StackItem)p).ToList();
                    break;
                case "name":
                    result = Name;
                    break;
                case "symbol":
                    result = Symbol;
                    break;
                case "decimals":
                    result = Decimals;
                    break;
                case "totalSupply":
                    result = TotalSupply(engine);
                    break;
                case "balanceOf":
                    result = BalanceOf(engine, args[0].GetByteArray());
                    break;
                case "transfer":
                    result = Transfer(engine, args[0].GetByteArray(), args[1].GetByteArray(), args[2].GetBigInteger());
                    break;
                default:
                    return false;
            }
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        private static BigInteger TotalSupply(ApplicationEngine engine)
        {
            StorageItem storage = engine.Service.Snapshot.Storages.TryGet(new StorageKey
            {
                ScriptHash = ScriptHash,
                Key = Encoding.ASCII.GetBytes("totalSupply")
            });
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        private static BigInteger BalanceOf(ApplicationEngine engine, byte[] account)
        {
            if (account.Length != 20) throw new ArgumentException();
            StorageItem storage = engine.Service.Snapshot.Storages.TryGet(new StorageKey
            {
                ScriptHash = ScriptHash,
                Key = account
            });
            if (storage is null) return BigInteger.Zero;
            Struct state = (Struct)storage.Value.DeserializeStackItem(engine.MaxArraySize);
            return state[0].GetBigInteger();
        }

        private static bool Transfer(ApplicationEngine engine, byte[] from, byte[] to, BigInteger amount)
        {
            if (engine.Service.Trigger != TriggerType.Application) throw new InvalidOperationException();
            UInt160 hash_from = new UInt160(from);
            UInt160 hash_to = new UInt160(to);
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!hash_from.Equals(new UInt160(engine.CurrentContext.CallingScriptHash)) && !engine.Service.CheckWitness(engine, new UInt160(from)))
                return false;
            ContractState contract_to = engine.Service.Snapshot.Contracts.TryGet(hash_to);
            if (contract_to?.Payable == false) return false;
            if (amount.Sign > 0)
            {
                StorageKey key_from = new StorageKey
                {
                    ScriptHash = ScriptHash,
                    Key = from
                };
                StorageItem storage_from = engine.Service.Snapshot.Storages.TryGet(key_from);
                if (storage_from is null) return false;
                AccountState state_from = AccountState.FromByteArray(storage_from.Value);
                if (state_from.Balance < amount) return false;
                if (!hash_from.Equals(hash_to))
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
                    StorageKey key_to = new StorageKey
                    {
                        ScriptHash = ScriptHash,
                        Key = to
                    };
                    StorageItem storage_to = engine.Service.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem
                    {
                        Value = new AccountState().ToByteArray()
                    });
                    AccountState state_to = AccountState.FromByteArray(storage_to.Value);
                    state_to.Balance += amount;
                    storage_to.Value = state_to.ToByteArray();
                }
            }
            engine.Service.SendNotification(engine, ScriptHash, new StackItem[] { "Transfer", from, to, amount });
            return true;
        }

        internal static void DistributeGas(ApplicationEngine engine, byte[] account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageKey key = new StorageKey
            {
                ScriptHash = ScriptHash,
                Key = account
            };
            StorageItem storage = engine.Service.Snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new AccountState().ToByteArray()
            });
            AccountState state = AccountState.FromByteArray(storage.Value);
            state.Balance += amount;
            storage.Value = state.ToByteArray();
            engine.Service.SendNotification(engine, ScriptHash, new StackItem[] { "Transfer", StackItem.Null, account, amount });
        }

        private class AccountState
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
