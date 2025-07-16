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
        public static bool IsLocalnet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.Localnet);
        }

        public static bool IsTestnet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.Testnet);
        }

        public static bool IsMainnet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.Mainnet);
        }

        public static bool IsPrivatenet(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment, nameof(hostEnvironment));
            return hostEnvironment.IsEnvironment(NeoHostingEnvironments.Privatenet);
        }
    }
}
