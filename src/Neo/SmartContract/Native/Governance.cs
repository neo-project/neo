// Copyright (C) 2015-2026 The Neo Project.
//
// Governance.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Extensions.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.VM;
using Neo.VM.Types;
using System.Buffers.Binary;
using System.Numerics;

namespace Neo.SmartContract.Native;

[ContractEvent(0, name: "CandidateStateChanged", "pubkey", ContractParameterType.PublicKey, "registered", ContractParameterType.Boolean, "votes", ContractParameterType.Integer)]
[ContractEvent(1, name: "Vote", "account", ContractParameterType.Hash160, "from", ContractParameterType.PublicKey, "to", ContractParameterType.PublicKey, "amount", ContractParameterType.Integer)]
[ContractEvent(2, name: "CommitteeChanged", "old", ContractParameterType.Array, "new", ContractParameterType.Array)]
public sealed class Governance : NativeContract
{
    public const string NeoTokenName = "NeoToken";
    public const string NeoTokenSymbol = "NEO";
    public const byte NeoTokenDecimals = 0;
    public static readonly BigInteger NeoTokenFactor = BigInteger.Pow(10, NeoTokenDecimals);
    public static readonly BigInteger NeoTokenTotalAmount = 100000000 * NeoTokenFactor;

    public const string GasTokenName = "GasToken";
    public const string GasTokenSymbol = "GAS";
    public const byte GasTokenDecimals = 8;
    public static readonly BigInteger GasTokenFactor = BigInteger.Pow(10, GasTokenDecimals);

    public UInt160 NeoTokenId => field ??= TokenManagement.GetAssetId(Hash, NeoTokenName);
    public UInt160 GasTokenId => field ??= TokenManagement.GetAssetId(Hash, GasTokenName);

    public const decimal EffectiveVoterTurnout = 0.2M;
    private const long VoteFactor = 100000000L;

    private const byte Prefix_NeoAccount = 10;
    private const byte Prefix_VotersCount = 1;
    private const byte Prefix_Candidate = 33;
    private const byte Prefix_Committee = 14;
    private const byte Prefix_GasPerBlock = 29;
    private const byte Prefix_RegisterPrice = 13;
    private const byte Prefix_VoterRewardPerCommittee = 23;

    private const byte NeoHolderRewardRatio = 10;
    private const byte CommitteeRewardRatio = 10;
    private const byte VoterRewardRatio = 80;

    internal Governance() : base(-13) { }

    internal override async ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardFork)
    {
        if (hardFork == ActiveIn)
        {
            UInt160 initialAccount = Contract.GetBFTAddress(engine.ProtocolSettings.StandbyValidators);

            UInt160 neoTokenId = TokenManagement.CreateInternal(engine, Hash, NeoTokenName, NeoTokenSymbol, NeoTokenDecimals, NeoTokenTotalAmount);
            await TokenManagement.MintInternal(engine, neoTokenId, initialAccount, NeoTokenTotalAmount, assertOwner: false, callOnBalanceChanged: false, callOnPayment: false, callOnTransfer: false);
            _OnBalanceChanged(engine, neoTokenId, initialAccount, NeoTokenTotalAmount, BigInteger.Zero, NeoTokenTotalAmount);

            UInt160 gasTokenId = TokenManagement.CreateInternal(engine, Hash, GasTokenName, GasTokenSymbol, GasTokenDecimals, BigInteger.MinusOne);
            await TokenManagement.MintInternal(engine, gasTokenId, initialAccount, engine.ProtocolSettings.InitialGasDistribution, assertOwner: false, callOnBalanceChanged: false, callOnPayment: false, callOnTransfer: false);

            engine.SnapshotCache.Add(CreateStorageKey(Prefix_Committee), new(new CachedCommittee(engine.ProtocolSettings.StandbyCommittee)));
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_VotersCount), new());
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_GasPerBlock, 0u), new(5 * GasTokenFactor));
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_RegisterPrice), new(1000 * GasTokenFactor));
        }
    }

    internal override async ContractTask OnPersistAsync(ApplicationEngine engine)
    {
        if (ShouldRefreshCommittee(engine.PersistingBlock!.Index, engine.ProtocolSettings.CommitteeMembersCount))
        {
            CachedCommittee cachedCommittee = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Committee))!.GetInteroperable<CachedCommittee>();
            ECPoint[] prevCommittee = cachedCommittee.Select(u => u.PublicKey).ToArray();
            cachedCommittee.Clear();
            cachedCommittee.AddRange(ComputeCommitteeMembers(engine.SnapshotCache, engine.ProtocolSettings));
            ECPoint[] newCommittee = cachedCommittee.Select(u => u.PublicKey).ToArray();
            if (!newCommittee.SequenceEqual(prevCommittee))
            {
                Notify(engine, "CommitteeChanged", prevCommittee, newCommittee);
            }
        }
        long totalNetworkFee = 0;
        foreach (Transaction tx in engine.PersistingBlock!.Transactions)
        {
            await TokenManagement.BurnInternal(engine, GasTokenId, tx.Sender, tx.SystemFee + tx.NetworkFee, assertOwner: false, callOnBalanceChanged: false, callOnTransfer: false);
            totalNetworkFee += tx.NetworkFee;

            // Reward for NotaryAssisted attribute will be minted to designated notary nodes
            // by Notary contract.
            var notaryAssisted = tx.GetAttribute<NotaryAssisted>();
            if (notaryAssisted is not null)
            {
                totalNetworkFee -= (notaryAssisted.NKeys + 1) * Policy.GetAttributeFee(engine.SnapshotCache, (byte)notaryAssisted.Type);
            }
        }
        ECPoint[] validators = GetNextBlockValidators(engine.SnapshotCache, engine.ProtocolSettings.ValidatorsCount);
        UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
        await TokenManagement.MintInternal(engine, GasTokenId, primary, totalNetworkFee, assertOwner: false, callOnBalanceChanged: false, callOnPayment: false, callOnTransfer: false);
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
        await TokenManagement.MintInternal(engine, Governance.GasTokenId, account, gasPerBlock * CommitteeRewardRatio / 100, assertOwner: false, callOnBalanceChanged: false, callOnPayment: false, callOnTransfer: false);

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
    void SetGasPerBlock(ApplicationEngine engine, BigInteger gasPerBlock)
    {
        if (gasPerBlock < 0 || gasPerBlock > 10 * GasTokenFactor)
            throw new ArgumentOutOfRangeException(nameof(gasPerBlock), $"GasPerBlock must be between [0, {10 * GasTokenFactor}]");
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
    public BigInteger GetGasPerBlock(IReadOnlyStore snapshot)
    {
        return GetSortedGasRecords(snapshot, Ledger.CurrentIndex(snapshot) + 1).First().GasPerBlock;
    }

    /// <summary>
    /// Sets the fees to be paid to register as a candidate. Only committee members can call this method.
    /// </summary>
    /// <param name="engine">The engine used to check committee witness and read data.</param>
    /// <param name="registerPrice">The fees to be paid to register as a candidate.</param>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    void SetRegisterPrice(ApplicationEngine engine, long registerPrice)
    {
        if (registerPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(registerPrice), "RegisterPrice must be positive");
        AssertCommittee(engine);
        engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_RegisterPrice))!.Set(registerPrice);
    }

    /// <summary>
    /// Gets the fees to be paid to register as a candidate.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>The amount of the fees.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public long GetRegisterPrice(IReadOnlyStore snapshot)
    {
        return (long)(BigInteger)snapshot[CreateStorageKey(Prefix_RegisterPrice)];
    }

    /// <summary>
    /// Get the amount of unclaimed GAS in the specified account.
    /// </summary>
    /// <param name="engine">The engine used to read data.</param>
    /// <param name="account">The account to check.</param>
    /// <param name="end">The block index used when calculating GAS.</param>
    /// <returns>The amount of unclaimed GAS.</returns>
    [ContractMethod(CpuFee = 1 << 17, RequiredCallFlags = CallFlags.ReadStates)]
    public BigInteger UnclaimedGas(ApplicationEngine engine, UInt160 account, uint end)
    {
        var expectEnd = Ledger.CurrentIndex(engine.SnapshotCache) + 1;
        ArgumentOutOfRangeException.ThrowIfNotEqual(end, expectEnd);
        BigInteger balance = TokenManagement.BalanceOf(engine.SnapshotCache, NeoTokenId, account);
        if (balance.IsZero) return BigInteger.Zero;
        StorageKey accountKey = CreateStorageKey(Prefix_NeoAccount, account);
        NeoAccountState state = engine.SnapshotCache[accountKey].GetInteroperable<NeoAccountState>();
        return CalculateBonus(engine.SnapshotCache, state, balance, end);
    }

    /// <summary>
    /// Registers a candidate.
    /// </summary>
    /// <param name="engine">The engine used to check witness and read data.</param>
    /// <param name="pubkey">The public key of the candidate.</param>
    /// <returns><see langword="true"/> if the candidate is registered; otherwise, <see langword="false"/>.</returns>
    [ContractMethod(RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    bool RegisterCandidate(ApplicationEngine engine, ECPoint pubkey)
    {
        engine.AddFee(GetRegisterPrice(engine.SnapshotCache));
        return RegisterInternal(engine, pubkey);
    }

    bool RegisterInternal(ApplicationEngine engine, ECPoint pubkey)
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
    bool UnregisterCandidate(ApplicationEngine engine, ECPoint pubkey)
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
    async ContractTask<bool> Vote(ApplicationEngine engine, UInt160 account, ECPoint? voteTo)
    {
        if (!engine.CheckWitnessInternal(account)) return false;
        return await VoteInternal(engine, account, voteTo);
    }

    internal async ContractTask<bool> VoteInternal(ApplicationEngine engine, UInt160 account, ECPoint? voteTo)
    {
        BigInteger balance = TokenManagement.BalanceOf(engine.SnapshotCache, NeoTokenId, account);
        if (balance.IsZero) return false;
        NeoAccountState stateAccount = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_NeoAccount, account))!.GetInteroperable<NeoAccountState>();
        CandidateState? validatorNew = null;
        if (voteTo != null)
        {
            validatorNew = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Candidate, voteTo))?.GetInteroperable<CandidateState>();
            if (validatorNew is null) return false;
            if (!validatorNew.Registered) return false;
        }
        if (stateAccount.VoteTo is null ^ voteTo is null)
        {
            StorageItem item = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_VotersCount))!;
            if (stateAccount.VoteTo is null)
                item.Add(balance);
            else
                item.Add(-balance);
        }
        GasDistribution? gasDistribution = DistributeGas(engine, account, stateAccount, balance);
        if (stateAccount.VoteTo != null)
        {
            StorageKey key = CreateStorageKey(Prefix_Candidate, stateAccount.VoteTo);
            StorageItem storageValidator = engine.SnapshotCache.GetAndChange(key)!;
            CandidateState stateValidator = storageValidator.GetInteroperable<CandidateState>();
            stateValidator.Votes -= balance;
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
            validatorNew.Votes += balance;
        }
        else
        {
            stateAccount.LastGasPerVote = 0;
        }
        Notify(engine, "Vote", account, from, voteTo, balance);
        if (gasDistribution is not null)
            await TokenManagement.MintInternal(engine, Governance.GasTokenId, gasDistribution.Account, gasDistribution.Amount, assertOwner: false, callOnBalanceChanged: false, callOnPayment: true, callOnTransfer: false);
        return true;
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public ECPoint? GetVoteTarget(IReadOnlyStore snapshot, UInt160 account)
    {
        StorageKey key = CreateStorageKey(Prefix_NeoAccount, account);
        return snapshot.TryGet(key)?.GetInteroperable<NeoAccountState>().VoteTo;
    }

    /// <summary>
    /// Gets the first 256 registered candidates.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>All the registered candidates.</returns>
    [ContractMethod(CpuFee = 1 << 22, RequiredCallFlags = CallFlags.ReadStates)]
    (ECPoint PublicKey, BigInteger Votes)[] GetCandidates(IReadOnlyStore snapshot)
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
    StorageIterator GetAllCandidates(IReadOnlyStore snapshot)
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
        var state = snapshot.TryGet(key)?.GetInteroperable<CandidateState>();
        return state?.Registered == true ? state.Votes : BigInteger.MinusOne;
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

    /// <summary>
    /// Gets the validators of the next block.
    /// </summary>
    /// <param name="engine">The engine used to read data.</param>
    /// <returns>The public keys of the validators.</returns>
    [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.ReadStates)]
    ECPoint[] GetNextBlockValidators(ApplicationEngine engine)
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

    [ContractMethod(CpuFee = 0, RequiredCallFlags = CallFlags.States)]
    void _OnBalanceChanged(ApplicationEngine engine, UInt160 assetId, UInt160 account, BigInteger amount, BigInteger balanceOld, BigInteger balanceNew)
    {
        if (assetId != NeoTokenId) return;
        if (amount.IsZero) return;
        StorageKey accountKey = CreateStorageKey(Prefix_NeoAccount, account);
        NeoAccountState accountState = engine.SnapshotCache.GetAndChange(accountKey, () => new(new NeoAccountState())).GetInteroperable<NeoAccountState>();
        if (balanceNew.IsZero) engine.SnapshotCache.Delete(accountKey);
        GasDistribution? distribution = DistributeGas(engine, account, accountState, balanceOld);
        if (distribution is not null)
        {
            var list = engine.CurrentContext!.GetState<ExecutionContextState>().CallingContext!.GetState<List<GasDistribution>>();
            list.Add(distribution);
        }
        if (accountState.VoteTo is null) return;
        engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_VotersCount))!.Add(amount);
        StorageKey candidateKey = CreateStorageKey(Prefix_Candidate, accountState.VoteTo);
        CandidateState candidate = engine.SnapshotCache.GetAndChange(candidateKey)!.GetInteroperable<CandidateState>();
        candidate.Votes += amount;
        CheckCandidate(engine.SnapshotCache, accountState.VoteTo, candidate);
    }

    [ContractMethod(CpuFee = 0, RequiredCallFlags = CallFlags.All)]
    async ContractTask _OnTransfer(ApplicationEngine engine, UInt160 assetId, UInt160 from, UInt160 to, BigInteger amount, StackItem data)
    {
        if (assetId != NeoTokenId) return;
        var list = engine.CurrentContext!.GetState<ExecutionContextState>().CallingContext!.GetState<List<GasDistribution>>();
        foreach (var distribution in list)
            await TokenManagement.MintInternal(engine, GasTokenId, distribution.Account, distribution.Amount, assertOwner: false, callOnBalanceChanged: false, callOnPayment: true, callOnTransfer: false);
    }

    GasDistribution? DistributeGas(ApplicationEngine engine, UInt160 account, NeoAccountState state, BigInteger balance)
    {
        BigInteger amount = CalculateBonus(engine.SnapshotCache, state, balance, engine.PersistingBlock!.Index);
        state.BalanceHeight = engine.PersistingBlock.Index;
        if (state.VoteTo is not null)
        {
            StorageKey key = CreateStorageKey(Prefix_VoterRewardPerCommittee, state.VoteTo);
            state.LastGasPerVote = engine.SnapshotCache.TryGet(key) ?? BigInteger.Zero;
        }
        if (amount == 0) return null;
        return new GasDistribution(account, amount);
    }

    BigInteger CalculateBonus(IReadOnlyStore snapshot, NeoAccountState state, BigInteger balance, uint end)
    {
        if (balance.IsZero) return BigInteger.Zero;
        if (state.BalanceHeight >= end) return BigInteger.Zero;
        var (neoHolderReward, voteReward) = CalculateReward(snapshot, state, balance, end);
        return neoHolderReward + voteReward;
    }

    (BigInteger NeoHolderReward, BigInteger VoteReward) CalculateReward(IReadOnlyStore snapshot, NeoAccountState state, BigInteger balance, uint end)
    {
        // Compute Neo holder reward
        BigInteger sumGasPerBlock = 0;
        foreach (var (index, gasPerBlock) in GetSortedGasRecords(snapshot, end - 1))
        {
            if (index > state.BalanceHeight)
            {
                sumGasPerBlock += gasPerBlock * (end - index);
                end = index;
            }
            else
            {
                sumGasPerBlock += gasPerBlock * (end - state.BalanceHeight);
                break;
            }
        }
        // Compute vote reward
        BigInteger voteReward = BigInteger.Zero;
        if (state.VoteTo != null)
        {
            var keyLastest = CreateStorageKey(Prefix_VoterRewardPerCommittee, state.VoteTo);
            var latestGasPerVote = snapshot.TryGet(keyLastest) ?? BigInteger.Zero;
            voteReward = balance * (latestGasPerVote - state.LastGasPerVote) / VoteFactor;
        }
        return (balance * sumGasPerBlock * NeoHolderRewardRatio / 100 / NeoTokenTotalAmount, voteReward);
    }

    IEnumerable<(uint Index, BigInteger GasPerBlock)> GetSortedGasRecords(IReadOnlyStore snapshot, uint end)
    {
        var key = CreateStorageKey(Prefix_GasPerBlock, end).ToArray();
        var boundary = CreateStorageKey(Prefix_GasPerBlock).ToArray();
        return snapshot.FindRange(key, boundary, SeekDirection.Backward)
            .Select(u => (BinaryPrimitives.ReadUInt32BigEndian(u.Key.Key.Span[^sizeof(uint)..]), (BigInteger)u.Value));
    }

    void CheckCandidate(DataCache snapshot, ECPoint pubkey, CandidateState candidate)
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

    /// <summary>
    /// Computes the validators of the next block.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="settings">The <see cref="ProtocolSettings"/> used during computing.</param>
    /// <returns>The public keys of the validators.</returns>
    public ECPoint[] ComputeNextBlockValidators(IReadOnlyStore snapshot, ProtocolSettings settings)
    {
        return ComputeCommitteeMembers(snapshot, settings).Select(p => p.PublicKey).Take(settings.ValidatorsCount).OrderBy(p => p).ToArray();
    }

    CachedCommittee GetCommitteeFromCache(IReadOnlyStore snapshot)
    {
        return snapshot[CreateStorageKey(Prefix_Committee)].GetInteroperable<CachedCommittee>();
    }

    IEnumerable<(ECPoint PublicKey, BigInteger Votes)> ComputeCommitteeMembers(IReadOnlyStore snapshot, ProtocolSettings settings)
    {
        decimal votersCount = (decimal)(BigInteger)snapshot[CreateStorageKey(Prefix_VotersCount)];
        decimal voterTurnout = votersCount / (decimal)NeoTokenTotalAmount;
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

    class NeoAccountState : IInteroperable
    {
        public uint BalanceHeight;
        public ECPoint? VoteTo;
        public BigInteger LastGasPerVote;

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            BalanceHeight = (uint)@struct[0].GetInteger();
            VoteTo = @struct[1].IsNull ? null : ECPoint.DecodePoint(@struct[1].GetSpan(), ECCurve.Secp256r1);
            LastGasPerVote = @struct[2].GetInteger();
        }

        StackItem IInteroperable.ToStackItem(IReferenceCounter? referenceCounter)
        {
            return new Struct(referenceCounter)
        {
            BalanceHeight,
            VoteTo?.ToArray() ?? StackItem.Null,
            LastGasPerVote
        };
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

    class CachedCommittee : InteroperableList<(ECPoint PublicKey, BigInteger Votes)>
    {
        public CachedCommittee() { }
        public CachedCommittee(IEnumerable<ECPoint> committee) => AddRange(committee.Select(p => (p, BigInteger.Zero)));
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

    record GasDistribution(UInt160 Account, BigInteger Amount);
}
