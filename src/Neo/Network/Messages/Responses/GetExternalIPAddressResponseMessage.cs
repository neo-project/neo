// Copyright (C) 2015-2025 The Neo Project.
//
// GetExternalIPAddressResponseMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.Xml;
using System.Net;
using System.Xml;

namespace Neo.Network.Messages.Responses
{
    internal class GetExternalIPAddressResponseMessage : ResponseMessage
    {
        public IPAddress ExternalIPAddress { get; private set; }

        public GetExternalIPAddressResponseMessage(XmlDocument response, string serviceType)
            : base(response, serviceType, nameof(GetExternalIPAddressResponseMessage))
        {
            var ip = GetNode().GetXmlElementText("NewExternalIPAddress");

            if (IPAddress.TryParse(ip, out var ipAddress))
                ExternalIPAddress = ipAddress;
        }
    }
}
