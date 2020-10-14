#pragma warning disable IDE0051

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public sealed class NeoToken : Nep5Token<NeoToken.NeoAccountState>
    {
        public override int Id => -1;
        public override string Name => "NEO";
        public override string Symbol => "neo";
        public override byte Decimals => 0;
        public BigInteger TotalAmount { get; }

        public const decimal EffectiveVoterTurnout = 0.2M;

        private const byte Prefix_VotersCount = 1;
        private const byte Prefix_Candidate = 33;
        private const byte Prefix_Committee = 14;
        private const byte Prefix_GasPerBlock = 29;

        private const byte NeoHolderRewardRatio = 10;
        private const byte CommitteeRewardRatio = 5;
        private const byte VoterRewardRatio = 85;

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
            if (state.VoteTo is null) return;
            engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_VotersCount)).Add(amount);
            StorageKey key = CreateStorageKey(Prefix_Candidate).Add(state.VoteTo);
            CandidateState candidate = engine.Snapshot.Storages.GetAndChange(key).GetInteroperable<CandidateState>();
            candidate.Votes += amount;
            CheckCandidate(engine.Snapshot, key, candidate);
        }

        private void DistributeGas(ApplicationEngine engine, UInt160 account, NeoAccountState state)
        {
            // PersistingBlock is null when running under the debugger
            if (engine.Snapshot.PersistingBlock == null) return;

            BigInteger gas = CalculateBonus(engine.Snapshot, state.Balance, state.BalanceHeight, engine.Snapshot.PersistingBlock.Index);
            state.BalanceHeight = engine.Snapshot.PersistingBlock.Index;
            GAS.Mint(engine, account, gas);
        }

        private BigInteger CalculateBonus(StoreView snapshot, BigInteger value, uint start, uint end)
        {
            if (value.IsZero || start >= end) return BigInteger.Zero;
            if (value.Sign < 0) throw new ArgumentOutOfRangeException(nameof(value));

            BigInteger sum = 0;
            foreach (var gasRecord in GetSortedGasRecords(snapshot, end))
            {
                if (gasRecord.index > start)
                {
                    sum += gasRecord.gasPerBlock * (end - gasRecord.index);
                    end = gasRecord.index;
                }
                else
                {
                    sum += gasRecord.gasPerBlock * (end - start);
                    break;
                }
            }
            return value * sum * NeoHolderRewardRatio / 100 / TotalAmount;
        }

        private static void CheckCandidate(StoreView snapshot, StorageKey key, CandidateState candidate)
        {
            if (!candidate.Registered && candidate.Votes.IsZero)
                snapshot.Storages.Delete(key);
        }

        private bool ShouldRefreshCommittee(uint height) => height % (ProtocolSettings.Default.CommitteeMembersCount + ProtocolSettings.Default.ValidatorsCount) == 0;

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Committee), new StorageItem(Blockchain.StandbyCommittee.ToByteArray()));
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_VotersCount), new StorageItem(new byte[0]));

            // Initialize economic parameters

            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_GasPerBlock).AddBigEndian(0u), new StorageItem(5 * GAS.Factor));
            Mint(engine, Blockchain.GetConsensusAddress(Blockchain.StandbyValidators), TotalAmount);
        }

        protected override void OnPersist(ApplicationEngine engine)
        {
            base.OnPersist(engine);

            // Set next committee
            if (ShouldRefreshCommittee(engine.Snapshot.Height))
            {
                StorageItem storageItem = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Committee));
                storageItem.Value = ComputeCommitteeMembers(engine.Snapshot).ToArray().ToByteArray();
            }
        }

        protected override void PostPersist(ApplicationEngine engine)
        {
            base.PostPersist(engine);

            // Distribute GAS for committee

            int index = (int)(engine.Snapshot.PersistingBlock.Index % (uint)ProtocolSettings.Default.CommitteeMembersCount);
            var gasPerBlock = GetGasPerBlock(engine.Snapshot);
            var pubkey = GetCommittee(engine.Snapshot)[index];
            var account = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
            GAS.Mint(engine, account, gasPerBlock * CommitteeRewardRatio / 100);
        }

        [ContractMethod(0_05000000, CallFlags.AllowModifyStates)]
        private bool SetGasPerBlock(ApplicationEngine engine, BigInteger gasPerBlock)
        {
            if (gasPerBlock < 0 || gasPerBlock > 10 * GAS.Factor)
                throw new ArgumentOutOfRangeException(nameof(gasPerBlock));
            if (!CheckCommittee(engine)) return false;

            uint index = engine.Snapshot.PersistingBlock.Index + 1;
            StorageItem entry = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_GasPerBlock).AddBigEndian(index), () => new StorageItem(gasPerBlock));
            entry.Set(gasPerBlock);
            return true;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public BigInteger GetGasPerBlock(StoreView snapshot)
        {
            return GetSortedGasRecords(snapshot, snapshot.PersistingBlock.Index + 1).First().gasPerBlock;
        }

        private IEnumerable<(uint index, BigInteger gasPerBlock)> GetSortedGasRecords(StoreView snapshot, uint to)
        {
            return snapshot.Storages.FindRange(
                CreateStorageKey(Prefix_GasPerBlock).ToArray(),
                CreateStorageKey(Prefix_GasPerBlock).AddBigEndian(0u).ToArray(), IO.Caching.SeekDirection.Backward).
                Select(u => (index: BinaryPrimitives.ReadUInt32BigEndian(u.Key.Key.AsSpan(u.Key.Key.Length - sizeof(uint))), gasPerBlock: (BigInteger)u.Value));
        }

        [ContractMethod(0_03000000, CallFlags.AllowStates)]
        public BigInteger UnclaimedGas(StoreView snapshot, UInt160 account, uint end)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_Account).Add(account));
            if (storage is null) return BigInteger.Zero;
            NeoAccountState state = storage.GetInteroperable<NeoAccountState>();
            return CalculateBonus(snapshot, state.Balance, state.BalanceHeight, end);
        }

        [ContractMethod(0_05000000, CallFlags.AllowModifyStates)]
        private bool RegisterCandidate(ApplicationEngine engine, ECPoint pubkey)
        {
            if (!engine.CheckWitnessInternal(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash()))
                return false;
            StorageKey key = CreateStorageKey(Prefix_Candidate).Add(pubkey);
            StorageItem item = engine.Snapshot.Storages.GetAndChange(key, () => new StorageItem(new CandidateState()));
            CandidateState state = item.GetInteroperable<CandidateState>();
            state.Registered = true;
            return true;
        }

        [ContractMethod(0_05000000, CallFlags.AllowModifyStates)]
        private bool UnregisterCandidate(ApplicationEngine engine, ECPoint pubkey)
        {
            if (!engine.CheckWitnessInternal(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash()))
                return false;
            StorageKey key = CreateStorageKey(Prefix_Candidate).Add(pubkey);
            if (engine.Snapshot.Storages.TryGet(key) is null) return true;
            StorageItem item = engine.Snapshot.Storages.GetAndChange(key);
            CandidateState state = item.GetInteroperable<CandidateState>();
            state.Registered = false;
            CheckCandidate(engine.Snapshot, key, state);
            return true;
        }

        [ContractMethod(5_00000000, CallFlags.AllowModifyStates)]
        private bool Vote(ApplicationEngine engine, UInt160 account, ECPoint voteTo)
        {
            if (!engine.CheckWitnessInternal(account)) return false;
            NeoAccountState state_account = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Account).Add(account))?.GetInteroperable<NeoAccountState>();
            if (state_account is null) return false;
            CandidateState validator_new = null;
            if (voteTo != null)
            {
                validator_new = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Candidate).Add(voteTo))?.GetInteroperable<CandidateState>();
                if (validator_new is null) return false;
                if (!validator_new.Registered) return false;
            }
            if (state_account.VoteTo is null ^ voteTo is null)
            {
                StorageItem item = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_VotersCount));
                if (state_account.VoteTo is null)
                    item.Add(state_account.Balance);
                else
                    item.Add(-state_account.Balance);
            }
            if (state_account.VoteTo != null)
            {
                StorageKey key = CreateStorageKey(Prefix_Candidate).Add(state_account.VoteTo);
                StorageItem storage_validator = engine.Snapshot.Storages.GetAndChange(key);
                CandidateState state_validator = storage_validator.GetInteroperable<CandidateState>();
                state_validator.Votes -= state_account.Balance;
                CheckCandidate(engine.Snapshot, key, state_validator);
            }
            state_account.VoteTo = voteTo;
            if (validator_new != null)
            {
                validator_new.Votes += state_account.Balance;
            }
            return true;
        }

        [ContractMethod(1_00000000, CallFlags.AllowStates)]
        public (ECPoint PublicKey, BigInteger Votes)[] GetCandidates(StoreView snapshot)
        {
            byte[] prefix_key = CreateStorageKey(Prefix_Candidate).ToArray();
            return snapshot.Storages.Find(prefix_key).Select(p =>
            (
                p.Key.Key.AsSerializable<ECPoint>(1),
                p.Value.GetInteroperable<CandidateState>()
            )).Where(p => p.Item2.Registered).Select(p => (p.Item1, p.Item2.Votes)).ToArray();
        }

        [ContractMethod(1_00000000, CallFlags.AllowStates)]
        public ECPoint[] GetCommittee(StoreView snapshot)
        {
            return GetCommitteeFromCache(snapshot).OrderBy(p => p).ToArray();
        }

        public UInt160 GetCommitteeAddress(StoreView snapshot)
        {
            ECPoint[] committees = GetCommittee(snapshot);
            return Contract.CreateMultiSigRedeemScript(committees.Length - (committees.Length - 1) / 2, committees).ToScriptHash();
        }

        private IEnumerable<ECPoint> GetCommitteeFromCache(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_Committee)].GetSerializableList<ECPoint>();
        }

        internal ECPoint[] ComputeNextBlockValidators(StoreView snapshot)
        {
            return ComputeCommitteeMembers(snapshot).Take(ProtocolSettings.Default.ValidatorsCount).OrderBy(p => p).ToArray();
        }

        private IEnumerable<ECPoint> ComputeCommitteeMembers(StoreView snapshot)
        {
            decimal votersCount = (decimal)(BigInteger)snapshot.Storages[CreateStorageKey(Prefix_VotersCount)];
            decimal VoterTurnout = votersCount / (decimal)TotalAmount;
            if (VoterTurnout < EffectiveVoterTurnout)
                return Blockchain.StandbyCommittee;
            var candidates = GetCandidates(snapshot);
            if (candidates.Length < ProtocolSettings.Default.CommitteeMembersCount)
                return Blockchain.StandbyCommittee;
            return candidates.OrderByDescending(p => p.Votes).ThenBy(p => p.PublicKey).Select(p => p.PublicKey).Take(ProtocolSettings.Default.CommitteeMembersCount);
        }

        [ContractMethod(1_00000000, CallFlags.AllowStates)]
        public ECPoint[] GetNextBlockValidators(StoreView snapshot)
        {
            return GetCommitteeFromCache(snapshot)
                .Take(ProtocolSettings.Default.ValidatorsCount)
                .OrderBy(p => p)
                .ToArray();
        }

        public class NeoAccountState : AccountState
        {
            public uint BalanceHeight;
            public ECPoint VoteTo;

            public override void FromStackItem(StackItem stackItem)
            {
                base.FromStackItem(stackItem);
                Struct @struct = (Struct)stackItem;
                BalanceHeight = (uint)@struct[1].GetInteger();
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
                Registered = @struct[0].GetBoolean();
                Votes = @struct[1].GetInteger();
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new Struct(referenceCounter) { Registered, Votes };
            }
        }
    }
}
