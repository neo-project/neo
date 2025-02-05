// Copyright (C) 2015-2025 The Neo Project.
//
// IMessageReceivedHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P;

namespace Neo.IEventHandlers
{
    public interface IMessageReceivedHandler
    {
        /// <summary>
        /// The handler of MessageReceived event from <see cref="RemoteNode"/>
        /// Triggered when a new message is received from a peer <see cref="RemoteNode"/>
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object</param>
        /// <param name="message"> The current node received <see cref="Message"/> from a peer <see cref="RemoteNode"/></param>
        bool RemoteNode_MessageReceived_Handler(NeoSystem system, Message message);
    }
}
