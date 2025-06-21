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
    internal sealed class NeoSystemOptions
    {
        public required NeoSystemNetworkOptions Network { get; set; }
        public required NeoSystemStorageOptions Storage { get; set; }
    }

    internal sealed class NeoSystemNetworkOptions
    {
        public required IPAddress Listen { get; set; }
        public required ushort Port { get; set; }
        public required int MinDesiredConnections { get; set; }
        public required int MaxConnections { get; set; }
        public required int MaxConnectionsPerAddress { get; set; }
        public required bool EnableCompression { get; set; }
    }

    internal sealed class NeoSystemStorageOptions
    {
        public required string StoreRoot { get; set; }
        public required string CheckPointRoot { get; set; }
    }
}
