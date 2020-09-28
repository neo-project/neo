#pragma warning disable IDE0051

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.IO;
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

        public const decimal EffectiveVoterTurnout = 0.2M;

        private const byte Prefix_VotersCount = 1;
        private const byte Prefix_Candidate = 33;
        private const byte Prefix_Committee = 14;
        private const byte Prefix_GasPerBlock = 29;
        private const byte Prefix_VoterRewardPerCommittee = 23;

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
            CheckCandidate(engine.Snapshot, state.VoteTo, candidate);
        }

        private void DistributeGas(ApplicationEngine engine, UInt160 account, NeoAccountState state)
        {
            // PersistingBlock is null when running under the debugger
            if (engine.Snapshot.PersistingBlock == null) return;

            BigInteger gas = CalculateBonus(engine.Snapshot, state.VoteTo, state.Balance, state.BalanceHeight, engine.Snapshot.PersistingBlock.Index);
            state.BalanceHeight = engine.Snapshot.PersistingBlock.Index;
            GAS.Mint(engine, account, gas);
        }

        private BigInteger CalculateBonus(StoreView snapshot, ECPoint vote, BigInteger value, uint start, uint end)
        {
            if (value.IsZero || start >= end) return BigInteger.Zero;
            if (value.Sign < 0) throw new ArgumentOutOfRangeException(nameof(value));

            BigInteger neoHolderReward = CalculateNeoHolderReward(snapshot, value, start, end);
            if (vote is null) return neoHolderReward;

            byte[] border = CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(vote).ToArray();
            byte[] keyStart = CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(vote).AddBigEndian(start).ToArray();
            (_, var item) = snapshot.Storages.FindRange(keyStart, border, SeekDirection.Backward).FirstOrDefault();
            BigInteger startRewardPerNeo = item ?? BigInteger.Zero;

            byte[] keyEnd = CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(vote).AddBigEndian(end).ToArray();
            (_, item) = snapshot.Storages.FindRange(keyEnd, border, SeekDirection.Backward).FirstOrDefault();
            BigInteger endRewardPerNeo = item ?? BigInteger.Zero;

            return neoHolderReward + value * (endRewardPerNeo - startRewardPerNeo) / 100000000L;
        }

        private BigInteger CalculateNeoHolderReward(StoreView snapshot, BigInteger value, uint start, uint end)
        {
            GasRecord gasRecord = snapshot.Storages[CreateStorageKey(Prefix_GasPerBlock)].GetInteroperable<GasRecord>();
            BigInteger sum = 0;
            for (var i = gasRecord.Count - 1; i >= 0; i--)
            {
                var currentIndex = gasRecord[i].Index;
                if (currentIndex >= end) continue;
                if (currentIndex > start)
                {
                    sum += gasRecord[i].GasPerBlock * (end - currentIndex);
                    end = currentIndex;
                }
                else
                {
                    sum += gasRecord[i].GasPerBlock * (end - start);
                    break;
                }
            }
            return value * sum * NeoHolderRewardRatio / 100 / TotalAmount;
        }

        private void CheckCandidate(StoreView snapshot, ECPoint pubkey, CandidateState candidate)
        {
            if (!candidate.Registered && candidate.Votes.IsZero)
            {
                foreach (var (rewardKey, _) in snapshot.Storages.Find(CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(pubkey).ToArray()))
                    snapshot.Storages.Delete(rewardKey);
                snapshot.Storages.Delete(CreateStorageKey(Prefix_Candidate).Add(pubkey));
            }
        }

        private bool ShouldRefreshCommittee(uint height) => height % ProtocolSettings.Default.CommitteeMembersCount == 0;

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Committee), new StorageItem(Blockchain.StandbyCommittee.Select(p => new CommitteeState { PublicKey = p }).ToArray().ToByteArray()));
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_VotersCount), new StorageItem(new byte[0]));

            // Initialize economic parameters

            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_GasPerBlock), new StorageItem(new GasRecord
            {
                (0, 5 * GAS.Factor)
            }));

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

            int m = ProtocolSettings.Default.CommitteeMembersCount;
            int n = ProtocolSettings.Default.ValidatorsCount;
            int index = (int)(engine.Snapshot.PersistingBlock.Index % (uint)m);
            var gasPerBlock = GetGasPerBlock(engine.Snapshot);
            var committeeState = GetCommitteeFromCache(engine.Snapshot).ElementAt(index);
            var pubkey = committeeState.PublicKey;
            var account = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
            GAS.Mint(engine, account, gasPerBlock * CommitteeRewardRatio / 100);

            // Record the cumulative reward of the voters of committee

            var factor = index < n ? 2 : 1;
            if (committeeState.Votes > 0)
            {
                BigInteger voterSumRewardPerNEO = factor * gasPerBlock * VoterRewardRatio * 100000000L * m / (m + n) / 100 / committeeState.Votes; // Zoom in 100000000 times, and the final calculation should be divided 100000000L
                StorageKey voterRewardKey = CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(pubkey).AddBigEndian(engine.Snapshot.PersistingBlock.Index);
                byte[] border = CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(pubkey).ToArray();
                (_, var item) = engine.Snapshot.Storages.FindRange(voterRewardKey.ToArray(), border, SeekDirection.Backward).FirstOrDefault();
                voterSumRewardPerNEO += (item ?? BigInteger.Zero);
                engine.Snapshot.Storages.Add(voterRewardKey, new StorageItem(voterSumRewardPerNEO));
            }
        }

        [ContractMethod(0_05000000, CallFlags.AllowModifyStates)]
        private bool SetGasPerBlock(ApplicationEngine engine, BigInteger gasPerBlock)
        {
            if (gasPerBlock < 0 || gasPerBlock > 10 * GAS.Factor)
                throw new ArgumentOutOfRangeException(nameof(gasPerBlock));
            if (!CheckCommittee(engine)) return false;
            uint index = engine.Snapshot.PersistingBlock.Index + 1;
            GasRecord gasRecord = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_GasPerBlock)).GetInteroperable<GasRecord>();
            if (gasRecord[^1].Index == index)
                gasRecord[^1] = (index, gasPerBlock);
            else
                gasRecord.Add((index, gasPerBlock));
            return true;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public BigInteger GetGasPerBlock(StoreView snapshot)
        {
            var index = snapshot.PersistingBlock.Index;
            GasRecord gasRecord = snapshot.Storages[CreateStorageKey(Prefix_GasPerBlock)].GetInteroperable<GasRecord>();
            for (var i = gasRecord.Count - 1; i >= 0; i--)
            {
                if (gasRecord[i].Index <= index)
                    return gasRecord[i].GasPerBlock;
            }
            throw new InvalidOperationException();
        }

        [ContractMethod(0_03000000, CallFlags.AllowStates)]
        public BigInteger UnclaimedGas(StoreView snapshot, UInt160 account, uint end)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_Account).Add(account));
            if (storage is null) return BigInteger.Zero;
            NeoAccountState state = storage.GetInteroperable<NeoAccountState>();
            return CalculateBonus(snapshot, state.VoteTo, state.Balance, state.BalanceHeight, end);
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
            CheckCandidate(engine.Snapshot, pubkey, state);
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
            DistributeGas(engine, account, state_account);
            if (state_account.VoteTo != null)
            {
                StorageKey key = CreateStorageKey(Prefix_Candidate).Add(state_account.VoteTo);
                StorageItem storage_validator = engine.Snapshot.Storages.GetAndChange(key);
                CandidateState state_validator = storage_validator.GetInteroperable<CandidateState>();
                state_validator.Votes -= state_account.Balance;
                CheckCandidate(engine.Snapshot, state_account.VoteTo, state_validator);
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
            return GetCommitteeFromCache(snapshot).Select(p => p.PublicKey).OrderBy(p => p).ToArray();
        }

        public UInt160 GetCommitteeAddress(StoreView snapshot)
        {
            ECPoint[] committees = GetCommittee(snapshot);
            return Contract.CreateMultiSigRedeemScript(committees.Length - (committees.Length - 1) / 2, committees).ToScriptHash();
        }

        private IEnumerable<CommitteeState> GetCommitteeFromCache(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_Committee)].GetSerializableList<CommitteeState>();
        }

        internal ECPoint[] ComputeNextBlockValidators(StoreView snapshot)
        {
            return ComputeCommitteeMembers(snapshot).Select(p => p.PublicKey).Take(ProtocolSettings.Default.ValidatorsCount).OrderBy(p => p).ToArray();
        }

        private IEnumerable<CommitteeState> ComputeCommitteeMembers(StoreView snapshot)
        {
            decimal votersCount = (decimal)(BigInteger)snapshot.Storages[CreateStorageKey(Prefix_VotersCount)];
            decimal VoterTurnout = votersCount / (decimal)TotalAmount;
            if (VoterTurnout < EffectiveVoterTurnout)
                return Blockchain.StandbyCommittee.Select(p => new CommitteeState { PublicKey = p });
            var candidates = GetCandidates(snapshot);
            if (candidates.Length < ProtocolSettings.Default.CommitteeMembersCount)
                return Blockchain.StandbyCommittee.Select(p => new CommitteeState { PublicKey = p });
            return candidates.OrderByDescending(p => p.Votes).ThenBy(p => p.PublicKey).Select(p => new CommitteeState { PublicKey = p.PublicKey, Votes = p.Votes }).Take(ProtocolSettings.Default.CommitteeMembersCount);
        }

        [ContractMethod(1_00000000, CallFlags.AllowStates)]
        public ECPoint[] GetNextBlockValidators(StoreView snapshot)
        {
            return GetCommitteeFromCache(snapshot)
                .Take(ProtocolSettings.Default.ValidatorsCount)
                .Select(p => p.PublicKey)
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

        private sealed class GasRecord : List<(uint Index, BigInteger GasPerBlock)>, IInteroperable
        {
            public void FromStackItem(StackItem stackItem)
            {
                foreach (StackItem item in (Array)stackItem)
                {
                    Struct @struct = (Struct)item;
                    Add(((uint)@struct[0].GetInteger(), @struct[1].GetInteger()));
                }
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new Array(referenceCounter, this.Select(p => new Struct(referenceCounter, new StackItem[] { p.Index, p.GasPerBlock })));
            }
        }

        private sealed class CommitteeState : ISerializable
        {
            public ECPoint PublicKey;
            public BigInteger Votes = BigInteger.Zero;

            public int Size => PublicKey.Size + sizeof(long);

            public void Deserialize(BinaryReader reader)
            {
                PublicKey = reader.ReadSerializable<ECPoint>();
                Votes = reader.ReadInt64();
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(PublicKey);
                writer.WriteVarInt((long)Votes);
            }
        }
    }
}
