// Copyright (C) 2015-2024 The Neo Project.
//
// TestP2PSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.DBFTPlugin.Tests;

public class TestP2PSettings
{
    public ushort Port { get; init; }
    public int MinDesiredConnections { get; } = 5;
    public int MaxConnections { get; } = 20;
    public int MaxConnectionsPerAddress { get; } = 10;

    public static readonly TestP2PSettings Node1 = new()
    {
        Port = 30333
    };

    public static readonly TestP2PSettings Node2 = new()
    {
        Port = 30334
    };

    public static readonly TestP2PSettings Node3 = new()
    {
        Port = 30335
    };

    public static readonly TestP2PSettings Node4 = new()
    {
        Port = 30336
    };

    public static readonly TestP2PSettings Node5 = new()
    {
        Port = 30337
    };

    public static readonly TestP2PSettings Node6 = new()
    {
        Port = 30338
    };

    public static readonly TestP2PSettings Node7 = new()
    {
        Port = 30339
    };
}
