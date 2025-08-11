// Copyright (C) 2015-2025 The Neo Project.
//
// NetworkMetrics.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Plugins.OpenTelemetry
{
    /// <summary>
    /// Network metrics snapshot
    /// </summary>
    public class NetworkMetrics
    {
        public DateTime Timestamp { get; set; }
        public int ConnectedPeers { get; set; }
        public int UnconnectedPeers { get; set; }
    }
}
