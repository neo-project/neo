// Copyright (C) 2015-2026 The Neo Project.
//
// TokenManagement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using System.Numerics;

namespace Neo.SmartContract.Native;

/// <summary>
/// Provides core functionality for creating, managing, and transferring tokens within a native contract environment.
/// </summary>
[ContractEvent(0, "Created", "assetId", ContractParameterType.Hash160, "type", ContractParameterType.Integer)]
public sealed partial class TokenManagement : NativeContract
{
    const byte Prefix_TokenState = 10;
    const byte Prefix_AccountState = 12;

    internal TokenManagement() : base(-12) { }

    partial void Initialize_Fungible(ApplicationEngine engine, Hardfork? hardfork);
    partial void Initialize_NonFungible(ApplicationEngine engine, Hardfork? hardfork);

    internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
    {
        Initialize_Fungible(engine, hardfork);
        Initialize_NonFungible(engine, hardfork);
        return ContractTask.CompletedTask;
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

    TokenState AddTotalSupply(ApplicationEngine engine, TokenType type, UInt160 assetId, BigInteger amount, bool assertOwner)
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
        return token;
    }

    async ContractTask<bool> AddBalance(ApplicationEngine engine, UInt160 assetId, TokenState token, UInt160 account, BigInteger amount, bool callOnBalanceChanged)
    {
        if (amount.IsZero) return true;
        StorageKey key = CreateStorageKey(Prefix_AccountState, account, assetId);
        AccountState? accountState = engine.SnapshotCache.GetAndChange(key)?.GetInteroperable<AccountState>();
        BigInteger balanceOld = accountState?.Balance ?? BigInteger.Zero;
        if (amount > 0)
        {
            if (accountState is null)
            {
                accountState = new AccountState { Balance = amount };
                engine.SnapshotCache.Add(key, new(accountState));
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
                engine.SnapshotCache.Delete(key);
        }
        if (callOnBalanceChanged)
            await engine.CallFromNativeContractIfExistsAsync(Hash, token.Owner, "_onBalanceChanged", assetId, account, amount, balanceOld, balanceOld + amount);
        return true;
    }
}
