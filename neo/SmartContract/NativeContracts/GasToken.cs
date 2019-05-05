using Neo.Ledger;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using System.Text;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class NeoService
    {
        private static readonly string[] GasToken_SupportedStandards = { "NEP-5", "NEP-10" };
        private const string GasToken_Name = "GAS";
        private const string GasToken_Symbol = "gas";
        private const int GasToken_Decimals = 8;
        private static readonly BigInteger GasToken_DecimalsFactor = BigInteger.Pow(10, GasToken_Decimals);

        private bool GasToken_Main(ExecutionEngine engine)
        {
            if (!new UInt160(engine.CurrentContext.ScriptHash).Equals(Blockchain.GasToken.ScriptHash))
                return false;
            string operation = engine.CurrentContext.EvaluationStack.Pop().GetString();
            VMArray args = (VMArray)engine.CurrentContext.EvaluationStack.Pop();
            StackItem result;
            switch (operation)
            {
                case "supportedStandards":
                    result = GasToken_SupportedStandards.Select(p => (StackItem)p).ToList();
                    break;
                case "name":
                    result = GasToken_Name;
                    break;
                case "symbol":
                    result = GasToken_Symbol;
                    break;
                case "decimals":
                    result = GasToken_Decimals;
                    break;
                case "totalSupply":
                    result = GasToken_TotalSupply();
                    break;
                case "balanceOf":
                    result = GasToken_BalanceOf(engine, args[0].GetByteArray());
                    break;
                case "transfer":
                    result = GasToken_Transfer(engine, args[0].GetByteArray(), args[1].GetByteArray(), args[2].GetBigInteger());
                    break;
                default:
                    return false;
            }
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        private BigInteger GasToken_TotalSupply()
        {
            StorageItem storage = Snapshot.Storages.TryGet(new StorageKey
            {
                ScriptHash = Blockchain.GasToken.ScriptHash,
                Key = Encoding.ASCII.GetBytes("totalSupply")
            });
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        private BigInteger GasToken_BalanceOf(ExecutionEngine engine, byte[] account)
        {
            if (account.Length != 20) throw new ArgumentException();
            StorageItem storage = Snapshot.Storages.TryGet(new StorageKey
            {
                ScriptHash = Blockchain.GasToken.ScriptHash,
                Key = account
            });
            if (storage is null) return BigInteger.Zero;
            Struct state = (Struct)storage.Value.DeserializeStackItem(engine.MaxArraySize);
            return state[0].GetBigInteger();
        }

        private bool GasToken_Transfer(ExecutionEngine engine, byte[] from, byte[] to, BigInteger amount)
        {
            if (Trigger != TriggerType.Application) throw new InvalidOperationException();
            UInt160 hash_from = new UInt160(from);
            UInt160 hash_to = new UInt160(to);
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!hash_from.Equals(new UInt160(engine.CurrentContext.CallingScriptHash)) && !CheckWitness(engine, new UInt160(from)))
                return false;
            ContractState contract_to = Snapshot.Contracts.TryGet(hash_to);
            if (contract_to?.Payable == false) return false;
            if (amount.Sign > 0)
            {
                StorageKey key_from = new StorageKey
                {
                    ScriptHash = Blockchain.GasToken.ScriptHash,
                    Key = from
                };
                StorageItem storage_from = Snapshot.Storages.TryGet(key_from);
                if (storage_from is null) return false;
                GasToken_AccountState state_from = GasToken_AccountState.FromStruct((Struct)storage_from.Value.DeserializeStackItem(engine.MaxArraySize));
                if (state_from.Balance < amount) return false;
                if (!hash_from.Equals(hash_to))
                {
                    if (state_from.Balance == amount)
                    {
                        Snapshot.Storages.Delete(key_from);
                    }
                    else
                    {
                        state_from.Balance -= amount;
                        storage_from = Snapshot.Storages.GetAndChange(key_from);
                        storage_from.Value = state_from.ToStruct().Serialize();
                    }
                    StorageKey key_to = new StorageKey
                    {
                        ScriptHash = Blockchain.GasToken.ScriptHash,
                        Key = to
                    };
                    StorageItem storage_to = Snapshot.Storages.GetAndChange(key_to, () => new StorageItem
                    {
                        Value = new GasToken_AccountState().ToStruct().Serialize()
                    });
                    GasToken_AccountState state_to = GasToken_AccountState.FromStruct((Struct)storage_to.Value.DeserializeStackItem(engine.MaxArraySize));
                    state_to.Balance += amount;
                    storage_to.Value = state_to.ToStruct().Serialize();
                }
            }
            SendNotification(engine, Blockchain.GasToken.ScriptHash, new StackItem[] { "Transfer", from, to, amount });
            return true;
        }

        private void GasToken_DistributeGas(ExecutionEngine engine, byte[] account, BigInteger amount)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            StorageKey key = new StorageKey
            {
                ScriptHash = Blockchain.GasToken.ScriptHash,
                Key = account
            };
            StorageItem storage = Snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new GasToken_AccountState().ToStruct().Serialize()
            });
            GasToken_AccountState state = GasToken_AccountState.FromStruct((Struct)storage.Value.DeserializeStackItem(engine.MaxArraySize));
            state.Balance += amount;
            storage.Value = state.ToStruct().Serialize();
            SendNotification(engine, Blockchain.GasToken.ScriptHash, new StackItem[] { "Transfer", StackItem.Null, account, amount });
        }

        private class GasToken_AccountState
        {
            public BigInteger Balance;

            public static GasToken_AccountState FromStruct(Struct @struct)
            {
                return new GasToken_AccountState
                {
                    Balance = @struct[0].GetBigInteger()
                };
            }

            public Struct ToStruct()
            {
                return new Struct(new StackItem[] { Balance });
            }
        }
    }
}
