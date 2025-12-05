// Copyright (C) 2015-2025 The Neo Project.
//
// TokenManagement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.IO;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native;

/// <summary>
/// Provides core functionality for creating, managing, and transferring tokens within a native contract environment.
/// </summary>
[ContractEvent(0, "Created", "assetId", ContractParameterType.Hash160, "type", ContractParameterType.Integer)]
[ContractEvent(1, "Transfer", "assetId", ContractParameterType.Hash160, "from", ContractParameterType.Hash160, "to", ContractParameterType.Hash160, "amount", ContractParameterType.Integer)]
[ContractEvent(1, "NFTTransfer", "uniqueId", ContractParameterType.Hash160, "from", ContractParameterType.Hash160, "to", ContractParameterType.Hash160)]
public sealed class TokenManagement : NativeContract
{
    const byte Prefix_TokenState = 10;
    const byte Prefix_AccountState = 12;
    const byte Prefix_NFTUniqueIdSeed = 15;
    const byte Prefix_NFTState = 8;
    const byte Prefix_NFTOwnerUniqueIdIndex = 21;
    const byte Prefix_NFTAssetIdUniqueIdIndex = 23;

    static readonly BigInteger MaxMintAmount = BigInteger.Pow(2, 128);

    internal TokenManagement() : base(-12) { }

    internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
    {
        if (hardfork == ActiveIn)
        {
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_NFTUniqueIdSeed), BigInteger.Zero);
        }
        return ContractTask.CompletedTask;
    }

    /// <summary>
    /// Creates a new token with an unlimited maximum supply.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="name">The token name (1-32 characters).</param>
    /// <param name="symbol">The token symbol (2-6 characters).</param>
    /// <param name="decimals">The number of decimals (0-18).</param>
    /// <returns>The asset <see cref="UInt160"/> identifier generated for the new token.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If parameter constraints are violated.</exception>
    /// <exception cref="InvalidOperationException">If a token with the same id already exists.</exception>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    internal UInt160 Create(ApplicationEngine engine, [Length(1, 32)] string name, [Length(2, 6)] string symbol, [Range(0, 18)] byte decimals)
    {
        return Create(engine, name, symbol, decimals, BigInteger.MinusOne);
    }

    /// <summary>
    /// Creates a new token with a specified maximum supply.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="name">The token name (1-32 characters).</param>
    /// <param name="symbol">The token symbol (2-6 characters).</param>
    /// <param name="decimals">The number of decimals (0-18).</param>
    /// <param name="maxSupply">Maximum total supply, or -1 for unlimited.</param>
    /// <returns>The asset <see cref="UInt160"/> identifier generated for the new token.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="maxSupply"/> is less than -1.</exception>
    /// <exception cref="InvalidOperationException">If a token with the same id already exists.</exception>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    internal UInt160 Create(ApplicationEngine engine, [Length(1, 32)] string name, [Length(2, 6)] string symbol, [Range(0, 18)] byte decimals, BigInteger maxSupply)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSupply, BigInteger.MinusOne);
        UInt160 owner = engine.CallingScriptHash!;
        UInt160 tokenid = GetAssetId(owner, name);
        StorageKey key = CreateStorageKey(Prefix_TokenState, tokenid);
        if (engine.SnapshotCache.Contains(key))
            throw new InvalidOperationException($"{name} already exists.");
        var state = new TokenState
        {
            Type = TokenType.Fungible,
            Owner = owner,
            Name = name,
            Symbol = symbol,
            Decimals = decimals,
            TotalSupply = BigInteger.Zero,
            MaxSupply = maxSupply
        };
        engine.SnapshotCache.Add(key, new(state));
        Notify(engine, "Created", tokenid, TokenType.Fungible);
        return tokenid;
    }

    /// <summary>
    /// Creates a new NFT collection with an unlimited maximum supply.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="name">The NFT collection name (1-32 characters).</param>
    /// <param name="symbol">The NFT collection symbol (2-6 characters).</param>
    /// <returns>The asset <see cref="UInt160"/> identifier generated for the new NFT collection.</returns>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    internal UInt160 CreateNFT(ApplicationEngine engine, [Length(1, 32)] string name, [Length(2, 6)] string symbol)
    {
        return CreateNFT(engine, name, symbol, BigInteger.MinusOne);
    }

    /// <summary>
    /// Creates a new NFT collection with a specified maximum supply.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="name">The NFT collection name (1-32 characters).</param>
    /// <param name="symbol">The NFT collection symbol (2-6 characters).</param>
    /// <param name="maxSupply">Maximum total supply for NFTs in this collection, or -1 for unlimited.</param>
    /// <returns>The asset <see cref="UInt160"/> identifier generated for the new NFT collection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="maxSupply"/> is less than -1.</exception>
    /// <exception cref="InvalidOperationException">If a collection with the same id already exists.</exception>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    internal UInt160 CreateNFT(ApplicationEngine engine, [Length(1, 32)] string name, [Length(2, 6)] string symbol, BigInteger maxSupply)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSupply, BigInteger.MinusOne);
        UInt160 owner = engine.CallingScriptHash!;
        UInt160 tokenid = GetAssetId(owner, name);
        StorageKey key = CreateStorageKey(Prefix_TokenState, tokenid);
        if (engine.SnapshotCache.Contains(key))
            throw new InvalidOperationException($"{name} already exists.");
        var state = new TokenState
        {
            Type = TokenType.NonFungible,
            Owner = owner,
            Name = name,
            Symbol = symbol,
            Decimals = 0,
            TotalSupply = BigInteger.Zero,
            MaxSupply = maxSupply
        };
        engine.SnapshotCache.Add(key, new(state));
        Notify(engine, "Created", tokenid, TokenType.NonFungible);
        return tokenid;
    }

    /// <summary>
    /// Retrieves the token metadata for the given asset id.
    /// </summary>
    /// <param name="snapshot">A readonly view of the storage.</param>
    /// <param name="assetId">The asset identifier.</param>
    /// <returns>The <see cref="TokenState"/> if found; otherwise <c>null</c>.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public TokenState? GetTokenInfo(IReadOnlyStore snapshot, UInt160 assetId)
    {
        StorageKey key = CreateStorageKey(Prefix_TokenState, assetId);
        return snapshot.TryGet(key)?.GetInteroperable<TokenState>();
    }

    /// <summary>
    /// Mints new tokens to an account. Only the token owner contract may call this method.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="assetId">The asset identifier.</param>
    /// <param name="account">The recipient account <see cref="UInt160"/>.</param>
    /// <param name="amount">The amount to mint (must be > 0 and &lt;= <see cref="MaxMintAmount"/>).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="amount"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">If the asset id does not exist or caller is not the owner or max supply would be exceeded.</exception>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.All)]
    internal async Task Mint(ApplicationEngine engine, UInt160 assetId, UInt160 account, BigInteger amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, MaxMintAmount);
        AddTotalSupply(engine, TokenType.Fungible, assetId, amount, assertOwner: true);
        AddBalance(engine.SnapshotCache, assetId, account, amount);
        await PostTransferAsync(engine, assetId, null, account, amount, StackItem.Null, callOnPayment: true);
    }

    /// <summary>
    /// Mints a new NFT for the given collection to the specified account using empty properties.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="assetId">The NFT collection asset identifier.</param>
    /// <param name="account">The recipient account <see cref="UInt160"/>.</param>
    /// <returns>The unique id (<see cref="UInt160"/>) of the newly minted NFT.</returns>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.All)]
    internal async Task<UInt160> MintNFT(ApplicationEngine engine, UInt160 assetId, UInt160 account)
    {
        return await MintNFT(engine, assetId, account, new Map(engine.ReferenceCounter));
    }

    /// <summary>
    /// Mints a new NFT for the given collection to the specified account with provided properties.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="assetId">The NFT collection asset identifier.</param>
    /// <param name="account">The recipient account <see cref="UInt160"/>.</param>
    /// <param name="properties">A <see cref="Map"/> of properties for the NFT (keys: ByteString, values: ByteString or Buffer).</param>
    /// <returns>The unique id (<see cref="UInt160"/>) of the newly minted NFT.</returns>
    /// <exception cref="ArgumentException">If properties are invalid (too many, invalid key/value types or lengths).</exception>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 10, RequiredCallFlags = CallFlags.All)]
    internal async Task<UInt160> MintNFT(ApplicationEngine engine, UInt160 assetId, UInt160 account, Map properties)
    {
        if (properties.Count > 8)
            throw new ArgumentException("Too many properties.", nameof(properties));
        foreach (var (k, v) in properties)
        {
            if (k is not ByteString)
                throw new ArgumentException("The key of a property should be a ByteString.", nameof(properties));
            if (k.Size < 1 || k.Size > 16)
                throw new ArgumentException("The key length of a property should be between 1 and 16.", nameof(properties));
            k.GetString(); // Ensure to invoke `ToStrictUtf8String()`
            switch (v)
            {
                case ByteString bs:
                    if (bs.Size < 1 || bs.Size > 128)
                        throw new ArgumentException("The value length of a property should be between 1 and 128.", nameof(properties));
                    break;
                case VM.Types.Buffer buffer:
                    if (buffer.Size < 1 || buffer.Size > 128)
                        throw new ArgumentException("The value length of a property should be between 1 and 128.", nameof(properties));
                    break;
                default:
                    throw new ArgumentException("The value of a property should be a ByteString or Buffer.", nameof(properties));
            }
            v.GetString(); // Ensure to invoke `ToStrictUtf8String()`
        }
        AddTotalSupply(engine, TokenType.NonFungible, assetId, 1, assertOwner: true);
        AddBalance(engine.SnapshotCache, assetId, account, 1);
        UInt160 uniqueId = GetNextNFTUniqueId(engine);
        StorageKey key = CreateStorageKey(Prefix_NFTAssetIdUniqueIdIndex, assetId, uniqueId);
        engine.SnapshotCache.Add(key, new());
        key = CreateStorageKey(Prefix_NFTOwnerUniqueIdIndex, account, uniqueId);
        engine.SnapshotCache.Add(key, new());
        key = CreateStorageKey(Prefix_NFTState, uniqueId);
        engine.SnapshotCache.Add(key, new(new NFTState
        {
            AssetId = assetId,
            Owner = account,
            Properties = properties
        }));
        await PostNFTTransferAsync(engine, uniqueId, null, account, StackItem.Null, callOnPayment: true);
        return uniqueId;
    }

    /// <summary>
    /// Burns tokens from an account, decreasing the total supply. Only the token owner contract may call this method.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="assetId">The asset identifier.</param>
    /// <param name="account">The account <see cref="UInt160"/> from which tokens will be burned.</param>
    /// <param name="amount">The amount to burn (must be > 0 and &lt;= <see cref="MaxMintAmount"/>).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="amount"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">If the asset id does not exist, caller is not the owner, or account has insufficient balance.</exception>
    [ContractMethod(CpuFee = 1 << 17, RequiredCallFlags = CallFlags.All)]
    internal async Task Burn(ApplicationEngine engine, UInt160 assetId, UInt160 account, BigInteger amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, MaxMintAmount);
        AddTotalSupply(engine, TokenType.Fungible, assetId, -amount, assertOwner: true);
        if (!AddBalance(engine.SnapshotCache, assetId, account, -amount))
            throw new InvalidOperationException("Insufficient balance to burn.");
        await PostTransferAsync(engine, assetId, account, null, amount, StackItem.Null, callOnPayment: false);
    }

    /// <summary>
    /// Burns an NFT identified by <paramref name="uniqueId"/>. Only the owner contract may call this method.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="uniqueId">The unique id of the NFT to burn.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">If the unique id does not exist or owner has insufficient balance or caller is not owner contract.</exception>
    [ContractMethod(CpuFee = 1 << 17, RequiredCallFlags = CallFlags.All)]
    internal async Task BurnNFT(ApplicationEngine engine, UInt160 uniqueId)
    {
        StorageKey key = CreateStorageKey(Prefix_NFTState, uniqueId);
        NFTState nft = engine.SnapshotCache.TryGet(key)?.GetInteroperable<NFTState>()
            ?? throw new InvalidOperationException("The unique id does not exist.");
        AddTotalSupply(engine, TokenType.NonFungible, nft.AssetId, BigInteger.MinusOne, assertOwner: true);
        if (!AddBalance(engine.SnapshotCache, nft.AssetId, nft.Owner, BigInteger.MinusOne))
            throw new InvalidOperationException("Insufficient balance to burn.");
        engine.SnapshotCache.Delete(key);
        key = CreateStorageKey(Prefix_NFTAssetIdUniqueIdIndex, nft.AssetId, uniqueId);
        engine.SnapshotCache.Delete(key);
        key = CreateStorageKey(Prefix_NFTOwnerUniqueIdIndex, nft.Owner, uniqueId);
        engine.SnapshotCache.Delete(key);
        await PostNFTTransferAsync(engine, uniqueId, nft.Owner, null, StackItem.Null, callOnPayment: false);
    }

    /// <summary>
    /// Transfers tokens between accounts.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="assetId">The asset identifier.</param>
    /// <param name="from">The sender account <see cref="UInt160"/>.</param>
    /// <param name="to">The recipient account <see cref="UInt160"/>.</param>
    /// <param name="amount">The amount to transfer (must be &gt;= 0).</param>
    /// <param name="data">Arbitrary data passed to <c>onPayment</c> or <c>onTransfer</c> callbacks.</param>
    /// <returns><c>true</c> if the transfer succeeded; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="amount"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">If the asset id does not exist.</exception>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.All)]
    internal async Task<bool> Transfer(ApplicationEngine engine, UInt160 assetId, UInt160 from, UInt160 to, BigInteger amount, StackItem data)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        StorageKey key = CreateStorageKey(Prefix_TokenState, assetId);
        TokenState token = engine.SnapshotCache.TryGet(key)?.GetInteroperable<TokenState>()
            ?? throw new InvalidOperationException("The asset id does not exist.");
        if (token.Type != TokenType.Fungible)
            throw new InvalidOperationException("The asset id and the token type do not match.");
        if (!engine.CheckWitnessInternal(from)) return false;
        if (!amount.IsZero && from != to)
        {
            if (!AddBalance(engine.SnapshotCache, assetId, from, -amount))
                return false;
            AddBalance(engine.SnapshotCache, assetId, to, amount);
        }
        await PostTransferAsync(engine, assetId, from, to, amount, data, callOnPayment: true);
        await engine.CallFromNativeContractAsync(Hash, token.Owner, "onTransfer", assetId, from, to, amount, data);
        return true;
    }

    /// <summary>
    /// Transfers an NFT between owners.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="uniqueId">The unique id of the NFT.</param>
    /// <param name="from">The current owner account <see cref="UInt160"/>.</param>
    /// <param name="to">The recipient account <see cref="UInt160"/>.</param>
    /// <param name="data">Arbitrary data passed to <c>onNFTPayment</c> or <c>onNFTTransfer</c> callbacks.</param>
    /// <returns><c>true</c> if the transfer succeeded; otherwise <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">If the unique id does not exist.</exception>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.All)]
    internal async Task<bool> TransferNFT(ApplicationEngine engine, UInt160 uniqueId, UInt160 from, UInt160 to, StackItem data)
    {
        StorageKey key_nft = CreateStorageKey(Prefix_NFTState, uniqueId);
        NFTState nft = engine.SnapshotCache.TryGet(key_nft)?.GetInteroperable<NFTState>()
            ?? throw new InvalidOperationException("The unique id does not exist.");
        if (nft.Owner != from) return false;
        if (!engine.CheckWitnessInternal(from)) return false;
        StorageKey key = CreateStorageKey(Prefix_TokenState, nft.AssetId);
        TokenState token = engine.SnapshotCache.TryGet(key)!.GetInteroperable<TokenState>();
        if (from != to)
        {
            if (!AddBalance(engine.SnapshotCache, nft.AssetId, from, BigInteger.MinusOne))
                return false;
            AddBalance(engine.SnapshotCache, nft.AssetId, to, BigInteger.One);
            key = CreateStorageKey(Prefix_NFTOwnerUniqueIdIndex, from, uniqueId);
            engine.SnapshotCache.Delete(key);
            key = CreateStorageKey(Prefix_NFTOwnerUniqueIdIndex, to, uniqueId);
            engine.SnapshotCache.Add(key, new());
            nft = engine.SnapshotCache.GetAndChange(key_nft)!.GetInteroperable<NFTState>();
            nft.Owner = to;
        }
        await PostNFTTransferAsync(engine, uniqueId, from, to, data, callOnPayment: true);
        await engine.CallFromNativeContractAsync(Hash, token.Owner, "onNFTTransfer", uniqueId, from, to, data);
        return true;
    }

    /// <summary>
    /// Returns the balance of <paramref name="account"/> for the specified <paramref name="assetId"/>.
    /// </summary>
    /// <param name="snapshot">A readonly view of the storage.</param>
    /// <param name="assetId">The asset identifier.</param>
    /// <param name="account">The account <see cref="UInt160"/> whose balance is requested.</param>
    /// <returns>The account balance as a <see cref="BigInteger"/>.</returns>
    /// <exception cref="InvalidOperationException">If the asset id does not exist.</exception>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public BigInteger BalanceOf(IReadOnlyStore snapshot, UInt160 assetId, UInt160 account)
    {
        StorageKey key = CreateStorageKey(Prefix_TokenState, assetId);
        if (!snapshot.Contains(key))
            throw new InvalidOperationException("The asset id does not exist.");
        key = CreateStorageKey(Prefix_AccountState, account, assetId);
        AccountState? accountState = snapshot.TryGet(key)?.GetInteroperable<AccountState>();
        if (accountState is null) return BigInteger.Zero;
        return accountState.Balance;
    }

    /// <summary>
    /// Gets NFT metadata for a unique id.
    /// </summary>
    /// <param name="snapshot">A readonly view of the storage.</param>
    /// <param name="uniqueId">The unique id of the NFT.</param>
    /// <returns>The <see cref="NFTState"/> if found; otherwise <c>null</c>.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public NFTState? GetNFTInfo(IReadOnlyStore snapshot, UInt160 uniqueId)
    {
        StorageKey key = CreateStorageKey(Prefix_NFTState, uniqueId);
        return snapshot.TryGet(key)?.GetInteroperable<NFTState>();
    }

    /// <summary>
    /// Returns an iterator over the unique ids of NFTs for the specified asset (collection).
    /// The iterator yields the stored unique id keys (UInt160) indexed under the NFT asset id.
    /// </summary>
    /// <param name="snapshot">A readonly view of the storage.</param>
    /// <param name="assetId">The asset (collection) identifier whose NFTs are requested.</param>
    /// <returns>
    /// An <see cref="IIterator"/> that enumerates the NFT unique ids belonging to the given collection.
    /// The iterator is configured to return keys only and to remove the storage prefix.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the specified asset id does not exist.</exception>
    /// <remarks>
    /// The returned iterator is backed by the storage layer and uses the NFT asset-to-unique-id index.
    /// Consumers should dispose the iterator when finished if they hold unmanaged resources from it.
    /// </remarks>
    [ContractMethod(CpuFee = 1 << 22, RequiredCallFlags = CallFlags.ReadStates)]
    public IIterator GetNFTs(IReadOnlyStore snapshot, UInt160 assetId)
    {
        StorageKey key = CreateStorageKey(Prefix_TokenState, assetId);
        if (!snapshot.Contains(key))
            throw new InvalidOperationException("The asset id does not exist.");
        const FindOptions options = FindOptions.KeysOnly | FindOptions.RemovePrefix;
        var prefixKey = CreateStorageKey(Prefix_NFTAssetIdUniqueIdIndex, assetId);
        var enumerator = snapshot.Find(prefixKey).GetEnumerator();
        return new StorageIterator(enumerator, 21, options);
    }

    /// <summary>
    /// Returns an iterator over the unique ids of NFTs owned by the specified account.
    /// The iterator yields the stored unique id keys (<see cref="UInt160"/>) indexed under the NFT owner index.
    /// </summary>
    /// <param name="snapshot">A readonly view of the storage.</param>
    /// <param name="account">The account whose NFTs are requested.</param>
    /// <returns>
    /// An <see cref="IIterator"/> that enumerates the NFT unique ids owned by the given account.
    /// The iterator is configured to return keys only and to remove the storage prefix.
    /// </returns>
    /// <remarks>
    /// The returned iterator is backed by the storage layer and uses the NFT owner-to-unique-id index.
    /// Consumers should dispose the iterator when finished if they hold unmanaged resources from it.
    /// </remarks>
    [ContractMethod(CpuFee = 1 << 22, RequiredCallFlags = CallFlags.ReadStates)]
    public IIterator GetNFTsOfOwner(IReadOnlyStore snapshot, UInt160 account)
    {
        const FindOptions options = FindOptions.KeysOnly | FindOptions.RemovePrefix;
        var prefixKey = CreateStorageKey(Prefix_NFTOwnerUniqueIdIndex, account);
        var enumerator = snapshot.Find(prefixKey).GetEnumerator();
        return new StorageIterator(enumerator, 21, options);
    }

    /// <summary>
    /// Computes a unique asset id from the token owner's script hash and the token name.
    /// </summary>
    /// <param name="owner">Owner contract hash.</param>
    /// <param name="name">Token name.</param>
    /// <returns>The asset id for the token.</returns>
    public static UInt160 GetAssetId(UInt160 owner, string name)
    {
        byte[] nameBytes = name.ToStrictUtf8Bytes();
        byte[] buffer = new byte[UInt160.Length + nameBytes.Length];
        owner.Serialize(buffer);
        nameBytes.CopyTo(buffer.AsSpan()[UInt160.Length..]);
        return buffer.ToScriptHash();
    }

    UInt160 GetNextNFTUniqueId(ApplicationEngine engine)
    {
        StorageKey key = CreateStorageKey(Prefix_NFTUniqueIdSeed);
        BigInteger seed = engine.SnapshotCache.GetAndChange(key)!.Add(BigInteger.One);
        using MemoryStream ms = new();
        ms.Write(engine.PersistingBlock!.Hash.GetSpan());
        ms.Write(seed.ToByteArrayStandard());
        return ms.ToArray().ToScriptHash();
    }

    void AddTotalSupply(ApplicationEngine engine, TokenType type, UInt160 assetId, BigInteger amount, bool assertOwner)
    {
        StorageKey key = CreateStorageKey(Prefix_TokenState, assetId);
        TokenState token = engine.SnapshotCache.GetAndChange(key)?.GetInteroperable<TokenState>()
            ?? throw new InvalidOperationException("The asset id does not exist.");
        if (token.Type != type)
            throw new InvalidOperationException("The asset id and the token type do not match.");
        if (assertOwner && token.Owner != engine.CallingScriptHash)
            throw new InvalidOperationException("This method can be called by the owner contract only.");
        token.TotalSupply += amount;
        if (token.TotalSupply < 0)
            throw new InvalidOperationException("Insufficient balance to burn.");
        if (token.MaxSupply >= 0 && token.TotalSupply > token.MaxSupply)
            throw new InvalidOperationException("The total supply exceeds the maximum supply.");
    }

    bool AddBalance(DataCache snapshot, UInt160 assetId, UInt160 account, BigInteger amount)
    {
        if (amount.IsZero) return true;
        StorageKey key = CreateStorageKey(Prefix_AccountState, account, assetId);
        AccountState? accountState = snapshot.GetAndChange(key)?.GetInteroperable<AccountState>();
        if (amount > 0)
        {
            if (accountState is null)
            {
                accountState = new AccountState { Balance = amount };
                snapshot.Add(key, new(accountState));
            }
            else
            {
                accountState.Balance += amount;
            }
        }
        else
        {
            if (accountState is null) return false;
            if (accountState.Balance < -amount) return false;
            accountState.Balance += amount;
            if (accountState.Balance.IsZero)
                snapshot.Delete(key);
        }
        return true;
    }

    async ContractTask PostTransferAsync(ApplicationEngine engine, UInt160 assetId, UInt160? from, UInt160? to, BigInteger amount, StackItem data, bool callOnPayment)
    {
        Notify(engine, "Transfer", assetId, from, to, amount);
        if (!callOnPayment || to is null || !ContractManagement.IsContract(engine.SnapshotCache, to)) return;
        await engine.CallFromNativeContractAsync(Hash, to, "onPayment", assetId, from, amount, data);
    }

    async ContractTask PostNFTTransferAsync(ApplicationEngine engine, UInt160 uniqueId, UInt160? from, UInt160? to, StackItem data, bool callOnPayment)
    {
        Notify(engine, "NFTTransfer", uniqueId, from, to);
        if (!callOnPayment || to is null || !ContractManagement.IsContract(engine.SnapshotCache, to)) return;
        await engine.CallFromNativeContractAsync(Hash, to, "onNFTPayment", uniqueId, from, data);
    }
}

/// <summary>
/// Specifies the type of token, indicating whether it is fungible or non-fungible.
/// </summary>
public enum TokenType : byte
{
    /// <summary>
    /// Fungible token type.
    /// </summary>
    Fungible = 1,
    /// <summary>
    /// Non-fungible token (NFT) type.
    /// </summary>
    NonFungible = 2
}

/// <summary>
/// Represents the persisted metadata for a token.
/// Implements <see cref="IInteroperable"/> to allow conversion to/from VM <see cref="StackItem"/>.
/// </summary>
public class TokenState : IInteroperable
{
    /// <summary>
    /// Specifies the type of token represented by this instance.
    /// </summary>
    public required TokenType Type;

    /// <summary>
    /// The owner contract script hash that can manage this token (mint/burn, onTransfer callback target).
    /// </summary>
    public required UInt160 Owner;

    /// <summary>
    /// The token's human-readable name.
    /// </summary>
    public required string Name;

    /// <summary>
    /// The token's symbol (short string).
    /// </summary>
    public required string Symbol;

    /// <summary>
    /// Number of decimal places the token supports.
    /// </summary>
    public required byte Decimals;

    /// <summary>
    /// Current total supply of the token.
    /// </summary>
    public BigInteger TotalSupply;

    /// <summary>
    /// Maximum total supply allowed; -1 indicates no limit.
    /// </summary>
    public BigInteger MaxSupply;

    /// <summary>
    /// Populates this instance from a VM <see cref="StackItem"/> representation.
    /// </summary>
    /// <param name="stackItem">A <see cref="StackItem"/> expected to be a <see cref="Struct"/> with the token fields in order.</param>
    public void FromStackItem(StackItem stackItem)
    {
        Struct @struct = (Struct)stackItem;
        Owner = new UInt160(@struct[0].GetSpan());
        Name = @struct[1].GetString()!;
        Symbol = @struct[2].GetString()!;
        Decimals = (byte)@struct[3].GetInteger();
        TotalSupply = @struct[4].GetInteger();
        MaxSupply = @struct[5].GetInteger();
    }

    /// <summary>
    /// Converts this instance to a VM <see cref="StackItem"/> representation.
    /// </summary>
    /// <param name="referenceCounter">Optional reference counter used by the VM.</param>
    /// <returns>A <see cref="Struct"/> containing the token fields in order.</returns>
    public StackItem ToStackItem(IReferenceCounter? referenceCounter)
    {
        return new Struct(referenceCounter) { Owner.ToArray(), Name, Symbol, Decimals, TotalSupply, MaxSupply };
    }
}

public class NFTState : IInteroperable
{
    /// <summary>
    /// The asset id (collection) this NFT belongs to.
    /// </summary>
    public required UInt160 AssetId;

    /// <summary>
    /// The account (owner) that currently owns this NFT.
    /// </summary>
    public required UInt160 Owner;

    /// <summary>
    /// Arbitrary properties associated with this NFT. Keys are ByteString and values are ByteString or Buffer.
    /// </summary>
    public required Map Properties;

    /// <summary>
    /// Populates this instance from a VM <see cref="StackItem"/> representation.
    /// </summary>
    /// <param name="stackItem">A <see cref="StackItem"/> expected to be a <see cref="Struct"/> with fields in the order: AssetId, Owner, Properties.</param>
    public void FromStackItem(StackItem stackItem)
    {
        Struct @struct = (Struct)stackItem;
        AssetId = new UInt160(@struct[0].GetSpan());
        Owner = new UInt160(@struct[1].GetSpan());
        Properties = (Map)@struct[2];
    }

    /// <summary>
    /// Convert current NFTState to a VM <see cref="StackItem"/> (Struct).
    /// </summary>
    /// <param name="referenceCounter">Optional reference counter used by the VM.</param>
    /// <returns>A <see cref="Struct"/> representing the NFTState.</returns>
    public StackItem ToStackItem(IReferenceCounter? referenceCounter)
    {
        return new Struct(referenceCounter) { AssetId.ToArray(), Owner.ToArray(), Properties };
    }
}
