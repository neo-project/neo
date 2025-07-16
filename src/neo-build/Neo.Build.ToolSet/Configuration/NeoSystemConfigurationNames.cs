// Copyright (C) 2015-2025 The Neo Project.
//
// NeoSystemConfigurationNames.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.ToolSet.Configuration
{
    internal static class NeoSystemConfigurationNames
    {
        // Peer to Peer Configuration
        public static readonly string ListenKey = "NETWORK:LISTEN";
        public static readonly string PortKey = "NETWORK:PORT";
        public static readonly string MinDesiredConnectionsKey = "NETWORK:MINDESIREDCONNECTIONS";
        public static readonly string MaxConnectionsKey = "NETWORK:MAXCONNECTIONS";
        public static readonly string MaxConnectionsPerAddressKey = "NETWORK:MAXCONNECTIONSPERADDRESS";
        public static readonly string EnableCompressionKey = "NETWORK:ENABLECOMPRESSION";

        // Storage Configuration
        public static readonly string StoreRootKey = "STORAGE:STOREROOT";
        public static readonly string CheckpointRootKey = "STORAGE:CHECKPOINTROOT";
    }
}
