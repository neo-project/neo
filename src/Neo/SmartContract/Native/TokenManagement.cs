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
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native;

/// <summary>
/// Provides core functionality for creating, managing, and transferring tokens within a native contract environment.
/// </summary>
[ContractEvent(0, "Created", "assetId", ContractParameterType.Hash160)]
[ContractEvent(1, "Transfer", "assetId", ContractParameterType.Hash160, "from", ContractParameterType.Hash160, "to", ContractParameterType.Hash160, "amount", ContractParameterType.Integer)]
public sealed class TokenManagement : NativeContract
{
    const byte Prefix_TokenState = 10;
    const byte Prefix_AccountState = 12;

    static readonly BigInteger MaxMintAmount = BigInteger.Pow(2, 128);

    internal TokenManagement() : base(-12) { }

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
            Owner = owner,
            Name = name,
            Symbol = symbol,
            Decimals = decimals,
            TotalSupply = BigInteger.Zero,
            MaxSupply = maxSupply
        };
        engine.SnapshotCache.Add(key, new(state));
        Notify(engine, "Created", tokenid);
        return tokenid;
    }

    /// <summary>
    /// Retrieves the token metadata for the given asset id.
    /// </summary>
    /// <param name="engine">The current <see cref="ApplicationEngine"/> instance.</param>
    /// <param name="assetId">The asset identifier.</param>
    /// <returns>The <see cref="TokenState"/> if found; otherwise <c>null</c>.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public TokenState? GetTokenInfo(ApplicationEngine engine, UInt160 assetId)
    {
        StorageKey key = CreateStorageKey(Prefix_TokenState, assetId);
        return engine.SnapshotCache.TryGet(key)?.GetInteroperable<TokenState>();
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
        AddTotalSupply(engine, assetId, amount, assertOwner: true);
        AddBalance(engine.SnapshotCache, assetId, account, amount);
        await PostTransferAsync(engine, assetId, null, account, amount, StackItem.Null, callOnPayment: true);
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
        AddTotalSupply(engine, assetId, -amount, assertOwner: true);
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
    internal async Task<bool> Transfer(ApplicationEngine engine, UInt160 assetId, UInt160 from, UInt160 to, BigInteger amount, StackItem data)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        StorageKey key = CreateStorageKey(Prefix_TokenState, assetId);
        TokenState token = engine.SnapshotCache.TryGet(key)?.GetInteroperable<TokenState>()
            ?? throw new InvalidOperationException("The asset id does not exist.");
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

    void AddTotalSupply(ApplicationEngine engine, UInt160 assetId, BigInteger amount, bool assertOwner)
    {
        StorageKey key = CreateStorageKey(Prefix_TokenState, assetId);
        TokenState token = engine.SnapshotCache.GetAndChange(key)?.GetInteroperable<TokenState>()
            ?? throw new InvalidOperationException("The asset id does not exist.");
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
}

/// <summary>
/// Represents the persisted metadata for a token.
/// Implements <see cref="IInteroperable"/> to allow conversion to/from VM <see cref="StackItem"/>.
/// </summary>
public class TokenState : IInteroperable
{
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
