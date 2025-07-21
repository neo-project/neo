// Copyright (C) 2015-2025 The Neo Project.
//
// Address.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.RpcServer.Model
{
    /// <summary>
    /// A record that contains an address for jsonrpc.
    /// This represents an address that can be either  UInt160 or Base58Check format when specifying a JSON-RPC method.
    /// </summary>
    /// <param name="ScriptHash">The script hash of the address.</param>
    /// <param name="AddressVersion">The address version of the address.</param>
    public record struct Address(UInt160 ScriptHash, byte AddressVersion);
}
