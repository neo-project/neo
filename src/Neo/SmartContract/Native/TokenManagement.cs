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

public sealed class TokenManagement : NativeContract
{
    const byte Prefix_TokenState = 10;
    const byte Prefix_AccountState = 12;

    static readonly BigInteger MaxMintAmount = BigInteger.Pow(2, 128);

    internal TokenManagement() { }

    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    internal UInt160 Create(ApplicationEngine engine, [Length(1, 32)] string name, [Length(2, 6)] string symbol, [Range(0, 18)] byte decimals)
    {
        UInt160 owner = engine.CallingScriptHash!;
        UInt160 tokenid = GetTokenId(owner, name);
        StorageKey key = CreateStorageKey(Prefix_TokenState, tokenid);
        if (engine.SnapshotCache.Contains(key))
            throw new InvalidOperationException($"{name} already exists.");
        var state = new TokenState
        {
            Owner = owner,
            Name = name,
            Symbol = symbol,
            Decimals = decimals,
            TotalSupply = BigInteger.Zero
        };
        engine.SnapshotCache.Add(key, new(state));
        return tokenid;
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public TokenState? GetTokenInfo(ApplicationEngine engine, UInt160 tokenid)
    {
        StorageKey key = CreateStorageKey(Prefix_TokenState, tokenid);
        return engine.SnapshotCache.TryGet(key)?.GetInteroperable<TokenState>();
    }

    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.All)]
    internal void Mint(ApplicationEngine engine, UInt160 tokenid, UInt160 account, BigInteger amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, MaxMintAmount);
        AddTotalSupply(engine, tokenid, amount);
        AddBalance(engine.SnapshotCache, tokenid, account, amount);
    }

    [ContractMethod(CpuFee = 1 << 17, RequiredCallFlags = CallFlags.All)]
    internal void Burn(ApplicationEngine engine, UInt160 tokenid, UInt160 account, BigInteger amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, MaxMintAmount);
        AddTotalSupply(engine, tokenid, -amount);
        if (!AddBalance(engine.SnapshotCache, tokenid, account, -amount))
            throw new InvalidOperationException("Insufficient balance to burn.");
    }

    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.All)]
    internal bool Transfer(ApplicationEngine engine, UInt160 tokenid, UInt160 from, UInt160 to, BigInteger amount, StackItem data)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        StorageKey key = CreateStorageKey(Prefix_TokenState, tokenid);
        if (!engine.SnapshotCache.Contains(key))
            throw new InvalidOperationException("The token id does not exist.");
        if (!engine.CheckWitnessInternal(from)) return false;
        if (amount.IsZero) return true;
        if (!AddBalance(engine.SnapshotCache, tokenid, from, -amount))
            return false;
        AddBalance(engine.SnapshotCache, tokenid, to, amount);
        return true;
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public BigInteger BalanceOf(IReadOnlyStore snapshot, UInt160 tokenid, UInt160 account)
    {
        StorageKey key = CreateStorageKey(Prefix_TokenState, tokenid);
        if (!snapshot.Contains(key))
            throw new InvalidOperationException("The token id does not exist.");
        key = CreateStorageKey(Prefix_AccountState, account, tokenid);
        AccountState? accountState = snapshot.TryGet(key)?.GetInteroperable<AccountState>();
        if (accountState is null) return BigInteger.Zero;
        return accountState.Balance;
    }

    public static UInt160 GetTokenId(UInt160 owner, string name)
    {
        byte[] nameBytes = name.ToStrictUtf8Bytes();
        byte[] buffer = new byte[UInt160.Length + nameBytes.Length];
        owner.Serialize(buffer);
        nameBytes.CopyTo(buffer.AsSpan()[UInt160.Length..]);
        return buffer.ToScriptHash();
    }

    void AddTotalSupply(ApplicationEngine engine, UInt160 tokenid, BigInteger amount)
    {
        StorageKey key = CreateStorageKey(Prefix_TokenState, tokenid);
        TokenState token = engine.SnapshotCache.GetAndChange(key)?.GetInteroperable<TokenState>()
            ?? throw new InvalidOperationException("The token id does not exist.");
        if (token.Owner != engine.CallingScriptHash)
            throw new InvalidOperationException("Mint can be called by the owner contract only.");
        if (token.TotalSupply + amount < 0)
            throw new InvalidOperationException("Insufficient balance to burn.");
        token.TotalSupply += amount;
    }

    bool AddBalance(DataCache snapshot, UInt160 tokenid, UInt160 account, BigInteger amount)
    {
        if (amount.IsZero) return true;
        StorageKey key = CreateStorageKey(Prefix_AccountState, account, tokenid);
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
}

public class TokenState : IInteroperable
{
    public required UInt160 Owner;
    public required string Name;
    public required string Symbol;
    public required byte Decimals;
    public BigInteger TotalSupply;

    public void FromStackItem(StackItem stackItem)
    {
        Struct @struct = (Struct)stackItem;
        Owner = new UInt160(@struct[0].GetSpan());
        Name = @struct[1].GetString()!;
        Symbol = @struct[2].GetString()!;
        Decimals = (byte)@struct[3].GetInteger();
        TotalSupply = @struct[4].GetInteger();
    }

    public StackItem ToStackItem(IReferenceCounter? referenceCounter)
    {
        return new Struct(referenceCounter) { Owner.ToArray(), Name, Symbol, Decimals, TotalSupply };
    }
}
