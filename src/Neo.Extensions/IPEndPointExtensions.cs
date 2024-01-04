// Copyright (C) 2015-2024 The Neo Project.
//
// IPEndPointExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Net;

namespace Neo.Extensions
{
    public static class IPEndPointExtensions
    {
        /// <summary>
        /// Checks if IPEndPoint is IPv4 Mapped to IPv6 format, if so, unmap to IPv4.
        /// Otherwise, return current endpoint.
        /// </summary>
        internal static IPEndPoint UnMap(this IPEndPoint endPoint)
        {
            if (!endPoint.Address.IsIPv4MappedToIPv6)
                return endPoint;
            return new IPEndPoint(endPoint.Address.UnMap(), endPoint.Port);
        }
    }
}
