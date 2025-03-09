// Copyright (C) 2015-2025 The Neo Project.
//
// NeoSystemDefaults.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.ToolSet.Services
{
    internal static class NeoSystemDefaults
    {
        public static readonly string ListenKey = "P2P:LISTEN";
        public static readonly string PortKey = "P2P:PORT";
        public static readonly string MinDesiredConnectionsKey = "P2P:MINDESIREDCONNECTIONS";
        public static readonly string MaxConnectionsKey = "P2P:MAXCONNECTIONS";
        public static readonly string MaxConnectionsPerAddressKey = "P2P:MAXCONNECTIONSPERADDRESS";
        public static readonly string EnableCompressionKey = "P2P:ENABLECOMPRESSION";
    }
}
