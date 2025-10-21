// Copyright (C) 2015-2025 The Neo Project.
//
// CreatePortMappingRequestMessage.cs file belongs to the neo project and is free
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
    internal class CreatePortMappingRequestMessage : RequestMessage
    {
        private readonly string _remoteHost;
        private readonly string _publicPort;
        private readonly string _privatePort;
        private readonly string _privateIp;
        private readonly string _description;

        public CreatePortMappingRequestMessage(
            string remoteHost,
            string publicPort,
            string privatePort,
            string privateIp,
            string description)
        {
            _remoteHost = remoteHost;
            _publicPort = publicPort;
            _privatePort = privatePort;
            _privateIp = privateIp;
            _description = description;
        }

        public override Dictionary<string, object> ToXml() =>
            new()
            {
                {"NewRemoteHost", _remoteHost},
                {"NewExternalPort", _publicPort},
                {"NewProtocol", "TCP"},
                {"NewInternalPort", _privatePort},
                {"NewInternalClient", _privateIp},
                {"NewEnabled", 1},
                {"NewPortMappingDescription", _description},
                {"NewLeaseDuration", 0},
            };
    }
}
