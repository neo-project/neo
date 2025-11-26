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
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native;

public sealed class TokenManagement : NativeContract
{
    const byte Prefix_TokenState = 10;

    internal TokenManagement() { }

    [ContractMethod(CpuFee = 1 << 17, StorageFee = 1 << 7, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    internal UInt160 Create(ApplicationEngine engine, [MaxLength(32)] string name, [MaxLength(6)] string symbol, byte decimals)
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

    static UInt160 GetTokenId(UInt160 owner, string name)
    {
        byte[] nameBytes = name.ToStrictUtf8Bytes();
        byte[] buffer = new byte[UInt160.Length + nameBytes.Length];
        owner.Serialize(buffer);
        nameBytes.CopyTo(buffer.AsSpan()[UInt160.Length..]);
        return buffer.ToScriptHash();
    }
}

file class TokenState : IInteroperable
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
