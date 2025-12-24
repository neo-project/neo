// Copyright (C) 2015-2025 The Neo Project.
//
// NeoToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Extensions.IO;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System.Buffers.Binary;
using System.Numerics;
using Array = System.Array;

namespace Neo.SmartContract.Native;

/// <summary>
/// Represents the NEO token in the NEO system.
/// </summary>
[ContractEvent(1, name: "CandidateStateChanged",
   "pubkey", ContractParameterType.PublicKey,
   "registered", ContractParameterType.Boolean,
   "votes", ContractParameterType.Integer)]
[ContractEvent(2, name: "Vote",
   "account", ContractParameterType.Hash160,
   "from", ContractParameterType.PublicKey,
   "to", ContractParameterType.PublicKey,
   "amount", ContractParameterType.Integer)]
[ContractEvent(3, name: "CommitteeChanged",
   "old", ContractParameterType.Array,
   "new", ContractParameterType.Array)]
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
    private const long VoteFactor = 100000000L;

    private const byte Prefix_VotersCount = 1;
    private const byte Prefix_Candidate = 33;
    private const byte Prefix_Committee = 14;
    private const byte Prefix_GasPerBlock = 29;
    private const byte Prefix_RegisterPrice = 13;
    private const byte Prefix_VoterRewardPerCommittee = 23;

    private const byte NeoHolderRewardRatio = 10;
    private const byte CommitteeRewardRatio = 10;
    private const byte VoterRewardRatio = 80;

    private readonly StorageKey _votersCount;
    private readonly StorageKey _registerPrice;

    internal NeoToken() : base(-14)
    {
        TotalAmount = 100000000 * Factor;
        _votersCount = CreateStorageKey(Prefix_VotersCount);
        _registerPrice = CreateStorageKey(Prefix_RegisterPrice);
    }

    public override BigInteger TotalSupply(IReadOnlyStore snapshot)
    {
        return TotalAmount;
    }

    internal override void OnBalanceChanging(ApplicationEngine engine, UInt160 account, NeoAccountState state, BigInteger amount)
    {
        GasDistribution? distribution = DistributeGas(engine, account, state);
        if (distribution is not null)
        {
            var list = engine.CurrentContext!.GetState<List<GasDistribution>>();
            list.Add(distribution);
        }
        if (amount.IsZero) return;
        if (state.VoteTo is null) return;
        engine.SnapshotCache.GetAndChange(_votersCount)!.Add(amount);
        StorageKey key = CreateStorageKey(Prefix_Candidate, state.VoteTo);
        CandidateState candidate = engine.SnapshotCache.GetAndChange(key)!.GetInteroperable<CandidateState>();
        candidate.Votes += amount;
        CheckCandidate(engine.SnapshotCache, state.VoteTo, candidate);
    }

    private protected override async ContractTask PostTransferAsync(ApplicationEngine engine, UInt160? from, UInt160? to, BigInteger amount, StackItem data, bool callOnPayment)
    {
        await base.PostTransferAsync(engine, from, to, amount, data, callOnPayment);
        var list = engine.CurrentContext!.GetState<List<GasDistribution>>();
        foreach (var distribution in list)
            await TokenManagement.MintInternal(engine, Governance.GasTokenId, distribution.Account, distribution.Amount, assertOwner: false, callOnPayment: callOnPayment);
    }

    protected override void OnManifestCompose(IsHardforkEnabledDelegate hfChecker, uint blockHeight, ContractManifest manifest)
    {
        manifest.SupportedStandards = ["NEP-17", "NEP-27"];
    }

    private GasDistribution? DistributeGas(ApplicationEngine engine, UInt160 account, NeoAccountState state)
    {
        // PersistingBlock is null when running under the debugger
        if (engine.PersistingBlock is null) return null;

        // In the unit of datoshi, 1 datoshi = 1e-8 GAS
        BigInteger datoshi = CalculateBonus(engine.SnapshotCache, state, engine.PersistingBlock.Index);
        state.BalanceHeight = engine.PersistingBlock.Index;
        if (state.VoteTo is not null)
        {
            var keyLastest = CreateStorageKey(Prefix_VoterRewardPerCommittee, state.VoteTo);
            var latestGasPerVote = engine.SnapshotCache.TryGet(keyLastest) ?? BigInteger.Zero;
            state.LastGasPerVote = latestGasPerVote;
        }
        if (datoshi == 0) return null;
        return new GasDistribution
        {
            Account = account,
            Amount = datoshi
        };
    }

    private BigInteger CalculateBonus(DataCache snapshot, NeoAccountState state, uint end)
    {
        if (state.Balance.IsZero) return BigInteger.Zero;
        if (state.Balance.Sign < 0) throw new ArgumentOutOfRangeException(nameof(state), "Balance cannot be negative");

        var expectEnd = Ledger.CurrentIndex(snapshot) + 1;
        ArgumentOutOfRangeException.ThrowIfNotEqual(end, expectEnd);
        if (state.BalanceHeight >= end) return BigInteger.Zero;
        // In the unit of datoshi, 1 datoshi = 1e-8 GAS
        (var neoHolderReward, var voteReward) = CalculateReward(snapshot, state, end);

        return neoHolderReward + voteReward;
    }

    private (BigInteger neoHold, BigInteger voteReward) CalculateReward(DataCache snapshot, NeoAccountState state, uint end)
    {
        var start = state.BalanceHeight;

        // Compute Neo holder reward

        // In the unit of datoshi, 1 GAS = 10^8 datoshi
        BigInteger sumGasPerBlock = 0;
        foreach (var (index, gasPerBlock) in GetSortedGasRecords(snapshot, end - 1))
        {
            if (index > start)
            {
                sumGasPerBlock += gasPerBlock * (end - index);
                end = index;
            }
            else
            {
                sumGasPerBlock += gasPerBlock * (end - start);
                break;
            }
        }

        // Compute vote reward

        var voteReward = BigInteger.Zero;

        if (state.VoteTo != null)
        {
            var keyLastest = CreateStorageKey(Prefix_VoterRewardPerCommittee, state.VoteTo);
            var latestGasPerVote = snapshot.TryGet(keyLastest) ?? BigInteger.Zero;
            voteReward = state.Balance * (latestGasPerVote - state.LastGasPerVote) / VoteFactor;
        }

        return (state.Balance * sumGasPerBlock * NeoHolderRewardRatio / 100 / TotalAmount, voteReward);
    }

    private void CheckCandidate(DataCache snapshot, ECPoint pubkey, CandidateState candidate)
    {
        if (!candidate.Registered && candidate.Votes.IsZero)
        {
            snapshot.Delete(CreateStorageKey(Prefix_VoterRewardPerCommittee, pubkey));
            snapshot.Delete(CreateStorageKey(Prefix_Candidate, pubkey));
        }
    }

    /// <summary>
    /// Determine whether the votes should be recounted at the specified height.
    /// </summary>
    /// <param name="height">The height to be checked.</param>
    /// <param name="committeeMembersCount">The number of committee members in the system.</param>
    /// <returns><see langword="true"/> if the votes should be recounted; otherwise, <see langword="false"/>.</returns>
    public static bool ShouldRefreshCommittee(uint height, int committeeMembersCount) => height % committeeMembersCount == 0;

    internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
    {
        if (hardfork == ActiveIn)
        {
            var cachedCommittee = new CachedCommittee(engine.ProtocolSettings.StandbyCommittee.Select(p => (p, BigInteger.Zero)));
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_Committee), new StorageItem(cachedCommittee));
            engine.SnapshotCache.Add(_votersCount, new StorageItem(Array.Empty<byte>()));
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_GasPerBlock, 0u), new StorageItem(5 * Governance.GasTokenFactor));
            engine.SnapshotCache.Add(_registerPrice, new StorageItem(1000 * Governance.GasTokenFactor));
            return Mint(engine, Contract.GetBFTAddress(engine.ProtocolSettings.StandbyValidators), TotalAmount, false);
        }
        return ContractTask.CompletedTask;
    }

    internal override ContractTask OnPersistAsync(ApplicationEngine engine)
    {
        // Set next committee
        if (ShouldRefreshCommittee(engine.PersistingBlock!.Index, engine.ProtocolSettings.CommitteeMembersCount))
        {
            var storageItem = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Committee))!;
            var cachedCommittee = storageItem.GetInteroperable<CachedCommittee>();

            var prevCommittee = cachedCommittee.Select(u => u.PublicKey).ToArray();

            cachedCommittee.Clear();
            cachedCommittee.AddRange(ComputeCommitteeMembers(engine.SnapshotCache, engine.ProtocolSettings));

            var newCommittee = cachedCommittee.Select(u => u.PublicKey).ToArray();

            if (!newCommittee.SequenceEqual(prevCommittee))
            {
                Notify(engine, "CommitteeChanged", prevCommittee, newCommittee);
            }
        }
        return ContractTask.CompletedTask;
    }

    internal override async ContractTask PostPersistAsync(ApplicationEngine engine)
    {
        // Distribute GAS for committee

        int m = engine.ProtocolSettings.CommitteeMembersCount;
        int n = engine.ProtocolSettings.ValidatorsCount;
        int index = (int)(engine.PersistingBlock!.Index % (uint)m);
        var gasPerBlock = GetGasPerBlock(engine.SnapshotCache);
        var committee = GetCommitteeFromCache(engine.SnapshotCache);
        var pubkey = committee[index].PublicKey;
        var account = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
        await TokenManagement.MintInternal(engine, Governance.GasTokenId, account, gasPerBlock * CommitteeRewardRatio / 100, assertOwner: false, callOnPayment: false);

        // Record the cumulative reward of the voters of committee

        if (ShouldRefreshCommittee(engine.PersistingBlock.Index, m))
        {
            BigInteger voterRewardOfEachCommittee = gasPerBlock * VoterRewardRatio * VoteFactor * m / (m + n) / 100; // Zoom in VoteFactor times, and the final calculation should be divided VoteFactor
            for (index = 0; index < committee.Count; index++)
            {
                var (publicKey, votes) = committee[index];
                var factor = index < n ? 2 : 1; // The `voter` rewards of validator will double than other committee's
                if (votes > 0)
                {
                    BigInteger voterSumRewardPerNEO = factor * voterRewardOfEachCommittee / votes;
                    StorageKey voterRewardKey = CreateStorageKey(Prefix_VoterRewardPerCommittee, publicKey);
                    StorageItem lastRewardPerNeo = engine.SnapshotCache.GetAndChange(voterRewardKey, () => new StorageItem(BigInteger.Zero));
                    lastRewardPerNeo.Add(voterSumRewardPerNEO);
                }
            }
        }
    }

    /// <summary>
    /// Sets the amount of GAS generated in each block. Only committee members can call this method.
    /// </summary>
    /// <param name="engine">The engine used to check committee witness and read data.</param>
    /// <param name="gasPerBlock">The amount of GAS generated in each block.</param>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void SetGasPerBlock(ApplicationEngine engine, BigInteger gasPerBlock)
    {
        if (gasPerBlock < 0 || gasPerBlock > 10 * Governance.GasTokenFactor)
            throw new ArgumentOutOfRangeException(nameof(gasPerBlock), $"GasPerBlock must be between [0, {10 * Governance.GasTokenFactor}]");
        AssertCommittee(engine);

        var index = engine.PersistingBlock!.Index + 1;
        var entry = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_GasPerBlock, index), () => new StorageItem(gasPerBlock));
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

    /// <summary>
    /// Sets the fees to be paid to register as a candidate. Only committee members can call this method.
    /// </summary>
    /// <param name="engine">The engine used to check committee witness and read data.</param>
    /// <param name="registerPrice">The fees to be paid to register as a candidate.</param>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void SetRegisterPrice(ApplicationEngine engine, long registerPrice)
    {
        if (registerPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(registerPrice), "RegisterPrice must be positive");
        AssertCommittee(engine);

        engine.SnapshotCache.GetAndChange(_registerPrice)!.Set(registerPrice);
    }

    /// <summary>
    /// Gets the fees to be paid to register as a candidate.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>The amount of the fees.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public long GetRegisterPrice(IReadOnlyStore snapshot)
    {
        // In the unit of datoshi, 1 datoshi = 1e-8 GAS
        return (long)(BigInteger)snapshot[_registerPrice];
    }

    private IEnumerable<(uint Index, BigInteger GasPerBlock)> GetSortedGasRecords(DataCache snapshot, uint end)
    {
        var key = CreateStorageKey(Prefix_GasPerBlock, end).ToArray();
        var boundary = CreateStorageKey(Prefix_GasPerBlock).ToArray();
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
        StorageItem? storage = snapshot.TryGet(CreateStorageKey(Prefix_Account, account));
        if (storage is null) return BigInteger.Zero;
        NeoAccountState state = storage.GetInteroperable<NeoAccountState>();
        return CalculateBonus(snapshot, state, end);
    }

    /// <summary>
    /// Handles the payment of GAS.
    /// </summary>
    /// <param name="engine">The engine used to check witness and read data.</param>
    /// <param name="assetId">The asset being paid.</param>
    /// <param name="from">The account that is paying the GAS.</param>
    /// <param name="amount">The amount of GAS being paid.</param>
    /// <param name="data">The data of the payment.</param>
    [ContractMethod(RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    private async ContractTask _OnPayment(ApplicationEngine engine, UInt160 assetId, UInt160 from, BigInteger amount, StackItem data)
    {
        if (assetId != Governance.GasTokenId)
            throw new InvalidOperationException("Only GAS is acceptable.");

        if ((long)amount != GetRegisterPrice(engine.SnapshotCache))
            throw new ArgumentException($"Incorrect GAS amount. Expected {GetRegisterPrice(engine.SnapshotCache)} GAS, but received {amount} GAS.");

        var pubkey = ECPoint.DecodePoint(data.GetSpan(), ECCurve.Secp256r1);

        if (!RegisterInternal(engine, pubkey))
            throw new InvalidOperationException("Failed to register candidate");

        await TokenManagement.BurnInternal(engine, Governance.GasTokenId, Hash, amount, assertOwner: false);
    }

    /// <summary>
    /// Registers a candidate.
    /// </summary>
    /// <param name="engine">The engine used to check witness and read data.</param>
    /// <param name="pubkey">The public key of the candidate.</param>
    /// <returns><see langword="true"/> if the candidate is registered; otherwise, <see langword="false"/>.</returns>
    [ContractMethod(RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    private bool RegisterCandidate(ApplicationEngine engine, ECPoint pubkey)
    {
        // In the unit of datoshi, 1 datoshi = 1e-8 GAS
        engine.AddFee(GetRegisterPrice(engine.SnapshotCache));
        return RegisterInternal(engine, pubkey);
    }

    private bool RegisterInternal(ApplicationEngine engine, ECPoint pubkey)
    {
        if (!engine.CheckWitnessInternal(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash()))
            return false;
        StorageKey key = CreateStorageKey(Prefix_Candidate, pubkey);
        StorageItem item = engine.SnapshotCache.GetAndChange(key, () => new StorageItem(new CandidateState()));
        CandidateState state = item.GetInteroperable<CandidateState>();
        if (state.Registered) return true;
        state.Registered = true;
        Notify(engine, "CandidateStateChanged", pubkey, true, state.Votes);
        return true;
    }

    /// <summary>
    /// Unregisters a candidate.
    /// </summary>
    /// <param name="engine">The engine used to check witness and read data.</param>
    /// <param name="pubkey">The public key of the candidate.</param>
    /// <returns><see langword="true"/> if the candidate is unregistered; otherwise, <see langword="false"/>.</returns>
    [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    private bool UnregisterCandidate(ApplicationEngine engine, ECPoint pubkey)
    {
        if (!engine.CheckWitnessInternal(Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash()))
            return false;
        StorageKey key = CreateStorageKey(Prefix_Candidate, pubkey);
        if (engine.SnapshotCache.TryGet(key) is null) return true;
        StorageItem item = engine.SnapshotCache.GetAndChange(key)!;
        CandidateState state = item.GetInteroperable<CandidateState>();
        if (!state.Registered) return true;
        state.Registered = false;
        CheckCandidate(engine.SnapshotCache, pubkey, state);
        Notify(engine, "CandidateStateChanged", pubkey, false, state.Votes);
        return true;
    }

    /// <summary>
    /// Votes for a candidate.
    /// </summary>
    /// <param name="engine">The engine used to check witness and read data.</param>
    /// <param name="account">The account that is voting.</param>
    /// <param name="voteTo">The candidate to vote for.</param>
    /// <returns><see langword="true"/> if the vote is successful; otherwise, <see langword="false"/>.</returns>
    [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    private async ContractTask<bool> Vote(ApplicationEngine engine, UInt160 account, ECPoint? voteTo)
    {
        if (!engine.CheckWitnessInternal(account)) return false;
        return await VoteInternal(engine, account, voteTo);
    }

    internal async ContractTask<bool> VoteInternal(ApplicationEngine engine, UInt160 account, ECPoint? voteTo)
    {
        NeoAccountState? stateAccount = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Account, account))?.GetInteroperable<NeoAccountState>();
        if (stateAccount is null) return false;
        if (stateAccount.Balance == 0) return false;

        CandidateState? validatorNew = null;
        if (voteTo != null)
        {
            validatorNew = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Candidate, voteTo))?.GetInteroperable<CandidateState>();
            if (validatorNew is null) return false;
            if (!validatorNew.Registered) return false;
        }
        if (stateAccount.VoteTo is null ^ voteTo is null)
        {
            StorageItem item = engine.SnapshotCache.GetAndChange(_votersCount)!;
            if (stateAccount.VoteTo is null)
                item.Add(stateAccount.Balance);
            else
                item.Add(-stateAccount.Balance);
        }
        GasDistribution? gasDistribution = DistributeGas(engine, account, stateAccount);
        if (stateAccount.VoteTo != null)
        {
            StorageKey key = CreateStorageKey(Prefix_Candidate, stateAccount.VoteTo);
            StorageItem storageValidator = engine.SnapshotCache.GetAndChange(key)!;
            CandidateState stateValidator = storageValidator.GetInteroperable<CandidateState>();
            stateValidator.Votes -= stateAccount.Balance;
            CheckCandidate(engine.SnapshotCache, stateAccount.VoteTo, stateValidator);
        }
        if (voteTo != null && voteTo != stateAccount.VoteTo)
        {
            StorageKey voterRewardKey = CreateStorageKey(Prefix_VoterRewardPerCommittee, voteTo);
            var latestGasPerVote = engine.SnapshotCache.TryGet(voterRewardKey) ?? BigInteger.Zero;
            stateAccount.LastGasPerVote = latestGasPerVote;
        }
        ECPoint? from = stateAccount.VoteTo;
        stateAccount.VoteTo = voteTo;

        if (validatorNew != null)
        {
            validatorNew.Votes += stateAccount.Balance;
        }
        else
        {
            stateAccount.LastGasPerVote = 0;
        }
        Notify(engine, "Vote", account, from, voteTo, stateAccount.Balance);
        if (gasDistribution is not null)
            await TokenManagement.MintInternal(engine, Governance.GasTokenId, gasDistribution.Account, gasDistribution.Amount, assertOwner: false, callOnPayment: true);
        return true;
    }

    /// <summary>
    /// Gets the first 256 registered candidates.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>All the registered candidates.</returns>
    [ContractMethod(CpuFee = 1 << 22, RequiredCallFlags = CallFlags.ReadStates)]
    internal (ECPoint PublicKey, BigInteger Votes)[] GetCandidates(DataCache snapshot)
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
    private StorageIterator GetAllCandidates(IReadOnlyStore snapshot)
    {
        const FindOptions options = FindOptions.RemovePrefix | FindOptions.DeserializeValues | FindOptions.PickField1;
        var enumerator = GetCandidatesInternal(snapshot)
            .Select(p => (p.Key, p.Value))
            .GetEnumerator();
        return new StorageIterator(enumerator, 1, options);
    }

    internal IEnumerable<(StorageKey Key, StorageItem Value, ECPoint PublicKey, CandidateState State)> GetCandidatesInternal(IReadOnlyStore snapshot)
    {
        var prefixKey = CreateStorageKey(Prefix_Candidate);
        return snapshot.Find(prefixKey)
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
    public BigInteger GetCandidateVote(IReadOnlyStore snapshot, ECPoint pubKey)
    {
        var key = CreateStorageKey(Prefix_Candidate, pubKey);
        var state = snapshot.TryGet(key, out var item) ? item.GetInteroperable<CandidateState>() : null;
        return state?.Registered == true ? state.Votes : -1;
    }

    /// <summary>
    /// Gets all the members of the committee.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>The public keys of the members.</returns>
    [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.ReadStates)]
    public ECPoint[] GetCommittee(IReadOnlyStore snapshot)
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
    public NeoAccountState? GetAccountState(IReadOnlyStore snapshot, UInt160 account)
    {
        var key = CreateStorageKey(Prefix_Account, account);
        return snapshot.TryGet(key, out var item) ? item.GetInteroperableClone<NeoAccountState>() : null;
    }

    /// <summary>
    /// Gets the address of the committee.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>The address of the committee.</returns>
    [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.ReadStates)]
    public UInt160 GetCommitteeAddress(IReadOnlyStore snapshot)
    {
        ECPoint[] committees = GetCommittee(snapshot);
        return Contract.CreateMultiSigRedeemScript(committees.Length - (committees.Length - 1) / 2, committees).ToScriptHash();
    }

    private CachedCommittee GetCommitteeFromCache(IReadOnlyStore snapshot)
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
        decimal votersCount = (decimal)(BigInteger)snapshot[_votersCount];
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

    /// <summary>
    /// Gets the validators of the next block.
    /// </summary>
    /// <param name="engine">The engine used to read data.</param>
    /// <returns>The public keys of the validators.</returns>
    [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.ReadStates)]
    private ECPoint[] GetNextBlockValidators(ApplicationEngine engine)
    {
        return GetNextBlockValidators(engine.SnapshotCache, engine.ProtocolSettings.ValidatorsCount);
    }

    /// <summary>
    /// Gets the validators of the next block.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="validatorsCount">The number of validators in the system.</param>
    /// <returns>The public keys of the validators.</returns>
    public ECPoint[] GetNextBlockValidators(IReadOnlyStore snapshot, int validatorsCount)
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
        public ECPoint? VoteTo;

        public BigInteger LastGasPerVote;

        public override void FromStackItem(StackItem stackItem)
        {
            base.FromStackItem(stackItem);
            Struct @struct = (Struct)stackItem;
            BalanceHeight = (uint)@struct[1].GetInteger();
            VoteTo = @struct[2].IsNull ? null : ECPoint.DecodePoint(@struct[2].GetSpan(), ECCurve.Secp256r1);
            LastGasPerVote = @struct[3].GetInteger();
        }

        public override StackItem ToStackItem(IReferenceCounter? referenceCounter)
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

        public StackItem ToStackItem(IReferenceCounter? referenceCounter)
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

        protected override StackItem ElementToStackItem((ECPoint PublicKey, BigInteger Votes) element, IReferenceCounter? referenceCounter)
        {
            return new Struct(referenceCounter) { element.PublicKey.ToArray(), element.Votes };
        }
    }

    private record GasDistribution
    {
        public required UInt160 Account { get; init; }
        public BigInteger Amount { get; init; }
    }
}
