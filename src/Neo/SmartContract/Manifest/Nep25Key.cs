// Copyright (C) 2015-2025 The Neo Project.
//
// Nep25Key.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// key is only used along with the Map type (MUST NOT be used for other types) and can have Signature, Boolean,
    /// Integer, Hash160, Hash256, ByteArray, PublicKey or String value.
    /// That is all the basic types that can be used as a map key.
    /// </summary>
    public enum Nep25Key
    {
        Signature,
        Boolean,
        Integer,
        Hash160,
        Hash256,
        ByteArray,
        PublicKey,
        String
    }
}
