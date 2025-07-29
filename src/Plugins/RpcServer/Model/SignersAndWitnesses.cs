// Copyright (C) 2015-2025 The Neo Project.
//
// SignersAndWitnesses.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;

namespace Neo.Plugins.RpcServer.Model
{
    /// <summary>
    /// A record that contains signers and witnesses for jsonrpc.
    /// This represents a list of signers that may contain witness info when specifying a JSON-RPC method.
    /// <see cref="ParameterConverter.ToSignersAndWitnesses"/>
    /// </summary>
    /// <param name="Signers">The signers to be used in the transaction.</param>
    /// <param name="Witnesses">The witnesses to be used in the transaction.</param>
    public record struct SignersAndWitnesses(Signer[] Signers, Witness[] Witnesses);
}
