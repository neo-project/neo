// Copyright (C) 2015-2025 The Neo Project.
//
// NeoSystemOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Net;

namespace Neo.Build.ToolSet.Options
{
    internal sealed class NeoSystemNetworkOptions
    {
        public IPAddress Listen { get; set; } = IPAddress.Loopback;
        public ushort Port { get; set; }
        public int MinDesiredConnections { get; set; }
        public int MaxConnections { get; set; }
        public int MaxConnectionsPerAddress { get; set; }
        public bool EnableCompression { get; set; }
    }

    internal sealed class NeoSystemStorageOptions
    {
        public string StoreRoot { get; set; } = string.Empty;
        public string CheckPointRoot { get; set; } = string.Empty;
    }
}
