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
        public static readonly string ListenKey = "NEOSYSTEM:NETWORK:LISTEN";
        public static readonly string PortKey = "NEOSYSTEM:NETWORK:PORT";
        public static readonly string MinDesiredConnectionsKey = "NEOSYSTEM:NETWORK:MINDESIREDCONNECTIONS";
        public static readonly string MaxConnectionsKey = "NEOSYSTEM:NETWORK:MAXCONNECTIONS";
        public static readonly string MaxConnectionsPerAddressKey = "NEOSYSTEM:NETWORK:MAXCONNECTIONSPERADDRESS";
        public static readonly string EnableCompressionKey = "NEOSYSTEM:NETWORK:ENABLECOMPRESSION";
    }
}
