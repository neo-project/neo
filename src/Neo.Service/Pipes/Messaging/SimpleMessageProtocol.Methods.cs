// Copyright (C) 2015-2024 The Neo Project.
//
// SimpleMessageProtocol.Methods.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO.Pipes.Protocols.Payloads;
using Neo.SmartContract.Native;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Neo.Service.Pipes.Messaging
{
    internal partial class SimpleMessageProtocol
    {
        public ServerInfoPayload GetServerInfo()
        {
            var height = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);
            var payload = new ServerInfoPayload()
            {
                Address = IPAddress.Parse(_options.P2P.Listen),
                Port = _options.P2P.Port,
                BlockHeight = height,
                HeaderHeight = _neoSystem.HeaderCache?.Last?.Index ?? height,
                Version = (uint)Assembly.GetExecutingAssembly().GetVersionNumber(),
                RemoteNodes = [.. _localNode.GetRemoteNodes().Select(s =>
                    new ServerInfoPayload.RemoteConnectedClient()
                    {
                        Address = s.Remote.Address,
                        Port = (ushort)s.Remote.Port,
                        LastBlockIndex = s.LastBlockIndex,
                    })],
            };
            return payload;
        }
    }
}
