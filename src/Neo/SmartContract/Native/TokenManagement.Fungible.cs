// Copyright (C) 2015-2025 The Neo Project.
//
// TokenManagement.Fungible.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native;

[ContractEvent(1, "Transfer", "assetId", ContractParameterType.Hash160, "from", ContractParameterType.Hash160, "to", ContractParameterType.Hash160, "amount", ContractParameterType.Integer)]
partial class TokenManagement
{
    static readonly BigInteger MaxMintAmount = BigInteger.Pow(2, 128);

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
        return CreateInternal(engine, engine.CallingScriptHash!, name, symbol, decimals, maxSupply);
    }

    internal UInt160 CreateInternal(ApplicationEngine engine, UInt160 owner, string name, string symbol, byte decimals, BigInteger maxSupply)
    {
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
    /// Mints new tokens to an account. Only the token owner contract may call this method.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="assetId">The asset identifier.</param>
    /// <param name="account">The recipient account <see cref="UInt160"/>.</param>
    /// <param name="amount">The amount to mint (must be > 0 and &lt;= <see cref="MaxMintAmount"/>).</param>
    /// <returns>A <see cref="ContractTask"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="amount"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">If the asset id does not exist or caller is not the owner or max supply would be exceeded.</exception>
    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.All)]
    internal async ContractTask Mint(ApplicationEngine engine, UInt160 assetId, UInt160 account, BigInteger amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, MaxMintAmount);
        await MintInternal(engine, assetId, account, amount, assertOwner: true, callOnPayment: true);
    }

    internal async ContractTask MintInternal(ApplicationEngine engine, UInt160 assetId, UInt160 account, BigInteger amount, bool assertOwner, bool callOnPayment)
    {
        AddTotalSupply(engine, TokenType.Fungible, assetId, amount, assertOwner);
        AddBalance(engine.SnapshotCache, assetId, account, amount);
        await PostTransferAsync(engine, assetId, null, account, amount, StackItem.Null, callOnPayment);
    }

    /// <summary>
    /// Burns tokens from an account, decreasing the total supply. Only the token owner contract may call this method.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="assetId">The asset identifier.</param>
    /// <param name="account">The account <see cref="UInt160"/> from which tokens will be burned.</param>
    /// <param name="amount">The amount to burn (must be > 0 and &lt;= <see cref="MaxMintAmount"/>).</param>
    /// <returns>A <see cref="ContractTask"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="amount"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">If the asset id does not exist, caller is not the owner, or account has insufficient balance.</exception>
    [ContractMethod(CpuFee = 1 << 17, RequiredCallFlags = CallFlags.All)]
    internal async ContractTask Burn(ApplicationEngine engine, UInt160 assetId, UInt160 account, BigInteger amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, MaxMintAmount);
        await BurnInternal(engine, assetId, account, amount, assertOwner: true);
    }

    internal async ContractTask BurnInternal(ApplicationEngine engine, UInt160 assetId, UInt160 account, BigInteger amount, bool assertOwner)
    {
        AddTotalSupply(engine, TokenType.Fungible, assetId, -amount, assertOwner);
        if (!AddBalance(engine.SnapshotCache, assetId, account, -amount))
            throw new InvalidOperationException("Insufficient balance to burn.");
        await PostTransferAsync(engine, assetId, account, null, amount, StackItem.Null, callOnPayment: false);
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
    internal async ContractTask<bool> Transfer(ApplicationEngine engine, UInt160 assetId, UInt160 from, UInt160 to, BigInteger amount, StackItem data)
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
        await engine.CallFromNativeContractAsync(Hash, token.Owner, "_onTransfer", assetId, from, to, amount, data);
        return true;
    }

    async ContractTask PostTransferAsync(ApplicationEngine engine, UInt160 assetId, UInt160? from, UInt160? to, BigInteger amount, StackItem data, bool callOnPayment)
    {
        Notify(engine, "Transfer", assetId, from, to, amount);
        if (!callOnPayment || to is null || !ContractManagement.IsContract(engine.SnapshotCache, to)) return;
        await engine.CallFromNativeContractAsync(Hash, to, "_onPayment", assetId, from, amount, data);
    }
}
