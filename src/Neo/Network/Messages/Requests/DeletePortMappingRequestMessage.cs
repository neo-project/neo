// Copyright (C) 2015-2025 The Neo Project.
//
// DeletePortMappingRequestMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.Network.Messages.Requests
{
    internal class DeletePortMappingRequestMessage : RequestMessage
    {
        private readonly string _publicPort;

        public DeletePortMappingRequestMessage(
            string publicPort)
        {
            _publicPort = publicPort;
        }

        public override Dictionary<string, object> ToXml() =>
            new()
            {
                {"NewRemoteHost", string.Empty},
                {"NewExternalPort", _publicPort},
                {"NewProtocol", "TCP"},
            };
    }
}
