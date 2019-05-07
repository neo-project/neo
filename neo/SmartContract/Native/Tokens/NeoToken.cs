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
    public sealed class NeoToken : Nep5Token<NeoToken.AccountState>
    {
        public override string ServiceName => "Neo.Native.Tokens.NEO";
        public override string Name => "NEO";
        public override string Symbol => "neo";
        public override int Decimals => 0;
        public BigInteger TotalAmount { get; }

        private const byte Prefix_Initialized = 11;
        private const byte Prefix_Validator = 33;
        private const byte Prefix_ValidatorsCount = 15;

        private NeoToken()
        {
            this.TotalAmount = 100000000 * Factor;
        }

        protected override StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            switch (operation)
            {
                case "initialize":
                    return Initialize(engine, new UInt160(args[0].GetByteArray()));
                case "unclaimedGas":
                    return UnclaimedGas(engine, new UInt160(args[0].GetByteArray()), (uint)args[1].GetBigInteger());
                case "registerValidator":
                    return RegisterValidator(engine, args[0].GetByteArray());
                case "vote":
                    return Vote(engine, new UInt160(args[0].GetByteArray()), ((VMArray)args[1]).Select(p => p.GetByteArray().AsSerializable<ECPoint>()).ToArray());
                case "getValidators":
                    return GetValidators(engine).Select(p => (StackItem)p.ToArray()).ToArray();
                default:
                    return base.Main(engine, operation, args);
            }
        }

        protected override BigInteger TotalSupply(ApplicationEngine engine)
        {
            return TotalAmount;
        }

        protected override void OnBalanceChanging(ApplicationEngine engine, UInt160 account, AccountState state, BigInteger amount)
        {
            DistributeGas(engine, account, state);
            if (amount.IsZero) return;
            if (state.Votes.Length == 0) return;
            foreach (ECPoint pubkey in state.Votes)
            {
                StorageItem storage_validator = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Validator, pubkey.ToArray()));
                ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes += amount;
                storage_validator.Value = state_validator.ToByteArray();
            }
            StorageItem storage_count = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_ValidatorsCount));
            ValidatorsCountState state_count = ValidatorsCountState.FromByteArray(storage_count.Value);
            state_count.Votes[state.Votes.Length - 1] += amount;
            storage_count.Value = state_count.ToByteArray();
        }

        private void DistributeGas(ApplicationEngine engine, UInt160 account, AccountState state)
        {
            BigInteger gas = CalculateBonus(engine, state.Balance, state.BalanceHeight, engine.Service.Snapshot.PersistingBlock.Index);
            state.BalanceHeight = engine.Service.Snapshot.PersistingBlock.Index;
            GAS.MintTokens(engine, account, gas);
            engine.Service.Snapshot.Storages.GetAndChange(CreateAccountKey(account)).Value = state.ToByteArray();
        }

        private BigInteger CalculateBonus(ApplicationEngine engine, BigInteger value, uint start, uint end)
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
            return value * amount * GAS.Factor / TotalAmount;
        }

        private bool Initialize(ApplicationEngine engine, UInt160 account)
        {
            if (engine.Service.Trigger != TriggerType.Application) throw new InvalidOperationException();
            StorageKey key = CreateStorageKey(Prefix_Initialized);
            if (engine.Service.Snapshot.Storages.TryGet(key) != null) return false;
            engine.Service.Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new byte[] { 1 },
                IsConstant = true
            });
            MintTokens(engine, account, TotalAmount);
            foreach (ECPoint pubkey in Blockchain.StandbyValidators)
                RegisterValidator(engine, pubkey.EncodePoint(true));
            return true;
        }

        private BigInteger UnclaimedGas(ApplicationEngine engine, UInt160 account, uint end)
        {
            StorageItem storage = engine.Service.Snapshot.Storages.TryGet(CreateAccountKey(account));
            if (storage is null) return BigInteger.Zero;
            AccountState state = new AccountState(storage.Value);
            return CalculateBonus(engine, state.Balance, state.BalanceHeight, end);
        }

        private bool RegisterValidator(ApplicationEngine engine, byte[] pubkey)
        {
            if (pubkey.Length != 33 || (pubkey[0] != 0x02 && pubkey[0] != 0x03))
                throw new ArgumentException();
            StorageKey key = CreateStorageKey(Prefix_Validator, pubkey);
            if (engine.Service.Snapshot.Storages.TryGet(key) != null) return false;
            engine.Service.Snapshot.Storages.Add(key, new StorageItem
            {
                Value = new ValidatorState().ToByteArray()
            });
            return true;
        }

        private bool Vote(ApplicationEngine engine, UInt160 account, ECPoint[] pubkeys)
        {
            if (!engine.Service.CheckWitness(engine, account)) return false;
            StorageKey key_account = CreateAccountKey(account);
            if (engine.Service.Snapshot.Storages.TryGet(key_account) is null) return false;
            StorageItem storage_account = engine.Service.Snapshot.Storages.GetAndChange(key_account);
            AccountState state_account = new AccountState(storage_account.Value);
            foreach (ECPoint pubkey in state_account.Votes)
            {
                StorageItem storage_validator = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Validator, pubkey.ToArray()));
                ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes -= state_account.Balance;
                storage_validator.Value = state_validator.ToByteArray();
            }
            pubkeys = pubkeys.Distinct().Where(p => engine.Service.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_Validator, p.ToArray())) != null).ToArray();
            if (pubkeys.Length != state_account.Votes.Length)
            {
                StorageItem storage_count = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_ValidatorsCount), () => new StorageItem
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
                StorageItem storage_validator = engine.Service.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Validator, pubkey.ToArray()));
                ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes += state_account.Balance;
                storage_validator.Value = state_validator.ToByteArray();
            }
            return true;
        }

        private ECPoint[] GetValidators(ApplicationEngine engine)
        {
            StorageItem storage_count = engine.Service.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_ValidatorsCount));
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

        public class AccountState : Nep5AccountState
        {
            public uint BalanceHeight;
            public ECPoint[] Votes;

            public AccountState()
            {
                this.Votes = new ECPoint[0];
            }

            public AccountState(byte[] data)
                : base(data)
            {
            }

            protected override void FromStruct(Struct @struct)
            {
                base.FromStruct(@struct);
                BalanceHeight = (uint)@struct[1].GetBigInteger();
                Votes = @struct[2].GetByteArray().AsSerializableArray<ECPoint>(Blockchain.MaxValidators);
            }

            protected override Struct ToStruct()
            {
                Struct @struct = base.ToStruct();
                @struct.Add(BalanceHeight);
                @struct.Add(Votes.ToByteArray());
                return @struct;
            }
        }

        internal class ValidatorState
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

        internal class ValidatorsCountState
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
