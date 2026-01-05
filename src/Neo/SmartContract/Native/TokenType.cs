// Copyright (C) 2015-2026 The Neo Project.
//
// TokenType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract.Native;

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
