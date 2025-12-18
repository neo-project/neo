// Copyright (C) 2015-2025 The Neo Project.
//
// NFTState.cs file belongs to the neo project and is free
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

namespace Neo.SmartContract.Native;

/// <summary>
/// Represents the state of a non-fungible token (NFT), including its asset identifier, owner, and associated properties.
/// Implements <see cref="IInteroperable"/> to allow conversion to/from VM <see cref="StackItem"/>.
/// </summary>
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
