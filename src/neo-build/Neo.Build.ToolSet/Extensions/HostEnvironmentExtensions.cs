// Copyright (C) 2015-2025 The Neo Project.
//
// HostEnvironmentExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using System;

namespace Neo.Build.ToolSet.Extensions
{
    internal static class HostEnvironmentExtensions
    {
        public static bool IsLocalNet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.LocalNet);
        }

        public static bool IsTestNet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.TestNet);
        }

        public static bool IsMainNet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.MainNet);
        }

        public static bool IsPrivateNet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.PrivateNet);
        }
    }
}
