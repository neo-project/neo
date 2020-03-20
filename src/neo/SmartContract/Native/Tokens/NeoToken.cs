#pragma warning disable IDE0051
#pragma warning disable IDE0060

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public sealed class NeoToken : Nep5Token<NeoToken.AccountState>
    {
        public override string ServiceName => "Neo.Native.Tokens.NEO";
        public override int Id => -1;
        public override string Name => "NEO";
        public override string Symbol => "neo";
        public override byte Decimals => 0;
        public BigInteger TotalAmount { get; }

        private const byte Prefix_Candidate = 33;
        private const byte Prefix_NextValidators = 14;

        internal NeoToken()
        {
            this.TotalAmount = 100000000 * Factor;
        }

        public override BigInteger TotalSupply(StoreView snapshot)
        {
            return TotalAmount;
        }

        protected override void OnBalanceChanging(ApplicationEngine engine, UInt160 account, AccountState state, BigInteger amount)
        {
            DistributeGas(engine, account, state);
            if (amount.IsZero) return;
            if (state.VoteTo != null)
            {
                StorageItem storage_validator = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Candidate, state.VoteTo.ToArray()));
                ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes += amount;
                storage_validator.Value = state_validator.ToByteArray();
            }
        }

        private void DistributeGas(ApplicationEngine engine, UInt160 account, AccountState state)
        {
            BigInteger gas = CalculateBonus(engine.Snapshot, state.Balance, state.BalanceHeight, engine.Snapshot.PersistingBlock.Index);
            state.BalanceHeight = engine.Snapshot.PersistingBlock.Index;
            GAS.Mint(engine, account, gas);
            engine.Snapshot.Storages.GetAndChange(CreateAccountKey(account)).Value = state.ToByteArray();
        }

        private BigInteger CalculateBonus(StoreView snapshot, BigInteger value, uint start, uint end)
        {
            if (value.IsZero || start >= end) return BigInteger.Zero;
            if (value.Sign < 0) throw new ArgumentOutOfRangeException(nameof(value));
            BigInteger amount = BigInteger.Zero;
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
            return value * amount * GAS.Factor / TotalAmount;
        }

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            if (base.TotalSupply(engine.Snapshot) != BigInteger.Zero) return false;
            BigInteger amount = TotalAmount;
            for (int i = 0; i < Blockchain.CommitteeMembersCount; i++)
            {
                ECPoint pubkey = Blockchain.StandbyCommittee[i];
                RegisterCandidate(engine.Snapshot, pubkey);
                BigInteger balance = TotalAmount / 2 / (Blockchain.ValidatorsCount * 2 + (Blockchain.CommitteeMembersCount - Blockchain.ValidatorsCount));
                if (i < Blockchain.ValidatorsCount) balance *= 2;
                UInt160 account = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
                Mint(engine, account, balance);
                Vote(engine.Snapshot, account, pubkey);
                amount -= balance;
            }
            Mint(engine, Blockchain.GetConsensusAddress(Blockchain.StandbyValidators), amount);
            return true;
        }

        protected override bool OnPersist(ApplicationEngine engine)
        {
            if (!base.OnPersist(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_NextValidators), () => new StorageItem());
            storage.Value = GetValidators(engine.Snapshot).ToByteArray();
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.Integer, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Integer }, ParameterNames = new[] { "account", "end" }, SafeMethod = true)]
        private StackItem UnclaimedGas(ApplicationEngine engine, Array args)
        {
            UInt160 account = new UInt160(args[0].GetSpan());
            uint end = (uint)args[1].GetBigInteger();
            return UnclaimedGas(engine.Snapshot, account, end);
        }

        public BigInteger UnclaimedGas(StoreView snapshot, UInt160 account, uint end)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateAccountKey(account));
            if (storage is null) return BigInteger.Zero;
            AccountState state = new AccountState(storage.Value);
            return CalculateBonus(snapshot, state.Balance, state.BalanceHeight, end);
        }

        [ContractMethod(0_05000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.PublicKey }, ParameterNames = new[] { "pubkey" })]
        private StackItem RegisterCandidate(ApplicationEngine engine, Array args)
        {
            ECPoint pubkey = args[0].GetSpan().AsSerializable<ECPoint>();
            if (!InteropService.Runtime.CheckWitnessInternal(engine, Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash()))
                return false;
            return RegisterCandidate(engine.Snapshot, pubkey);
        }

        private bool RegisterCandidate(StoreView snapshot, ECPoint pubkey)
        {
            StorageKey key = CreateStorageKey(Prefix_Candidate, pubkey);
            if (snapshot.Storages.TryGet(key) != null) return false;
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = new ValidatorState().ToByteArray()
            });
            return true;
        }

        [ContractMethod(5_00000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Array }, ParameterNames = new[] { "account", "pubkeys" })]
        private StackItem Vote(ApplicationEngine engine, Array args)
        {
            UInt160 account = new UInt160(args[0].GetSpan());
            ECPoint voteTo = args[1].IsNull ? null : args[1].GetSpan().AsSerializable<ECPoint>();
            if (!InteropService.Runtime.CheckWitnessInternal(engine, account)) return false;
            return Vote(engine.Snapshot, account, voteTo);
        }

        private bool Vote(StoreView snapshot, UInt160 account, ECPoint voteTo)
        {
            StorageKey key_account = CreateAccountKey(account);
            if (snapshot.Storages.TryGet(key_account) is null) return false;
            StorageItem storage_account = snapshot.Storages.GetAndChange(key_account);
            AccountState state_account = new AccountState(storage_account.Value);
            if (state_account.VoteTo != null)
            {
                StorageItem storage_validator = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Candidate, state_account.VoteTo.ToArray()));
                ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes -= state_account.Balance;
                storage_validator.Value = state_validator.ToByteArray();
            }
            state_account.VoteTo = voteTo;
            storage_account.Value = state_account.ToByteArray();
            if (voteTo != null)
            {
                StorageKey key = CreateStorageKey(Prefix_Candidate, voteTo.ToArray());
                if (snapshot.Storages.TryGet(key) is null) return false;
                StorageItem storage_validator = snapshot.Storages.GetAndChange(key);
                ValidatorState state_validator = ValidatorState.FromByteArray(storage_validator.Value);
                state_validator.Votes += state_account.Balance;
                storage_validator.Value = state_validator.ToByteArray();
            }
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Array, SafeMethod = true)]
        private StackItem GetCandidates(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetCandidates(engine.Snapshot).Select(p => new Struct(engine.ReferenceCounter, new StackItem[] { p.PublicKey.ToArray(), p.Votes })));
        }

        public IEnumerable<(ECPoint PublicKey, BigInteger Votes)> GetCandidates(StoreView snapshot)
        {
            byte[] prefix_key = StorageKey.CreateSearchPrefix(Id, new[] { Prefix_Candidate });
            return snapshot.Storages.Find(prefix_key).Select(p =>
            (
                p.Key.Key.AsSerializable<ECPoint>(1),
                ValidatorState.FromByteArray(p.Value.Value).Votes
            ));
        }

        [ContractMethod(1_00000000, ContractParameterType.Array, SafeMethod = true)]
        private StackItem GetValidators(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetValidators(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public ECPoint[] GetValidators(StoreView snapshot)
        {
            return GetCommitteeMembers(snapshot, Blockchain.ValidatorsCount).OrderBy(p => p).ToArray();
        }

        [ContractMethod(1_00000000, ContractParameterType.Array, SafeMethod = true)]
        private StackItem GetCommittee(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetCommittee(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public ECPoint[] GetCommittee(StoreView snapshot)
        {
            return GetCommitteeMembers(snapshot, Blockchain.CommitteeMembersCount).OrderBy(p => p).ToArray();
        }

        private IEnumerable<ECPoint> GetCommitteeMembers(StoreView snapshot, int count)
        {
            return GetCandidates(snapshot).OrderByDescending(p => p.Votes).ThenBy(p => p.PublicKey).Select(p => p.PublicKey).Take(count);
        }

        [ContractMethod(1_00000000, ContractParameterType.Array, SafeMethod = true)]
        private StackItem GetNextBlockValidators(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetNextBlockValidators(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public ECPoint[] GetNextBlockValidators(StoreView snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_NextValidators));
            if (storage is null) return Blockchain.StandbyValidators;
            return storage.Value.AsSerializableArray<ECPoint>();
        }

        public class AccountState : Nep5AccountState
        {
            public uint BalanceHeight;
            public ECPoint VoteTo;

            public AccountState()
            {
            }

            public AccountState(byte[] data)
                : base(data)
            {
            }

            protected override void FromStruct(Struct @struct)
            {
                base.FromStruct(@struct);
                BalanceHeight = (uint)@struct[1].GetBigInteger();
                VoteTo = @struct[2].IsNull ? null : @struct[2].GetSpan().AsSerializable<ECPoint>();
            }

            protected override Struct ToStruct()
            {
                Struct @struct = base.ToStruct();
                @struct.Add(BalanceHeight);
                @struct.Add(VoteTo?.ToArray() ?? StackItem.Null);
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
                return Votes.ToByteArrayStandard();
            }
        }
    }
}
