// Copyright (C) 2015-2026 The Neo Project.
//
// AssetDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.SmartContract.Native;

namespace Neo.Wallets;

/// <summary>
/// Represents the descriptor of an asset.
/// </summary>
public class AssetDescriptor
{
    /// <summary>
    /// The id of the asset.
    /// </summary>
    public UInt160 AssetId { get; }

    /// <summary>
    /// The name of the asset.
    /// </summary>
    public string AssetName { get; }

    /// <summary>
    /// The symbol of the asset.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// The number of decimal places of the token.
    /// </summary>
    public byte Decimals { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetDescriptor"/> class.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="assetId">The id of the asset.</param>
    public AssetDescriptor(DataCache snapshot, UInt160 assetId)
    {
        TokenState token = NativeContract.TokenManagement.GetTokenInfo(snapshot, assetId)
            ?? throw new ArgumentException($"No token found for assetId {assetId}. Please ensure the assetId is correct and the asset is deployed on the blockchain.", nameof(assetId));
        AssetId = assetId;
        AssetName = token.Name;
        Symbol = token.Symbol;
        Decimals = token.Decimals;
    }

    public override string ToString()
    {
        return AssetName;
    }
}
