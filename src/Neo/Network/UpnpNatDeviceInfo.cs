// Copyright (C) 2015-2025 The Neo Project.
//
// UpnpNatDeviceInfo.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Net;

namespace Neo.Network
{
    internal class UpnpNatDeviceInfo
    {
        public UpnpNatDeviceInfo(IPAddress localAddress, Uri locationUri, string serviceControlUrl, string serviceType)
        {
            LocalAddress = localAddress;
            ServiceType = serviceType;
            HostEndPoint = new IPEndPoint(IPAddress.Parse(locationUri.Host), locationUri.Port);

            if (Uri.IsWellFormedUriString(serviceControlUrl, UriKind.Absolute))
                serviceControlUrl = new Uri(serviceControlUrl).PathAndQuery;

            var builder = new UriBuilder("http", locationUri.Host, locationUri.Port);
            ServiceControlUri = new Uri(builder.Uri, serviceControlUrl); ;
        }

        public IPEndPoint HostEndPoint { get; private set; }
        public IPAddress LocalAddress { get; private set; }
        public string ServiceType { get; private set; }
        public Uri ServiceControlUri { get; private set; }
    }
}
