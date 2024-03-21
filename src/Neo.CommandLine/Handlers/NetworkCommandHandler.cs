// Copyright (C) 2015-2024 The Neo Project.
//
// NetworkCommandHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Net;

namespace Neo.CommandLine.Handlers
{
    internal static class NetworkCommandHandler
    {
        public static void OnBroadcastAddress(IPAddress ipAddress, ushort port)
        {
            Console.WriteLine("Exec OnNetworkBroadcastAddress");
        }

        public static void OnBroadcastBlock(string blockIndexOrHash)
        {
            Console.WriteLine("Exec OnNetworkBroadcastBlock");
        }

        public static void OnBroadcastGetHeader(uint blockIndex)
        {
            Console.WriteLine("Exec OnNetworkBroadcastGetHeader");
        }
    }
}
