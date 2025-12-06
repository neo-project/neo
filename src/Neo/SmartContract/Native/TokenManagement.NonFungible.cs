// Copyright (C) 2015-2025 The Neo Project.
//
// TokenManagement.NonFungible.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native;

[ContractEvent(2, "NFTTransfer", "uniqueId", ContractParameterType.Hash160, "from", ContractParameterType.Hash160, "to", ContractParameterType.Hash160)]
partial class TokenManagement
{
    const byte Prefix_NFTUniqueIdSeed = 15;
    const byte Prefix_NFTState = 8;
    const byte Prefix_NFTOwnerUniqueIdIndex = 21;
    const byte Prefix_NFTAssetIdUniqueIdIndex = 23;

    partial void Initialize_NonFungible(ApplicationEngine engine, Hardfork? hardfork)
    {
        if (hardfork == ActiveIn)
        {
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_NFTUniqueIdSeed), BigInteger.Zero);
        }
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

    UInt160 GetNextNFTUniqueId(ApplicationEngine engine)
    {
        StorageKey key = CreateStorageKey(Prefix_NFTUniqueIdSeed);
        BigInteger seed = engine.SnapshotCache.GetAndChange(key)!.Add(BigInteger.One);
        using MemoryStream ms = new();
        ms.Write(engine.PersistingBlock!.Hash.GetSpan());
        ms.Write(seed.ToByteArrayStandard());
        return ms.ToArray().ToScriptHash();
    }

    async ContractTask PostNFTTransferAsync(ApplicationEngine engine, UInt160 uniqueId, UInt160? from, UInt160? to, StackItem data, bool callOnPayment)
    {
        Notify(engine, "NFTTransfer", uniqueId, from, to);
        if (!callOnPayment || to is null || !ContractManagement.IsContract(engine.SnapshotCache, to)) return;
        await engine.CallFromNativeContractAsync(Hash, to, "onNFTPayment", uniqueId, from, data);
    }
}
