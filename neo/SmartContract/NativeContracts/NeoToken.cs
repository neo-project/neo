using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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

        private const byte NeoToken_Prefix_Initialized = 11;
        private const byte NeoToken_Prefix_Account = 20;
        private const byte NeoToken_Prefix_Validator = 33;
        private const byte NeoToken_Prefix_ValidatorsCount = 15;

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
                    result = NeoToken_Initialize(engine);
                    break;
                case "unclaimedGas":
                    result = NeoToken_UnclaimedGas(args[0].GetByteArray(), (uint)args[1].GetBigInteger());
                    break;
                case "registerValidator":
                    result = NeoToken_RegisterValidator(args[0].GetByteArray());
                    break;
                case "vote":
                    result = NeoToken_Vote(engine, args[0].GetByteArray(), ((VMArray)args[1]).Select(p => p.GetByteArray().AsSerializable<ECPoint>()).ToArray());
                    break;
                case "getValidators":
                    result = NeoToken_GetValidators().Select(p => (StackItem)p.ToArray()).ToArray();
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
            StorageItem storage = Snapshot.Storages.TryGet(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Account, account));
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
            StorageKey key_from = CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Account, from);
            StorageItem storage_from = Snapshot.Storages.TryGet(key_from);
            if (amount.IsZero)
            {
                if (storage_from != null)
                {
                    NeoToken_AccountState state_from = NeoToken_AccountState.FromByteArray(storage_from.Value);
                    NeoToken_DistributeGas(engine, from, state_from);
                    storage_from = Snapshot.Storages.GetAndChange(key_from);
                    storage_from.Value = state_from.ToByteArray();
                }
            }
            else
            {
                if (storage_from is null) return false;
                NeoToken_AccountState state_from = NeoToken_AccountState.FromByteArray(storage_from.Value);
                if (state_from.Balance < amount) return false;
                NeoToken_DistributeGas(engine, from, state_from);
                if (hash_from.Equals(hash_to))
                {
                    storage_from = Snapshot.Storages.GetAndChange(key_from);
                    storage_from.Value = state_from.ToByteArray();
                }
                else
                {
                    if (state_from.Balance == amount)
                    {
                        Snapshot.Storages.Delete(key_from);
                    }
                    else
                    {
                        state_from.Balance -= amount;
                        storage_from = Snapshot.Storages.GetAndChange(key_from);
                        storage_from.Value = state_from.ToByteArray();
                    }
                    if (state_from.Votes.Length > 0)
                    {
                        foreach (ECPoint pubkey in state_from.Votes)
                        {
                            StorageItem storage_validator = Snapshot.Storages.GetAndChange(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Validator, pubkey.ToArray()));
                            NeoToken_ValidatorState state_validator = NeoToken_ValidatorState.FromByteArray(storage_validator.Value);
                            state_validator.Votes -= amount;
                            storage_validator.Value = state_validator.ToByteArray();
                        }
                        StorageItem storage_count = Snapshot.Storages.GetAndChange(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_ValidatorsCount));
                        NeoToken_ValidatorsCountState state_count = NeoToken_ValidatorsCountState.FromByteArray(storage_count.Value);
                        state_count.Votes[state_from.Votes.Length - 1] -= amount;
                        storage_count.Value = state_count.ToByteArray();
                    }
                    StorageKey key_to = CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Account, to);
                    StorageItem storage_to = Snapshot.Storages.GetAndChange(key_to, () => new StorageItem
                    {
                        Value = new NeoToken_AccountState().ToByteArray()
                    });
                    NeoToken_AccountState state_to = NeoToken_AccountState.FromByteArray(storage_to.Value);
                    NeoToken_DistributeGas(engine, to, state_to);
                    state_to.Balance += amount;
                    storage_to.Value = state_to.ToByteArray();
                    if (state_to.Votes.Length > 0)
                    {
                        foreach (ECPoint pubkey in state_to.Votes)
                        {
                            StorageItem storage_validator = Snapshot.Storages.GetAndChange(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Validator, pubkey.ToArray()));
                            NeoToken_ValidatorState state_validator = NeoToken_ValidatorState.FromByteArray(storage_validator.Value);
                            state_validator.Votes += amount;
                            storage_validator.Value = state_validator.ToByteArray();
                        }
                        StorageItem storage_count = Snapshot.Storages.GetAndChange(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_ValidatorsCount));
                        NeoToken_ValidatorsCountState state_count = NeoToken_ValidatorsCountState.FromByteArray(storage_count.Value);
                        state_count.Votes[state_to.Votes.Length - 1] += amount;
                        storage_count.Value = state_count.ToByteArray();
                    }
                }
            }
            SendNotification(engine, Blockchain.NeoToken.ScriptHash, new StackItem[] { "Transfer", from, to, amount });
            return true;
        }

        private void NeoToken_DistributeGas(ExecutionEngine engine, byte[] account, NeoToken_AccountState state)
        {
            BigInteger gas = NeoToken_CalculateBonus(state.Balance, state.BalanceHeight, Snapshot.PersistingBlock.Index);
            state.BalanceHeight = Snapshot.PersistingBlock.Index;
            GasToken_DistributeGas(engine, account, gas);
        }

        private BigInteger NeoToken_CalculateBonus(BigInteger value, uint start, uint end)
        {
            if (value.IsZero || start >= end) return BigInteger.Zero;
            if (value.Sign < 0) throw new ArgumentOutOfRangeException(nameof(value));
            uint amount = 0;
            uint ustart = start / Blockchain.DecrementInterval;
            if (ustart < Blockchain.GenerationAmount.Length)
            {
                uint istart = start % Blockchain.DecrementInterval;
                uint uend = end / Blockchain.DecrementInterval;
                uint iend = end % Blockchain.DecrementInterval;
                if (uend >= Blockchain.GenerationAmount.Length)
                {
                    uend = (uint)Blockchain.GenerationAmount.Length;
                    iend = 0;
                }
                if (iend == 0)
                {
                    uend--;
                    iend = Blockchain.DecrementInterval;
                }
                while (ustart < uend)
                {
                    amount += (Blockchain.DecrementInterval - istart) * Blockchain.GenerationAmount[ustart];
                    ustart++;
                    istart = 0;
                }
                amount += (iend - istart) * Blockchain.GenerationAmount[ustart];
            }
            amount += (uint)(Snapshot.GetSysFeeAmount(end - 1) - (start == 0 ? 0 : Snapshot.GetSysFeeAmount(start - 1)));
            return value * amount * GasToken_DecimalsFactor / NeoToken_TotalAmount;
        }

        private bool NeoToken_Initialize(ExecutionEngine engine)
        {
            if (Trigger != TriggerType.Application) throw new InvalidOperationException();
            StorageKey key = CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Initialized);
            if (Snapshot.Storages.TryGet(key) != null) return false;
            Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new byte[] { 1 },
                IsConstant = true
            });
            byte[] account = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1, Blockchain.StandbyValidators).ToScriptHash().ToArray();
            key = CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Account, account);
            Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new NeoToken_AccountState { Balance = NeoToken_TotalAmount }.ToByteArray()
            });
            SendNotification(engine, Blockchain.NeoToken.ScriptHash, new StackItem[] { "Transfer", StackItem.Null, account, NeoToken_TotalAmount });
            foreach (ECPoint pubkey in Blockchain.StandbyValidators)
                NeoToken_RegisterValidator(pubkey.EncodePoint(true));
            return true;
        }

        private BigInteger NeoToken_UnclaimedGas(byte[] account, uint end)
        {
            if (account.Length != 20) throw new ArgumentException();
            StorageItem storage = Snapshot.Storages.TryGet(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Account, account));
            if (storage is null) return BigInteger.Zero;
            NeoToken_AccountState state = NeoToken_AccountState.FromByteArray(storage.Value);
            return NeoToken_CalculateBonus(state.Balance, state.BalanceHeight, end);
        }

        private bool NeoToken_RegisterValidator(byte[] pubkey)
        {
            if (pubkey.Length != 33 || (pubkey[0] != 0x02 && pubkey[0] != 0x03))
                throw new ArgumentException();
            StorageKey key = CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Validator, pubkey);
            if (Snapshot.Storages.TryGet(key) != null) return false;
            Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new NeoToken_ValidatorState().ToByteArray()
            });
            return true;
        }

        private bool NeoToken_Vote(ExecutionEngine engine, byte[] account, ECPoint[] pubkeys)
        {
            UInt160 hash_account = new UInt160(account);
            if (!CheckWitness(engine, hash_account)) return false;
            StorageKey key_account = CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Account, account);
            if (Snapshot.Storages.TryGet(key_account) is null) return false;
            StorageItem storage_account = Snapshot.Storages.GetAndChange(key_account);
            NeoToken_AccountState state_account = NeoToken_AccountState.FromByteArray(storage_account.Value);
            foreach (ECPoint pubkey in state_account.Votes)
            {
                StorageItem storage_validator = Snapshot.Storages.GetAndChange(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Validator, pubkey.ToArray()));
                NeoToken_ValidatorState state_validator = NeoToken_ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes -= state_account.Balance;
                storage_validator.Value = state_validator.ToByteArray();
            }
            pubkeys = pubkeys.Distinct().Where(p => Snapshot.Storages.TryGet(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Validator, p.ToArray())) != null).ToArray();
            if (pubkeys.Length != state_account.Votes.Length)
            {
                StorageItem storage_count = Snapshot.Storages.GetAndChange(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_ValidatorsCount), () => new StorageItem
                {
                    Value = new NeoToken_ValidatorsCountState().ToByteArray()
                });
                NeoToken_ValidatorsCountState state_count = NeoToken_ValidatorsCountState.FromByteArray(storage_count.Value);
                if (state_account.Votes.Length > 0)
                    state_count.Votes[state_account.Votes.Length - 1] -= state_account.Balance;
                if (pubkeys.Length > 0)
                    state_count.Votes[pubkeys.Length - 1] += state_account.Balance;
                storage_count.Value = state_count.ToByteArray();
            }
            state_account.Votes = pubkeys;
            storage_account.Value = state_account.ToByteArray();
            foreach (ECPoint pubkey in state_account.Votes)
            {
                StorageItem storage_validator = Snapshot.Storages.GetAndChange(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_Validator, pubkey.ToArray()));
                NeoToken_ValidatorState state_validator = NeoToken_ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes += state_account.Balance;
                storage_validator.Value = state_validator.ToByteArray();
            }
            return true;
        }

        private ECPoint[] NeoToken_GetValidators()
        {
            StorageItem storage_count = Snapshot.Storages.TryGet(CreateStorageKey(Blockchain.NeoToken.ScriptHash, NeoToken_Prefix_ValidatorsCount));
            if (storage_count is null) return Blockchain.StandbyValidators;
            NeoToken_ValidatorsCountState state_count = NeoToken_ValidatorsCountState.FromByteArray(storage_count.Value);
            int count = (int)state_count.Votes.Select((p, i) => new
            {
                Count = i,
                Votes = p
            }).Where(p => p.Votes.Sign > 0).ToArray().WeightedFilter(0.25, 0.75, p => p.Votes, (p, w) => new
            {
                p.Count,
                Weight = w
            }).WeightedAverage(p => p.Count, p => p.Weight);
            count = Math.Max(count, Blockchain.StandbyValidators.Length);
            HashSet<ECPoint> sv = new HashSet<ECPoint>(Blockchain.StandbyValidators);
            return Snapshot.Storages.Find(new[] { NeoToken_Prefix_Validator }).Select(p => new
            {
                PublicKey = p.Key.Key.Skip(1).ToArray().AsSerializable<ECPoint>(),
                NeoToken_ValidatorState.FromByteArray(p.Value.Value).Votes
            }).Where(p => (p.Votes.Sign > 0) || sv.Contains(p.PublicKey)).OrderByDescending(p => p.Votes).ThenBy(p => p.PublicKey).Select(p => p.PublicKey).Take(count).OrderBy(p => p).ToArray();
        }

        private class NeoToken_AccountState
        {
            public BigInteger Balance;
            public uint BalanceHeight;
            public ECPoint[] Votes = new ECPoint[0];

            public static NeoToken_AccountState FromByteArray(byte[] data)
            {
                Struct @struct = (Struct)data.DeserializeStackItem(3);
                return new NeoToken_AccountState
                {
                    Balance = @struct[0].GetBigInteger(),
                    BalanceHeight = (uint)@struct[1].GetBigInteger(),
                    Votes = @struct[2].GetByteArray().AsSerializableArray<ECPoint>(Blockchain.MaxValidators)
                };
            }

            public byte[] ToByteArray()
            {
                return new Struct(new StackItem[]
                {
                    Balance,
                    BalanceHeight,
                    Votes.ToByteArray()
                }).Serialize();
            }
        }

        private class NeoToken_ValidatorState
        {
            public BigInteger Votes;

            public static NeoToken_ValidatorState FromByteArray(byte[] data)
            {
                return new NeoToken_ValidatorState
                {
                    Votes = new BigInteger(data)
                };
            }

            public byte[] ToByteArray()
            {
                return Votes.ToByteArray();
            }
        }

        private class NeoToken_ValidatorsCountState
        {
            public BigInteger[] Votes = new BigInteger[Blockchain.MaxValidators];

            public static NeoToken_ValidatorsCountState FromByteArray(byte[] data)
            {
                using (MemoryStream ms = new MemoryStream(data, false))
                using (BinaryReader r = new BinaryReader(ms))
                {
                    BigInteger[] votes = new BigInteger[(int)r.ReadVarInt(Blockchain.MaxValidators)];
                    for (int i = 0; i < votes.Length; i++)
                        votes[i] = new BigInteger(r.ReadVarBytes());
                    return new NeoToken_ValidatorsCountState
                    {
                        Votes = votes
                    };
                }
            }

            public byte[] ToByteArray()
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter w = new BinaryWriter(ms))
                {
                    w.WriteVarInt(Votes.Length);
                    foreach (BigInteger vote in Votes)
                        w.WriteVarBytes(vote.ToByteArray());
                    w.Flush();
                    return ms.ToArray();
                }
            }
        }
    }
}
