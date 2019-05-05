using Neo.Cryptography.ECC;
using Neo.IO;
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
        private static readonly string[] NeoToken_SupportedStandards = { "NEP-5", "NEP-10" };
        private const string NeoToken_Name = "NEO";
        private const string NeoToken_Symbol = "neo";
        private const int NeoToken_Decimals = 0;
        private static readonly BigInteger NeoToken_DecimalsFactor = BigInteger.Pow(10, NeoToken_Decimals);
        private static readonly BigInteger NeoToken_TotalAmount = 100000000 * NeoToken_DecimalsFactor;

        private bool NeoToken_Main(ExecutionEngine engine)
        {
            if (!new UInt160(engine.CurrentContext.ScriptHash).Equals(Blockchain.NeoToken.ScriptHash))
                return false;
            string operation = engine.CurrentContext.EvaluationStack.Pop().GetString();
            VMArray args = (VMArray)engine.CurrentContext.EvaluationStack.Pop();
            StackItem result;
            switch (operation)
            {
                case "supportedStandards":
                    result = NeoToken_SupportedStandards.Select(p => (StackItem)p).ToList();
                    break;
                case "name":
                    result = NeoToken_Name;
                    break;
                case "symbol":
                    result = NeoToken_Symbol;
                    break;
                case "decimals":
                    result = NeoToken_Decimals;
                    break;
                case "totalSupply":
                    result = NeoToken_TotalAmount;
                    break;
                case "balanceOf":
                    result = NeoToken_BalanceOf(engine, args[0].GetByteArray());
                    break;
                case "transfer":
                    result = NeoToken_Transfer(engine, args[0].GetByteArray(), args[1].GetByteArray(), args[2].GetBigInteger());
                    break;
                case "initialize":
                    result = NeoToken_Initialize();
                    break;
                default:
                    return false;
            }
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        private BigInteger NeoToken_BalanceOf(ExecutionEngine engine, byte[] account)
        {
            if (account.Length != 20) throw new ArgumentException();
            StorageItem storage = Snapshot.Storages.TryGet(new StorageKey
            {
                ScriptHash = Blockchain.NeoToken.ScriptHash,
                Key = account
            });
            if (storage is null) return BigInteger.Zero;
            Struct state = (Struct)storage.Value.DeserializeStackItem(engine.MaxArraySize);
            return state[0].GetBigInteger();
        }

        private bool NeoToken_Transfer(ExecutionEngine engine, byte[] from, byte[] to, BigInteger amount)
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
                    ScriptHash = Blockchain.NeoToken.ScriptHash,
                    Key = from
                };
                StorageItem storage_from = Snapshot.Storages.TryGet(key_from);
                if (storage_from is null) return false;
                NeoToken_AccountState state_from = NeoToken_AccountState.FromStruct((Struct)storage_from.Value.DeserializeStackItem(engine.MaxArraySize));
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
                        ScriptHash = Blockchain.NeoToken.ScriptHash,
                        Key = to
                    };
                    StorageItem storage_to = Snapshot.Storages.GetAndChange(key_to, () => new StorageItem
                    {
                        Value = new NeoToken_AccountState().ToStruct().Serialize()
                    });
                    NeoToken_AccountState state_to = NeoToken_AccountState.FromStruct((Struct)storage_to.Value.DeserializeStackItem(engine.MaxArraySize));
                    state_to.Balance += amount;
                    storage_to.Value = state_to.ToStruct().Serialize();
                }
            }
            SendNotification(engine, new StackItem[] { "Transfer", from, to, amount });
            return true;
        }

        private bool NeoToken_Initialize()
        {
            if (Trigger != TriggerType.Application) throw new InvalidOperationException();
            StorageKey key = new StorageKey
            {
                ScriptHash = Blockchain.NeoToken.ScriptHash,
                Key = Encoding.ASCII.GetBytes("initialized")
            };
            if (Snapshot.Storages.TryGet(key) != null) return false;
            Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new byte[] { 1 },
                IsConstant = true
            });
            UInt160 account = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1, Blockchain.StandbyValidators).ToScriptHash();
            key = new StorageKey
            {
                ScriptHash = Blockchain.NeoToken.ScriptHash,
                Key = account.ToArray()
            };
            Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new NeoToken_AccountState { Balance = NeoToken_TotalAmount }.ToStruct().Serialize()
            });
            return true;
        }

        private class NeoToken_AccountState
        {
            public BigInteger Balance;
            public uint BalanceHeight;
            public BigInteger GasAvailable;
            public ECPoint[] Votes = new ECPoint[0];

            public static NeoToken_AccountState FromStruct(Struct @struct)
            {
                return new NeoToken_AccountState
                {
                    Balance = @struct[0].GetBigInteger(),
                    BalanceHeight = (uint)@struct[1].GetBigInteger(),
                    GasAvailable = @struct[2].GetBigInteger(),
                    Votes = @struct[3].GetByteArray().AsSerializableArray<ECPoint>(Blockchain.MaxValidators)
                };
            }

            public Struct ToStruct()
            {
                return new Struct(new StackItem[]
                {
                    Balance,
                    BalanceHeight,
                    GasAvailable,
                    Votes.ToByteArray()
                });
            }
        }
    }
}
