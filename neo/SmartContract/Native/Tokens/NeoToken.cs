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

namespace Neo.SmartContract.Native.Tokens
{
    public class NeoToken : NativeContractBase
    {
        public const string ServiceName = "Neo.Native.Tokens.NEO";
        public static readonly byte[] Script = CreateNativeScript(ServiceName);
        public static readonly UInt160 ScriptHash = Script.ToScriptHash();
        public static readonly string[] SupportedStandards = { "NEP-5", "NEP-10" };
        public const string Name = "NEO";
        public const string Symbol = "neo";
        public const int Decimals = 0;
        public static readonly BigInteger DecimalsFactor = BigInteger.Pow(10, Decimals);
        public static readonly BigInteger TotalAmount = 100000000 * DecimalsFactor;

        private const byte Prefix_Initialized = 11;
        private const byte Prefix_Account = 20;
        private const byte Prefix_Validator = 33;
        private const byte Prefix_ValidatorsCount = 15;

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
                    result = TotalAmount;
                    break;
                case "balanceOf":
                    result = BalanceOf(engine, args[0].GetByteArray());
                    break;
                case "transfer":
                    result = Transfer(engine, args[0].GetByteArray(), args[1].GetByteArray(), args[2].GetBigInteger());
                    break;
                case "initialize":
                    result = Initialize(engine);
                    break;
                case "unclaimedGas":
                    result = UnclaimedGas(engine, args[0].GetByteArray(), (uint)args[1].GetBigInteger());
                    break;
                case "registerValidator":
                    result = RegisterValidator(engine, args[0].GetByteArray());
                    break;
                case "vote":
                    result = Vote(engine, args[0].GetByteArray(), ((VMArray)args[1]).Select(p => p.GetByteArray().AsSerializable<ECPoint>()).ToArray());
                    break;
                case "getValidators":
                    result = GetValidators(engine).Select(p => (StackItem)p.ToArray()).ToArray();
                    break;
                default:
                    return false;
            }
            engine.CurrentContext.EvaluationStack.Push(result);
            return true;
        }

        private static StorageKey CreateStorageKey(UInt160 script_hash, byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                ScriptHash = script_hash,
                Key = new byte[sizeof(byte) + key?.Length ?? 0]
            };
            storageKey.Key[0] = prefix;
            if (key != null)
                Buffer.BlockCopy(key, 0, storageKey.Key, 1, key.Length);
            return storageKey;
        }

        private static BigInteger BalanceOf(ApplicationEngine engine, byte[] account)
        {
            if (account.Length != 20) throw new ArgumentException();
            StorageItem storage = engine.Service.Snapshot.Storages.TryGet(CreateStorageKey(ScriptHash, Prefix_Account, account));
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
            StorageKey key_from = CreateStorageKey(ScriptHash, Prefix_Account, from);
            StorageItem storage_from = engine.Service.Snapshot.Storages.TryGet(key_from);
            if (amount.IsZero)
            {
                if (storage_from != null)
                {
                    AccountState state_from = AccountState.FromByteArray(storage_from.Value);
                    DistributeGas(engine, from, state_from);
                    storage_from = engine.Service.Snapshot.Storages.GetAndChange(key_from);
                    storage_from.Value = state_from.ToByteArray();
                }
            }
            else
            {
                if (storage_from is null) return false;
                AccountState state_from = AccountState.FromByteArray(storage_from.Value);
                if (state_from.Balance < amount) return false;
                DistributeGas(engine, from, state_from);
                if (hash_from.Equals(hash_to))
                {
                    storage_from = engine.Service.Snapshot.Storages.GetAndChange(key_from);
                    storage_from.Value = state_from.ToByteArray();
                }
                else
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
                    if (state_from.Votes.Length > 0)
                    {
                        foreach (ECPoint pubkey in state_from.Votes)
                        {
                            StorageItem storage_validator = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(ScriptHash, Prefix_Validator, pubkey.ToArray()));
                            ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                            state_validator.Votes -= amount;
                            storage_validator.Value = state_validator.ToByteArray();
                        }
                        StorageItem storage_count = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(ScriptHash, Prefix_ValidatorsCount));
                        ValidatorsCountState state_count = ValidatorsCountState.FromByteArray(storage_count.Value);
                        state_count.Votes[state_from.Votes.Length - 1] -= amount;
                        storage_count.Value = state_count.ToByteArray();
                    }
                    StorageKey key_to = CreateStorageKey(ScriptHash, Prefix_Account, to);
                    StorageItem storage_to = engine.Service.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem
                    {
                        Value = new AccountState().ToByteArray()
                    });
                    AccountState state_to = AccountState.FromByteArray(storage_to.Value);
                    DistributeGas(engine, to, state_to);
                    state_to.Balance += amount;
                    storage_to.Value = state_to.ToByteArray();
                    if (state_to.Votes.Length > 0)
                    {
                        foreach (ECPoint pubkey in state_to.Votes)
                        {
                            StorageItem storage_validator = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(ScriptHash, Prefix_Validator, pubkey.ToArray()));
                            ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                            state_validator.Votes += amount;
                            storage_validator.Value = state_validator.ToByteArray();
                        }
                        StorageItem storage_count = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(ScriptHash, Prefix_ValidatorsCount));
                        ValidatorsCountState state_count = ValidatorsCountState.FromByteArray(storage_count.Value);
                        state_count.Votes[state_to.Votes.Length - 1] += amount;
                        storage_count.Value = state_count.ToByteArray();
                    }
                }
            }
            engine.Service.SendNotification(engine, ScriptHash, new StackItem[] { "Transfer", from, to, amount });
            return true;
        }

        private static void DistributeGas(ApplicationEngine engine, byte[] account, AccountState state)
        {
            BigInteger gas = CalculateBonus(engine, state.Balance, state.BalanceHeight, engine.Service.Snapshot.PersistingBlock.Index);
            state.BalanceHeight = engine.Service.Snapshot.PersistingBlock.Index;
            GasToken.DistributeGas(engine, account, gas);
        }

        private static BigInteger CalculateBonus(ApplicationEngine engine, BigInteger value, uint start, uint end)
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
            amount += (uint)(engine.Service.Snapshot.GetSysFeeAmount(end - 1) - (start == 0 ? 0 : engine.Service.Snapshot.GetSysFeeAmount(start - 1)));
            return value * amount * GasToken.DecimalsFactor / TotalAmount;
        }

        private static bool Initialize(ApplicationEngine engine)
        {
            if (engine.Service.Trigger != TriggerType.Application) throw new InvalidOperationException();
            StorageKey key = CreateStorageKey(ScriptHash, Prefix_Initialized);
            if (engine.Service.Snapshot.Storages.TryGet(key) != null) return false;
            engine.Service.Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new byte[] { 1 },
                IsConstant = true
            });
            byte[] account = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1, Blockchain.StandbyValidators).ToScriptHash().ToArray();
            key = CreateStorageKey(ScriptHash, Prefix_Account, account);
            engine.Service.Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new AccountState { Balance = TotalAmount }.ToByteArray()
            });
            engine.Service.SendNotification(engine, ScriptHash, new StackItem[] { "Transfer", StackItem.Null, account, TotalAmount });
            foreach (ECPoint pubkey in Blockchain.StandbyValidators)
                RegisterValidator(engine, pubkey.EncodePoint(true));
            return true;
        }

        private static BigInteger UnclaimedGas(ApplicationEngine engine, byte[] account, uint end)
        {
            if (account.Length != 20) throw new ArgumentException();
            StorageItem storage = engine.Service.Snapshot.Storages.TryGet(CreateStorageKey(ScriptHash, Prefix_Account, account));
            if (storage is null) return BigInteger.Zero;
            AccountState state = AccountState.FromByteArray(storage.Value);
            return CalculateBonus(engine, state.Balance, state.BalanceHeight, end);
        }

        private static bool RegisterValidator(ApplicationEngine engine, byte[] pubkey)
        {
            if (pubkey.Length != 33 || (pubkey[0] != 0x02 && pubkey[0] != 0x03))
                throw new ArgumentException();
            StorageKey key = CreateStorageKey(ScriptHash, Prefix_Validator, pubkey);
            if (engine.Service.Snapshot.Storages.TryGet(key) != null) return false;
            engine.Service.Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new ValidatorState().ToByteArray()
            });
            return true;
        }

        private static bool Vote(ApplicationEngine engine, byte[] account, ECPoint[] pubkeys)
        {
            UInt160 hash_account = new UInt160(account);
            if (!engine.Service.CheckWitness(engine, hash_account)) return false;
            StorageKey key_account = CreateStorageKey(ScriptHash, Prefix_Account, account);
            if (engine.Service.Snapshot.Storages.TryGet(key_account) is null) return false;
            StorageItem storage_account = engine.Service.Snapshot.Storages.GetAndChange(key_account);
            AccountState state_account = AccountState.FromByteArray(storage_account.Value);
            foreach (ECPoint pubkey in state_account.Votes)
            {
                StorageItem storage_validator = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(ScriptHash, Prefix_Validator, pubkey.ToArray()));
                ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes -= state_account.Balance;
                storage_validator.Value = state_validator.ToByteArray();
            }
            pubkeys = pubkeys.Distinct().Where(p => engine.Service.Snapshot.Storages.TryGet(CreateStorageKey(ScriptHash, Prefix_Validator, p.ToArray())) != null).ToArray();
            if (pubkeys.Length != state_account.Votes.Length)
            {
                StorageItem storage_count = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(ScriptHash, Prefix_ValidatorsCount), () => new StorageItem
                {
                    Value = new ValidatorsCountState().ToByteArray()
                });
                ValidatorsCountState state_count = ValidatorsCountState.FromByteArray(storage_count.Value);
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
                StorageItem storage_validator = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(ScriptHash, Prefix_Validator, pubkey.ToArray()));
                ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes += state_account.Balance;
                storage_validator.Value = state_validator.ToByteArray();
            }
            return true;
        }

        private static ECPoint[] GetValidators(ApplicationEngine engine)
        {
            StorageItem storage_count = engine.Service.Snapshot.Storages.TryGet(CreateStorageKey(ScriptHash, Prefix_ValidatorsCount));
            if (storage_count is null) return Blockchain.StandbyValidators;
            ValidatorsCountState state_count = ValidatorsCountState.FromByteArray(storage_count.Value);
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
            return engine.Service.Snapshot.Storages.Find(new[] { Prefix_Validator }).Select(p => new
            {
                PublicKey = p.Key.Key.Skip(1).ToArray().AsSerializable<ECPoint>(),
                ValidatorState.FromByteArray(p.Value.Value).Votes
            }).Where(p => (p.Votes.Sign > 0) || sv.Contains(p.PublicKey)).OrderByDescending(p => p.Votes).ThenBy(p => p.PublicKey).Select(p => p.PublicKey).Take(count).OrderBy(p => p).ToArray();
        }

        private class AccountState
        {
            public BigInteger Balance;
            public uint BalanceHeight;
            public ECPoint[] Votes = new ECPoint[0];

            public static AccountState FromByteArray(byte[] data)
            {
                Struct @struct = (Struct)data.DeserializeStackItem(3);
                return new AccountState
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

        private class ValidatorState
        {
            public BigInteger Votes;

            public static ValidatorState FromByteArray(byte[] data)
            {
                return new ValidatorState
                {
                    Votes = new BigInteger(data)
                };
            }

            public byte[] ToByteArray()
            {
                return Votes.ToByteArray();
            }
        }

        private class ValidatorsCountState
        {
            public BigInteger[] Votes = new BigInteger[Blockchain.MaxValidators];

            public static ValidatorsCountState FromByteArray(byte[] data)
            {
                using (MemoryStream ms = new MemoryStream(data, false))
                using (BinaryReader r = new BinaryReader(ms))
                {
                    BigInteger[] votes = new BigInteger[(int)r.ReadVarInt(Blockchain.MaxValidators)];
                    for (int i = 0; i < votes.Length; i++)
                        votes[i] = new BigInteger(r.ReadVarBytes());
                    return new ValidatorsCountState
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
