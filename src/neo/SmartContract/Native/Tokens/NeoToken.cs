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
    public sealed class NeoToken : Nep5Token<NeoToken.NeoAccountState>
    {
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

        protected override void OnBalanceChanging(ApplicationEngine engine, UInt160 account, NeoAccountState state, BigInteger amount)
        {
            DistributeGas(engine, account, state);
            if (amount.IsZero) return;
            if (state.VoteTo != null)
            {
                StorageItem storage_validator = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Candidate, state.VoteTo.ToArray()));
                CandidateState state_validator = storage_validator.GetInteroperable<CandidateState>();
                state_validator.Votes += amount;
            }
        }

        private void DistributeGas(ApplicationEngine engine, UInt160 account, NeoAccountState state)
        {
            BigInteger gas = CalculateBonus(engine.Snapshot, state.Balance, state.BalanceHeight, engine.Snapshot.PersistingBlock.Index);
            state.BalanceHeight = engine.Snapshot.PersistingBlock.Index;
            GAS.Mint(engine, account, gas);
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

        internal override void Initialize(ApplicationEngine engine)
        {
            BigInteger amount = TotalAmount;
            for (int i = 0; i < Blockchain.StandbyCommittee.Length; i++)
            {
                ECPoint pubkey = Blockchain.StandbyCommittee[i];
                RegisterCandidate(engine.Snapshot, pubkey);
                BigInteger balance = TotalAmount / 2 / (Blockchain.StandbyValidators.Length * 2 + (Blockchain.StandbyCommittee.Length - Blockchain.StandbyValidators.Length));
                if (i < Blockchain.StandbyValidators.Length) balance *= 2;
                UInt160 account = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
                Mint(engine, account, balance);
                Vote(engine.Snapshot, account, pubkey);
                amount -= balance;
            }
            Mint(engine, Blockchain.GetConsensusAddress(Blockchain.StandbyValidators), amount);
        }

        protected override bool OnPersist(ApplicationEngine engine)
        {
            if (!base.OnPersist(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_NextValidators), () => new StorageItem());
            storage.Value = GetValidators(engine.Snapshot).ToByteArray();
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.Integer, CallFlags.AllowStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Integer }, ParameterNames = new[] { "account", "end" })]
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
            NeoAccountState state = storage.GetInteroperable<NeoAccountState>();
            return CalculateBonus(snapshot, state.Balance, state.BalanceHeight, end);
        }

        [ContractMethod(0_05000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.PublicKey }, ParameterNames = new[] { "pubkey" })]
        private StackItem RegisterCandidate(ApplicationEngine engine, Array args)
        {
            ECPoint pubkey = args[0].GetSpan().AsSerializable<ECPoint>();
            if (!engine.CheckWitnessInternal(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash()))
                return false;
            return RegisterCandidate(engine.Snapshot, pubkey);
        }

        private bool RegisterCandidate(StoreView snapshot, ECPoint pubkey)
        {
            StorageKey key = CreateStorageKey(Prefix_Candidate, pubkey);
            StorageItem item = snapshot.Storages.GetAndChange(key, () => new StorageItem(new CandidateState()));
            CandidateState state = item.GetInteroperable<CandidateState>();
            state.Registered = true;
            return true;
        }

        [ContractMethod(0_05000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.PublicKey }, ParameterNames = new[] { "pubkey" })]
        private StackItem UnregisterCandidate(ApplicationEngine engine, Array args)
        {
            ECPoint pubkey = args[0].GetSpan().AsSerializable<ECPoint>();
            if (!engine.CheckWitnessInternal(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash()))
                return false;
            return UnregisterCandidate(engine.Snapshot, pubkey);
        }

        private bool UnregisterCandidate(StoreView snapshot, ECPoint pubkey)
        {
            StorageKey key = CreateStorageKey(Prefix_Candidate, pubkey);
            if (snapshot.Storages.TryGet(key) is null) return true;
            StorageItem item = snapshot.Storages.GetAndChange(key);
            CandidateState state = item.GetInteroperable<CandidateState>();
            if (state.Votes.IsZero)
                snapshot.Storages.Delete(key);
            else
                state.Registered = false;
            return true;
        }

        [ContractMethod(5_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Array }, ParameterNames = new[] { "account", "pubkeys" })]
        private StackItem Vote(ApplicationEngine engine, Array args)
        {
            UInt160 account = new UInt160(args[0].GetSpan());
            ECPoint voteTo = args[1].IsNull ? null : args[1].GetSpan().AsSerializable<ECPoint>();
            if (!engine.CheckWitnessInternal(account)) return false;
            return Vote(engine.Snapshot, account, voteTo);
        }

        private bool Vote(StoreView snapshot, UInt160 account, ECPoint voteTo)
        {
            StorageKey key_account = CreateAccountKey(account);
            if (snapshot.Storages.TryGet(key_account) is null) return false;
            StorageItem storage_account = snapshot.Storages.GetAndChange(key_account);
            NeoAccountState state_account = storage_account.GetInteroperable<NeoAccountState>();
            if (state_account.VoteTo != null)
            {
                StorageKey key = CreateStorageKey(Prefix_Candidate, state_account.VoteTo.ToArray());
                StorageItem storage_validator = snapshot.Storages.GetAndChange(key);
                CandidateState state_validator = storage_validator.GetInteroperable<CandidateState>();
                state_validator.Votes -= state_account.Balance;
                if (!state_validator.Registered && state_validator.Votes.IsZero)
                    snapshot.Storages.Delete(key);
            }
            state_account.VoteTo = voteTo;
            if (voteTo != null)
            {
                StorageKey key = CreateStorageKey(Prefix_Candidate, voteTo.ToArray());
                if (snapshot.Storages.TryGet(key) is null) return false;
                StorageItem storage_validator = snapshot.Storages.GetAndChange(key);
                CandidateState state_validator = storage_validator.GetInteroperable<CandidateState>();
                if (!state_validator.Registered) return false;
                state_validator.Votes += state_account.Balance;
            }
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Array, CallFlags.AllowStates)]
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
                p.Value.GetInteroperable<CandidateState>()
            )).Where(p => p.Item2.Registered).Select(p => (p.Item1, p.Item2.Votes));
        }

        [ContractMethod(1_00000000, ContractParameterType.Array, CallFlags.AllowStates)]
        private StackItem GetValidators(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetValidators(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public ECPoint[] GetValidators(StoreView snapshot)
        {
            return GetCommitteeMembers(snapshot, ProtocolSettings.Default.MaxValidatorsCount).OrderBy(p => p).ToArray();
        }

        [ContractMethod(1_00000000, ContractParameterType.Array, CallFlags.AllowStates)]
        private StackItem GetCommittee(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetCommittee(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public ECPoint[] GetCommittee(StoreView snapshot)
        {
            return GetCommitteeMembers(snapshot, ProtocolSettings.Default.MaxCommitteeMembersCount).OrderBy(p => p).ToArray();
        }

        public UInt160 GetCommitteeAddress(StoreView snapshot)
        {
            ECPoint[] committees = GetCommittee(snapshot);
            return Contract.CreateMultiSigRedeemScript(committees.Length - (committees.Length - 1) / 2, committees).ToScriptHash();
        }

        private IEnumerable<ECPoint> GetCommitteeMembers(StoreView snapshot, int count)
        {
            return GetCandidates(snapshot).OrderByDescending(p => p.Votes).ThenBy(p => p.PublicKey).Select(p => p.PublicKey).Take(count);
        }

        [ContractMethod(1_00000000, ContractParameterType.Array, CallFlags.AllowStates)]
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

        public class NeoAccountState : AccountState
        {
            public uint BalanceHeight;
            public ECPoint VoteTo;

            public override void FromStackItem(StackItem stackItem)
            {
                base.FromStackItem(stackItem);
                Struct @struct = (Struct)stackItem;
                BalanceHeight = (uint)@struct[1].GetBigInteger();
                VoteTo = @struct[2].IsNull ? null : @struct[2].GetSpan().AsSerializable<ECPoint>();
            }

            public override StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                Struct @struct = (Struct)base.ToStackItem(referenceCounter);
                @struct.Add(BalanceHeight);
                @struct.Add(VoteTo?.ToArray() ?? StackItem.Null);
                return @struct;
            }
        }

        internal class CandidateState : IInteroperable
        {
            public bool Registered = true;
            public BigInteger Votes;

            public void FromStackItem(StackItem stackItem)
            {
                Struct @struct = (Struct)stackItem;
                Registered = @struct[0].ToBoolean();
                Votes = @struct[1].GetBigInteger();
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new Struct(referenceCounter) { Registered, Votes };
            }
        }
    }
}
