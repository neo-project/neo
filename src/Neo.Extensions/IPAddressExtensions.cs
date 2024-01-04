// Copyright (C) 2015-2024 The Neo Project.
//
// IPAddressExtensions.cs file belongs to the neo project and is free
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
    public static class IPAddressExtensions
    {
        /// <summary>
        /// Checks if address is IPv4 Mapped to IPv6 format, if so, Map to IPv4.
        /// Otherwise, return current address.
        /// </summary>
        internal static IPAddress UnMap(this IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            return address;
        }
    }
}
