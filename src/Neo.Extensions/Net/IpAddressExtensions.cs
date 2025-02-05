// Copyright (C) 2015-2025 The Neo Project.
//
// IpAddressExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Net;

namespace Neo.Extensions
{
    public static class IpAddressExtensions
    {
        /// <summary>
        /// Checks if address is IPv4 Mapped to IPv6 format, if so, Map to IPv4.
        /// Otherwise, return current address.
        /// </summary>
        public static IPAddress UnMap(this IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            return address;
        }

        /// <summary>
        /// Checks if IPEndPoint is IPv4 Mapped to IPv6 format, if so, unmap to IPv4.
        /// Otherwise, return current endpoint.
        /// </summary>
        public static IPEndPoint UnMap(this IPEndPoint endPoint)
        {
            if (!endPoint.Address.IsIPv4MappedToIPv6)
                return endPoint;
            return new IPEndPoint(endPoint.Address.UnMap(), endPoint.Port);
        }
    }
}
