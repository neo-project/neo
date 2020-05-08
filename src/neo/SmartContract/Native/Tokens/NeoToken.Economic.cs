
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
    public partial class NeoToken
    {
        private const byte Prefix_Epoch = 17;
        private const byte Prefix_EconomicEpoch = 19;
        private const byte Prefix_CommitteeEpoch = 23;

        private void OnPersistEpochState(ApplicationEngine engine)
        {
            if (engine.Snapshot.PersistingBlock.Index % Blockchain.Epoch != Blockchain.Epoch - 1)
                return;

            uint currentHeight = engine.Snapshot.PersistingBlock.Index;

            EpochState epochState = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Epoch), () => new StorageItem(new EpochState())).GetInteroperable<EpochState>();
            EconomicParameter economicParameter = GetEconomicParameter(engine);
            EconomicEpochState economicEpoch = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_EconomicEpoch, epochState.EconomicId), () => new StorageItem(new EconomicEpochState())).GetInteroperable<EconomicEpochState>();
            economicEpoch.End = currentHeight;
            if (!economicEpoch.CompareTo(economicParameter))
            {
                EconomicEpochState newEconomicEpoch = new EconomicEpochState(economicParameter, currentHeight);
                engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_EconomicEpoch, ++epochState.EconomicId), new StorageItem(newEconomicEpoch));
            }

            IEnumerable<(ECPoint, BigInteger)> committees = GetCommitteeMembers(engine.Snapshot, Blockchain.CommitteeMembersCount);
            CommitteesEpochState committeeEpoch = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_CommitteeEpoch, epochState.CommitteeId), () => new StorageItem(new CommitteesEpochState())).GetInteroperable<CommitteesEpochState>();
            committeeEpoch.End = currentHeight;
            if (!committeeEpoch.CompareTo(committees))
            {
                CommitteesEpochState newCommitteeEpoch = new CommitteesEpochState(committees, currentHeight);
                engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_CommitteeEpoch, ++epochState.CommitteeId), new StorageItem(newCommitteeEpoch));
            }
        }

        private void DistributeGas(ApplicationEngine engine, UInt160 account, AccountState state)
        {
            BigInteger gas = CalculateBonus(engine.Snapshot, account, state.VoteTo, state.Balance, state.BalanceHeight, engine.Snapshot.PersistingBlock.Index, out uint claimedHeight);
            state.BalanceHeight = claimedHeight;
            GAS.Mint(engine, account, gas);
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
            AccountState state = storage.GetInteroperable<AccountState>();
            return CalculateBonus(snapshot, account, state.VoteTo, state.Balance, state.BalanceHeight, end, out _);
        }

        public BigInteger CalculateBonus(StoreView snapshot, UInt160 account, ECPoint votee, BigInteger votes, uint start, uint end, out uint claimedHeight)
        {
            claimedHeight = start;
            if (votes.IsZero || start >= end) return BigInteger.Zero;
            if (votes.Sign < 0) throw new ArgumentOutOfRangeException(nameof(votes));

            EpochState epochState = snapshot.Storages.TryGet(CreateStorageKey(Prefix_Epoch))?.GetInteroperable<EpochState>();
            if (epochState is null) return BigInteger.Zero;

            uint economicId = epochState.EconomicId;
            uint committeeId = epochState.CommitteeId;
            EconomicEpochState economicEpochState = snapshot.Storages.TryGet(CreateStorageKey(Prefix_EconomicEpoch, economicId)).GetInteroperable<EconomicEpochState>();
            CommitteesEpochState committeeEpochState = snapshot.Storages.TryGet(CreateStorageKey(Prefix_CommitteeEpoch, committeeId)).GetInteroperable< CommitteesEpochState>();

            BigInteger unClaimGas = 0;
            while (economicEpochState.Start >= end)
            {
                economicEpochState = snapshot.Storages.TryGet(CreateStorageKey(Prefix_EconomicEpoch, --economicId)).GetInteroperable<EconomicEpochState>();
            }
            while (economicEpochState.End > start)
            {
                uint aStart = Math.Max(economicEpochState.Start, start);
                uint aEnd = Math.Min(economicEpochState.End, end);

                while (committeeEpochState.Start >= aEnd)
                {
                    committeeEpochState = snapshot.Storages.TryGet(CreateStorageKey(Prefix_CommitteeEpoch, --committeeId)).GetInteroperable<CommitteesEpochState>();
                }
                while (committeeEpochState.End > aStart)
                {
                    uint bStart = Math.Max(committeeEpochState.Start, aStart);
                    uint bEnd = Math.Min(committeeEpochState.End, aEnd);

                    BigInteger totalGas = (bEnd - bStart) * economicEpochState.GasPerBlock;
                    BigInteger totalRewardRatio = economicEpochState.TotalRewardRatio;

                    // More incentive model details, see https://github.com/neo-project/neo/issues/1617#issue-607424930
                    if (votee != null && committeeEpochState.TryGetVotes(votee, out BigInteger totalVotes))
                    {
                        int ratio = committeeEpochState.IsValidator(votee) ? 2 : 1;
                        int baseCount = Blockchain.CommitteeMembersCount + Blockchain.ValidatorsCount;
                        unClaimGas += ratio * totalGas * economicEpochState.VotersRewardRatio * votes / totalRewardRatio / totalVotes / baseCount; // Voter reward
                    }
                    if (committeeEpochState.IsCommittee(account))
                    {
                        unClaimGas += totalGas * economicEpochState.CommitteesRewardRatio / totalRewardRatio / Blockchain.CommitteeMembersCount; // Committee reward
                    }
                    unClaimGas += totalGas * economicEpochState.NeoHoldersRewardRatio * votes / totalRewardRatio / this.TotalAmount; // Neo holder reward

                    if (bEnd > claimedHeight)
                    {
                        claimedHeight = bEnd;
                    }
                    committeeEpochState = snapshot.Storages.TryGet(CreateStorageKey(Prefix_CommitteeEpoch, --committeeId)).GetInteroperable<CommitteesEpochState>();
                }

                economicEpochState = snapshot.Storages.TryGet(CreateStorageKey(Prefix_EconomicEpoch, --economicId)).GetInteroperable<EconomicEpochState>();
            }
            return unClaimGas;
        }

        internal class EpochState : IInteroperable
        {
            public uint CommitteeId;
            public uint EconomicId;

            public void FromStackItem(StackItem stackItem)
            {
                CommitteeId = (uint)((Struct)stackItem)[0].GetBigInteger();
            EconomicId = (uint)((Struct)stackItem)[1].GetBigInteger();
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new Struct(referenceCounter) { CommitteeId, EconomicId };
            }
        }

        internal class EconomicEpochState : EconomicParameter
        {
            public uint Start;
            public uint End;

            public EconomicEpochState() { }

            public EconomicEpochState(EconomicParameter config, uint start)
            {
                GasPerBlock = config.GasPerBlock;
                NeoHoldersRewardRatio = config.NeoHoldersRewardRatio;
                CommitteesRewardRatio = config.CommitteesRewardRatio;
                VotersRewardRatio = config.VotersRewardRatio;
                Start = start;
            }

            public override void FromStackItem(StackItem stackItem)
            {
                base.FromStackItem(stackItem);
                Start = (uint)((Struct)stackItem)[4].GetBigInteger();
                End = (uint)((Struct)stackItem)[5].GetBigInteger();
            }

            public override StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                Struct @struct = (Struct) base.ToStackItem(referenceCounter);
                @struct.Add(Start);
                @struct.Add(End);
                return @struct;
            }

            internal bool CompareTo(EconomicParameter parameter)
            {
                if (GasPerBlock != parameter.GasPerBlock) return false;
                if (NeoHoldersRewardRatio != parameter.NeoHoldersRewardRatio) return false;
                if (CommitteesRewardRatio != parameter.CommitteesRewardRatio) return false;
                if (VotersRewardRatio != parameter.VotersRewardRatio) return false;
                return true;
            }
        }

        internal class CommitteesEpochState : IInteroperable
        {
            public uint Start;
            public uint End = uint.MaxValue;
            public (ECPoint, BigInteger, UInt160)[] Committees;

            public CommitteesEpochState() { }

            public CommitteesEpochState(IEnumerable<(ECPoint, BigInteger)> enumerator, uint start)
            {
                Start = start;
                Committees = new (ECPoint, BigInteger, UInt160)[enumerator.Count()];
                var i = 0;
                foreach((ECPoint, BigInteger) item in enumerator)
                {
                    Committees[i].Item1 = item.Item1;
                    Committees[i].Item2 = item.Item2;
                    Committees[i].Item3 = Contract.CreateSignatureContract(item.Item1).ScriptHash;
                }
            }

            public bool IsValidator(ECPoint publicKey)
            {
                for (uint i = 0; i < Blockchain.ValidatorsCount; i++)
                    if (Committees[i].Item1.Equals(publicKey))
                        return true;
                return false;
            }

            public bool IsCommittee(UInt160 address)
            {
                for (uint i = 0; i < Blockchain.CommitteeMembersCount; i++)
                    if (Committees[i].Item3.Equals(address))
                        return true;
                return false;
            }

            public bool TryGetVotes(ECPoint publicKey, out BigInteger votes)
            {
                for (uint i = 0; i < Committees.Length; i++)
                {
                    if (Committees[i].Item1.Equals(publicKey))
                    {
                        votes = Committees[i].Item2;
                        return true;
                    }
                }
                votes = 0;
                return false;
            }

            public virtual void FromStackItem(StackItem stackItem)
            {
                Struct @struct = (Struct)stackItem;
                Start = (uint)@struct[0].GetBigInteger();
                End = (uint)@struct[1].GetBigInteger();
                
                Array votees = (Array)@struct[2];
                Array votes = (Array)@struct[3];
                Array voters = (Array)@struct[4];

                Committees = new (ECPoint, BigInteger, UInt160)[votees.Count];
                for(var i = 0; i < votees.Count; i++)
                {
                    Committees[i].Item1 = votees[i].GetSpan().AsSerializable<ECPoint>();
                    Committees[i].Item2 = votes[i].GetBigInteger();
                    Committees[i].Item3 = voters[i].GetSpan().AsSerializable<UInt160>();
                }
            }

            public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                Struct @struct = new Struct(referenceCounter) { Start, End };
                Array votees = new Array(referenceCounter);
                Array votes = new Array(referenceCounter);
                Array voters = new Array(referenceCounter);
                foreach((ECPoint, BigInteger, UInt160) item in Committees)
                {
                    votees.Add(item.Item1.ToArray());
                    votes.Add(item.Item2);
                    voters.Add(item.Item3.ToArray());
                }
                @struct.Add(votees);
                @struct.Add(votes);
                @struct.Add(voters);
                return @struct;
            }

            internal bool CompareTo(IEnumerable<(ECPoint, BigInteger)> enumerable)
            {
                if (Committees is null || Committees.Length != enumerable.Count())
                    return false;

                var i = 0;
                foreach((ECPoint, BigInteger) item in enumerable)
                {
                    if (!item.Item1.Equals(Committees[i].Item1)) return false;
                    if (!item.Item2.Equals(Committees[i].Item2)) return false;
                    i++;
                }
                return true;
            }
        }
    }
}
