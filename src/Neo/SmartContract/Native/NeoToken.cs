// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents the NEO token in the NEO system.
    /// </summary>
    public sealed class NeoToken : FungibleToken<NeoToken.NeoAccountState>
    {
        public override string Symbol => "NEO";
        public override byte Decimals => 0;

        /// <summary>
        /// Indicates the total amount of NEO.
        /// </summary>
        public BigInteger TotalAmount { get; }

        /// <summary>
        /// Indicates the effective voting turnout in NEO. The voted candidates will only be effective when the voting turnout exceeds this value.
        /// </summary>
        public const decimal EffectiveVoterTurnout = 0.2M;

        private const byte Prefix_VotersCount = 1;
        private const byte Prefix_Candidate = 33;
        private const byte Prefix_Committee = 14;
        private const byte Prefix_GasPerBlock = 29;
        private const byte Prefix_RegisterPrice = 13;
        private const byte Prefix_VoterRewardPerCommittee = 23;

        private const byte NeoHolderRewardRatio = 10;
        private const byte CommitteeRewardRatio = 10;
        private const byte VoterRewardRatio = 80;

        internal NeoToken()
        {
            this.TotalAmount = 100000000 * Factor;

            var events = new List<ContractEventDescriptor>(Manifest.Abi.Events)
            {
                new ContractEventDescriptor
                {
                    Name = "CandidateStateChanged",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "pubkey",
                            Type = ContractParameterType.PublicKey
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "registered",
                            Type = ContractParameterType.Boolean
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "votes",
                            Type = ContractParameterType.Integer
                        }
                    }
                },
                new ContractEventDescriptor
                {
                    Name = "Vote",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "account",
                            Type = ContractParameterType.Hash160
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "from",
                            Type = ContractParameterType.PublicKey
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "to",
                            Type = ContractParameterType.PublicKey
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "amount",
                            Type = ContractParameterType.Integer
                        }
                    }
                }
            };

            Manifest.Abi.Events = events.ToArray();
        }

        public override BigInteger TotalSupply(DataCache snapshot)
        {
            return TotalAmount;
        }

        internal override void OnBalanceChanging(ApplicationEngine engine, UInt160 account, NeoAccountState state, BigInteger amount)
        {
            GasDistribution distribution = DistributeGas(engine, account, state);
            if (distribution is not null)
            {
                var list = engine.CurrentContext.GetState<List<GasDistribution>>();
                list.Add(distribution);
            }
            if (amount.IsZero) return;
            if (state.VoteTo is null) return;
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_VotersCount)).Add(amount);
            StorageKey key = CreateStorageKey(Prefix_Candidate).Add(state.VoteTo);
            CandidateState candidate = engine.Snapshot.GetAndChange(key).GetInteroperable<CandidateState>();
            candidate.Votes += amount;
            CheckCandidate(engine.Snapshot, state.VoteTo, candidate);
        }

        private protected override async ContractTask PostTransfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, StackItem data, bool callOnPayment)
        {
            await base.PostTransfer(engine, from, to, amount, data, callOnPayment);
            var list = engine.CurrentContext.GetState<List<GasDistribution>>();
            foreach (var distribution in list)
                await GAS.Mint(engine, distribution.Account, distribution.Amount, callOnPayment);
        }

        private GasDistribution DistributeGas(ApplicationEngine engine, UInt160 account, NeoAccountState state)
        {
            // PersistingBlock is null when running under the debugger
            if (engine.PersistingBlock is null) return null;

            BigInteger gas = CalculateBonus(engine.Snapshot, state, engine.PersistingBlock.Index);
            state.BalanceHeight = engine.PersistingBlock.Index;
            if (state.VoteTo is not null)
            {
                var keyLastest = CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(state.VoteTo);
                var latestGasPerVote = engine.Snapshot.TryGet(keyLastest) ?? BigInteger.Zero;
                state.LastGasPerVote = latestGasPerVote;
            }
            if (gas == 0) return null;
            return new GasDistribution
            {
                Account = account,
                Amount = gas
            };
        }

        private BigInteger CalculateBonus(DataCache snapshot, NeoAccountState state, uint end)
        {
            if (state.Balance.IsZero) return BigInteger.Zero;
            if (state.Balance.Sign < 0) throw new ArgumentOutOfRangeException(nameof(state.Balance));

            var expectEnd = Ledger.CurrentIndex(snapshot) + 1;
            if (expectEnd != end) throw new ArgumentOutOfRangeException(nameof(end));
            if (state.BalanceHeight >= end) return BigInteger.Zero;
            BigInteger neoHolderReward = CalculateNeoHolderReward(snapshot, state.Balance, state.BalanceHeight, end);
            if (state.VoteTo is null) return neoHolderReward;

            var keyLastest = CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(state.VoteTo);
            var latestGasPerVote = snapshot.TryGet(keyLastest) ?? BigInteger.Zero;
            var voteReward = state.Balance * (latestGasPerVote - state.LastGasPerVote) / 100000000L;

            return neoHolderReward + voteReward;
        }

        private BigInteger CalculateNeoHolderReward(DataCache snapshot, BigInteger value, uint start, uint end)
        {
            BigInteger sum = 0;
            foreach (var (index, gasPerBlock) in GetSortedGasRecords(snapshot, end - 1))
            {
                if (index > start)
                {
                    sum += gasPerBlock * (end - index);
                    end = index;
                }
                else
                {
                    sum += gasPerBlock * (end - start);
                    break;
                }
            }
            return value * sum * NeoHolderRewardRatio / 100 / TotalAmount;
        }

        private void CheckCandidate(DataCache snapshot, ECPoint pubkey, CandidateState candidate)
        {
            if (!candidate.Registered && candidate.Votes.IsZero)
            {
                snapshot.Delete(CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(pubkey));
                snapshot.Delete(CreateStorageKey(Prefix_Candidate).Add(pubkey));
            }
        }

        /// <summary>
        /// Determine whether the votes should be recounted at the specified height.
        /// </summary>
        /// <param name="height">The height to be checked.</param>
        /// <param name="committeeMembersCount">The number of committee members in the system.</param>
        /// <returns><see langword="true"/> if the votes should be recounted; otherwise, <see langword="false"/>.</returns>
        public static bool ShouldRefreshCommittee(uint height, int committeeMembersCount) => height % committeeMembersCount == 0;

        internal override ContractTask Initialize(ApplicationEngine engine)
        {
            var cachedCommittee = new CachedCommittee(engine.ProtocolSettings.StandbyCommittee.Select(p => (p, BigInteger.Zero)));
            engine.Snapshot.Add(CreateStorageKey(Prefix_Committee), new StorageItem(cachedCommittee));
            engine.Snapshot.Add(CreateStorageKey(Prefix_VotersCount), new StorageItem(System.Array.Empty<byte>()));
            engine.Snapshot.Add(CreateStorageKey(Prefix_GasPerBlock).AddBigEndian(0u), new StorageItem(5 * GAS.Factor));
            engine.Snapshot.Add(CreateStorageKey(Prefix_RegisterPrice), new StorageItem(1000 * GAS.Factor));
            return Mint(engine, Contract.GetBFTAddress(engine.ProtocolSettings.StandbyValidators), TotalAmount, false);
        }

        internal override ContractTask OnPersist(ApplicationEngine engine)
        {
            // Set next committee
            if (ShouldRefreshCommittee(engine.PersistingBlock.Index, engine.ProtocolSettings.CommitteeMembersCount))
            {
                StorageItem storageItem = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Committee));
                var cachedCommittee = storageItem.GetInteroperable<CachedCommittee>();
                cachedCommittee.Clear();
                cachedCommittee.AddRange(ComputeCommitteeMembers(engine.Snapshot, engine.ProtocolSettings));
            }
            return ContractTask.CompletedTask;
        }

        internal override async ContractTask PostPersist(ApplicationEngine engine)
        {
            // Distribute GAS for committee

            int m = engine.ProtocolSettings.CommitteeMembersCount;
            int n = engine.ProtocolSettings.ValidatorsCount;
            int index = (int)(engine.PersistingBlock.Index % (uint)m);
            var gasPerBlock = GetGasPerBlock(engine.Snapshot);
            var committee = GetCommitteeFromCache(engine.Snapshot);
            var pubkey = committee[index].PublicKey;
            var account = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
            await GAS.Mint(engine, account, gasPerBlock * CommitteeRewardRatio / 100, false);

            // Record the cumulative reward of the voters of committee

            if (ShouldRefreshCommittee(engine.PersistingBlock.Index, m))
            {
                BigInteger voterRewardOfEachCommittee = gasPerBlock * VoterRewardRatio * 100000000L * m / (m + n) / 100; // Zoom in 100000000 times, and the final calculation should be divided 100000000L
                for (index = 0; index < committee.Count; index++)
                {
                    var (PublicKey, Votes) = committee[index];
                    var factor = index < n ? 2 : 1; // The `voter` rewards of validator will double than other committee's
                    if (Votes > 0)
                    {
                        BigInteger voterSumRewardPerNEO = factor * voterRewardOfEachCommittee / Votes;
                        StorageKey voterRewardKey = CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(PublicKey);
                        StorageItem lastRewardPerNeo = engine.Snapshot.GetAndChange(voterRewardKey, () => new StorageItem(BigInteger.Zero));
                        lastRewardPerNeo.Add(voterSumRewardPerNEO);
                    }
                }
            }
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetGasPerBlock(ApplicationEngine engine, BigInteger gasPerBlock)
        {
            if (gasPerBlock < 0 || gasPerBlock > 10 * GAS.Factor)
                throw new ArgumentOutOfRangeException(nameof(gasPerBlock));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();

            uint index = engine.PersistingBlock.Index + 1;
            StorageItem entry = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_GasPerBlock).AddBigEndian(index), () => new StorageItem(gasPerBlock));
            entry.Set(gasPerBlock);
        }

        /// <summary>
        /// Gets the amount of GAS generated in each block.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The amount of GAS generated.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public BigInteger GetGasPerBlock(DataCache snapshot)
        {
            return GetSortedGasRecords(snapshot, Ledger.CurrentIndex(snapshot) + 1).First().GasPerBlock;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetRegisterPrice(ApplicationEngine engine, long registerPrice)
        {
            if (registerPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(registerPrice));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_RegisterPrice)).Set(registerPrice);
        }

        /// <summary>
        /// Gets the fees to be paid to register as a candidate.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The amount of the fees.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public long GetRegisterPrice(DataCache snapshot)
        {
            return (long)(BigInteger)snapshot[CreateStorageKey(Prefix_RegisterPrice)];
        }

        private IEnumerable<(uint Index, BigInteger GasPerBlock)> GetSortedGasRecords(DataCache snapshot, uint end)
        {
            byte[] key = CreateStorageKey(Prefix_GasPerBlock).AddBigEndian(end).ToArray();
            byte[] boundary = CreateStorageKey(Prefix_GasPerBlock).ToArray();
            return snapshot.FindRange(key, boundary, SeekDirection.Backward)
                .Select(u => (BinaryPrimitives.ReadUInt32BigEndian(u.Key.Key.Span[^sizeof(uint)..]), (BigInteger)u.Value));
        }

        /// <summary>
        /// Get the amount of unclaimed GAS in the specified account.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="account">The account to check.</param>
        /// <param name="end">The block index used when calculating GAS.</param>
        /// <returns>The amount of unclaimed GAS.</returns>
        [ContractMethod(CpuFee = 1 << 17, RequiredCallFlags = CallFlags.ReadStates)]
        public BigInteger UnclaimedGas(DataCache snapshot, UInt160 account, uint end)
        {
            StorageItem storage = snapshot.TryGet(CreateStorageKey(Prefix_Account).Add(account));
            if (storage is null) return BigInteger.Zero;
            NeoAccountState state = storage.GetInteroperable<NeoAccountState>();
            return CalculateBonus(snapshot, state, end);
        }

        [ContractMethod(RequiredCallFlags = CallFlags.States)]
        private bool RegisterCandidate(ApplicationEngine engine, ECPoint pubkey)
        {
            if (!engine.CheckWitnessInternal(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash()))
                return false;
            engine.AddGas(GetRegisterPrice(engine.Snapshot));
            StorageKey key = CreateStorageKey(Prefix_Candidate).Add(pubkey);
            StorageItem item = engine.Snapshot.GetAndChange(key, () => new StorageItem(new CandidateState()));
            CandidateState state = item.GetInteroperable<CandidateState>();
            if (state.Registered) return true;
            state.Registered = true;
            engine.SendNotification(Hash, "CandidateStateChanged",
                new VM.Types.Array(engine.ReferenceCounter) { pubkey.ToArray(), true, state.Votes });
            return true;
        }

        [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.States)]
        private bool UnregisterCandidate(ApplicationEngine engine, ECPoint pubkey)
        {
            if (!engine.CheckWitnessInternal(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash()))
                return false;
            StorageKey key = CreateStorageKey(Prefix_Candidate).Add(pubkey);
            if (engine.Snapshot.TryGet(key) is null) return true;
            StorageItem item = engine.Snapshot.GetAndChange(key);
            CandidateState state = item.GetInteroperable<CandidateState>();
            if (!state.Registered) return true;
            state.Registered = false;
            CheckCandidate(engine.Snapshot, pubkey, state);
            engine.SendNotification(Hash, "CandidateStateChanged",
                new VM.Types.Array(engine.ReferenceCounter) { pubkey.ToArray(), false, state.Votes });
            return true;
        }

        [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.States)]
        private async ContractTask<bool> Vote(ApplicationEngine engine, UInt160 account, ECPoint voteTo)
        {
            if (!engine.CheckWitnessInternal(account)) return false;
            NeoAccountState state_account = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Account).Add(account))?.GetInteroperable<NeoAccountState>();
            if (state_account is null) return false;
            if (state_account.Balance == 0) return false;
            CandidateState validator_new = null;
            if (voteTo != null)
            {
                validator_new = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Candidate).Add(voteTo))?.GetInteroperable<CandidateState>();
                if (validator_new is null) return false;
                if (!validator_new.Registered) return false;
            }
            if (state_account.VoteTo is null ^ voteTo is null)
            {
                StorageItem item = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_VotersCount));
                if (state_account.VoteTo is null)
                    item.Add(state_account.Balance);
                else
                    item.Add(-state_account.Balance);
            }
            GasDistribution gasDistribution = DistributeGas(engine, account, state_account);
            if (state_account.VoteTo != null)
            {
                StorageKey key = CreateStorageKey(Prefix_Candidate).Add(state_account.VoteTo);
                StorageItem storage_validator = engine.Snapshot.GetAndChange(key);
                CandidateState state_validator = storage_validator.GetInteroperable<CandidateState>();
                state_validator.Votes -= state_account.Balance;
                CheckCandidate(engine.Snapshot, state_account.VoteTo, state_validator);
            }
            if (voteTo != null && voteTo != state_account.VoteTo)
            {
                StorageKey voterRewardKey = CreateStorageKey(Prefix_VoterRewardPerCommittee).Add(voteTo);
                var latestGasPerVote = engine.Snapshot.TryGet(voterRewardKey) ?? BigInteger.Zero;
                state_account.LastGasPerVote = latestGasPerVote;
            }
            ECPoint from = state_account.VoteTo;
            state_account.VoteTo = voteTo;

            if (validator_new != null)
            {
                validator_new.Votes += state_account.Balance;
            }
            engine.SendNotification(Hash, "Vote",
                new VM.Types.Array(engine.ReferenceCounter) { account.ToArray(), from?.ToArray() ?? StackItem.Null, voteTo?.ToArray() ?? StackItem.Null, state_account.Balance });
            if (gasDistribution is not null)
                await GAS.Mint(engine, gasDistribution.Account, gasDistribution.Amount, true);
            return true;
        }

        /// <summary>
        /// Gets the first 256 registered candidates.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>All the registered candidates.</returns>
        [ContractMethod(CpuFee = 1 << 22, RequiredCallFlags = CallFlags.ReadStates)]
        private (ECPoint PublicKey, BigInteger Votes)[] GetCandidates(DataCache snapshot)
        {
            return GetCandidatesInternal(snapshot)
                .Select(p => (p.PublicKey, p.State.Votes))
                .Take(256)
                .ToArray();
        }

        /// <summary>
        /// Gets the registered candidates iterator.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>All the registered candidates.</returns>
        [ContractMethod(CpuFee = 1 << 22, RequiredCallFlags = CallFlags.ReadStates)]
        private IIterator GetAllCandidates(DataCache snapshot)
        {
            const FindOptions options = FindOptions.RemovePrefix | FindOptions.DeserializeValues | FindOptions.PickField1;
            var enumerator = GetCandidatesInternal(snapshot)
                .Select(p => (p.Key, p.Value))
                .GetEnumerator();
            return new StorageIterator(enumerator, 1, options);
        }

        internal IEnumerable<(StorageKey Key, StorageItem Value, ECPoint PublicKey, CandidateState State)> GetCandidatesInternal(DataCache snapshot)
        {
            byte[] prefix_key = CreateStorageKey(Prefix_Candidate).ToArray();
            return snapshot.Find(prefix_key)
                .Select(p => (p.Key, p.Value, PublicKey: p.Key.Key[1..].AsSerializable<ECPoint>(), State: p.Value.GetInteroperable<CandidateState>()))
                .Where(p => p.State.Registered)
                .Where(p => !Policy.IsBlocked(snapshot, Contract.CreateSignatureRedeemScript(p.PublicKey).ToScriptHash()));
        }

        /// <summary>
        /// Gets votes from specific candidate.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="pubKey">Specific public key</param>
        /// <returns>Votes or -1 if it was not found.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public BigInteger GetCandidateVote(DataCache snapshot, ECPoint pubKey)
        {
            StorageItem storage = snapshot.TryGet(CreateStorageKey(Prefix_Candidate).Add(pubKey));
            CandidateState state = storage?.GetInteroperable<CandidateState>();
            return state?.Registered == true ? state.Votes : -1;
        }

        /// <summary>
        /// Gets all the members of the committee.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The public keys of the members.</returns>
        [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.ReadStates)]
        public ECPoint[] GetCommittee(DataCache snapshot)
        {
            return GetCommitteeFromCache(snapshot).Select(p => p.PublicKey).OrderBy(p => p).ToArray();
        }

        /// <summary>
        /// Get account state.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="account">account</param>
        /// <returns>The state of the account.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public NeoAccountState GetAccountState(DataCache snapshot, UInt160 account)
        {
            return snapshot.TryGet(CreateStorageKey(Prefix_Account).Add(account))?.GetInteroperable<NeoAccountState>();
        }

        /// <summary>
        /// Gets the address of the committee.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The address of the committee.</returns>
        public UInt160 GetCommitteeAddress(DataCache snapshot)
        {
            ECPoint[] committees = GetCommittee(snapshot);
            return Contract.CreateMultiSigRedeemScript(committees.Length - (committees.Length - 1) / 2, committees).ToScriptHash();
        }

        private CachedCommittee GetCommitteeFromCache(DataCache snapshot)
        {
            return snapshot[CreateStorageKey(Prefix_Committee)].GetInteroperable<CachedCommittee>();
        }

        /// <summary>
        /// Computes the validators of the next block.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="settings">The <see cref="ProtocolSettings"/> used during computing.</param>
        /// <returns>The public keys of the validators.</returns>
        public ECPoint[] ComputeNextBlockValidators(DataCache snapshot, ProtocolSettings settings)
        {
            return ComputeCommitteeMembers(snapshot, settings).Select(p => p.PublicKey).Take(settings.ValidatorsCount).OrderBy(p => p).ToArray();
        }

        private IEnumerable<(ECPoint PublicKey, BigInteger Votes)> ComputeCommitteeMembers(DataCache snapshot, ProtocolSettings settings)
        {
            decimal votersCount = (decimal)(BigInteger)snapshot[CreateStorageKey(Prefix_VotersCount)];
            decimal voterTurnout = votersCount / (decimal)TotalAmount;
            var candidates = GetCandidatesInternal(snapshot)
                .Select(p => (p.PublicKey, p.State.Votes))
                .ToArray();
            if (voterTurnout < EffectiveVoterTurnout || candidates.Length < settings.CommitteeMembersCount)
                return settings.StandbyCommittee.Select(p => (p, candidates.FirstOrDefault(k => k.PublicKey.Equals(p)).Votes));
            return candidates
                .OrderByDescending(p => p.Votes)
                .ThenBy(p => p.PublicKey)
                .Take(settings.CommitteeMembersCount);
        }

        [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.ReadStates)]
        private ECPoint[] GetNextBlockValidators(ApplicationEngine engine)
        {
            return GetNextBlockValidators(engine.Snapshot, engine.ProtocolSettings.ValidatorsCount);
        }

        /// <summary>
        /// Gets the validators of the next block.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="validatorsCount">The number of validators in the system.</param>
        /// <returns>The public keys of the validators.</returns>
        public ECPoint[] GetNextBlockValidators(DataCache snapshot, int validatorsCount)
        {
            return GetCommitteeFromCache(snapshot)
                .Take(validatorsCount)
                .Select(p => p.PublicKey)
                .OrderBy(p => p)
                .ToArray();
        }

        /// <summary>
        /// Represents the account state of <see cref="NeoToken"/>.
        /// </summary>
        public class NeoAccountState : AccountState
        {
            /// <summary>
            /// The height of the block where the balance changed last time.
            /// </summary>
            public uint BalanceHeight;

            /// <summary>
            /// The voting target of the account. This field can be <see langword="null"/>.
            /// </summary>
            public ECPoint VoteTo;

            public BigInteger LastGasPerVote;

            public override void FromStackItem(StackItem stackItem)
            {
                base.FromStackItem(stackItem);
                Struct @struct = (Struct)stackItem;
                BalanceHeight = (uint)@struct[1].GetInteger();
                VoteTo = @struct[2].IsNull ? null : ECPoint.DecodePoint(@struct[2].GetSpan(), ECCurve.Secp256r1);
                LastGasPerVote = @struct[3].GetInteger();
            }

            public override StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                Struct @struct = (Struct)base.ToStackItem(referenceCounter);
                @struct.Add(BalanceHeight);
                @struct.Add(VoteTo?.ToArray() ?? StackItem.Null);
                @struct.Add(LastGasPerVote);
                return @struct;
            }
        }

        internal class CandidateState : IInteroperable
        {
            public bool Registered;
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

        internal class CachedCommittee : InteroperableList<(ECPoint PublicKey, BigInteger Votes)>
        {
            public CachedCommittee() { }
            public CachedCommittee(IEnumerable<(ECPoint, BigInteger)> collection) => AddRange(collection);

            protected override (ECPoint, BigInteger) ElementFromStackItem(StackItem item)
            {
                Struct @struct = (Struct)item;
                return (ECPoint.DecodePoint(@struct[0].GetSpan(), ECCurve.Secp256r1), @struct[1].GetInteger());
            }

            protected override StackItem ElementToStackItem((ECPoint PublicKey, BigInteger Votes) element, ReferenceCounter referenceCounter)
            {
                return new Struct(referenceCounter) { element.PublicKey.ToArray(), element.Votes };
            }
        }

        private record GasDistribution
        {
            public UInt160 Account { get; init; }
            public BigInteger Amount { get; init; }
        }
    }
}
