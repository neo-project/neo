// Copyright (C) 2015-2024 The Neo Project.
//
// IHostEnvironmentExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using Neo.Hosting.App.Host;
using System;

namespace Neo.Hosting.App.Extensions
{
    internal static class IHostEnvironmentExtensions
    {
        public static bool IsNeoLocalNet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.LocalNet);
        }

        public static bool IsNeoTestNet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.TestNet);
        }

        public static bool IsNeoMainNet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.MainNet);
        }

        public static bool IsNeoPrivateNet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.PrivateNet);
        }
    }
}
