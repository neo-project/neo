// Copyright (C) 2015-2025 The Neo Project.
//
// Nep17Token.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;

namespace Neo.Build.Core.Tokens
{
    public class Nep17Token(
        ProtocolSettings protocolSettings,
        DataCache snapshot,
        UInt160 scriptHash)
        : TokenBase(
            protocolSettings,
            snapshot,
            scriptHash)
    { }
}
