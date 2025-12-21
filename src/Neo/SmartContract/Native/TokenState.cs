// Copyright (C) 2015-2025 The Neo Project.
//
// TokenState.cs file belongs to the neo project and is free
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
        Type = (TokenType)(byte)@struct[0].GetInteger();
        Owner = new UInt160(@struct[1].GetSpan());
        Name = @struct[2].GetString()!;
        Symbol = @struct[3].GetString()!;
        Decimals = (byte)@struct[4].GetInteger();
        TotalSupply = @struct[5].GetInteger();
        MaxSupply = @struct[6].GetInteger();
    }

    /// <summary>
    /// Converts this instance to a VM <see cref="StackItem"/> representation.
    /// </summary>
    /// <param name="referenceCounter">Optional reference counter used by the VM.</param>
    /// <returns>A <see cref="Struct"/> containing the token fields in order.</returns>
    public StackItem ToStackItem(IReferenceCounter? referenceCounter)
    {
        return new Struct(referenceCounter) { (byte)Type, Owner.ToArray(), Name, Symbol, Decimals, TotalSupply, MaxSupply };
    }
}
