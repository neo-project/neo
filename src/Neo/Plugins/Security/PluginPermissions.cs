// Copyright (C) 2015-2025 The Neo Project.
//
// PluginPermissions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Defines the permissions that can be granted to plugins.
    /// </summary>
    [Flags]
    public enum PluginPermissions : uint
    {
        /// <summary>
        /// No permissions granted.
        /// </summary>
        None = 0,

        /// <summary>
        /// Basic read-only access to blockchain data.
        /// </summary>
        ReadOnly = 1,

        /// <summary>
        /// Access to write blockchain data and submit transactions.
        /// </summary>
        StorageAccess = 2,

        /// <summary>
        /// Network communication capabilities.
        /// </summary>
        NetworkAccess = 4,

        /// <summary>
        /// File system access within allowed paths.
        /// </summary>
        FileSystemAccess = 8,

        /// <summary>
        /// RPC server functionality.
        /// </summary>
        RpcPlugin = 16,

        /// <summary>
        /// Advanced system operations.
        /// </summary>
        SystemAccess = 32,

        /// <summary>
        /// Cryptographic operations.
        /// </summary>
        CryptographicAccess = 64,

        /// <summary>
        /// Process creation and management.
        /// </summary>
        ProcessAccess = 128,

        /// <summary>
        /// Registry access (Windows only).
        /// </summary>
        RegistryAccess = 256,

        /// <summary>
        /// Service management access.
        /// </summary>
        ServiceAccess = 512,

        /// <summary>
        /// Database access.
        /// </summary>
        DatabaseAccess = 1024,

        /// <summary>
        /// Consensus participation.
        /// </summary>
        ConsensusAccess = 2048,

        /// <summary>
        /// Wallet operations.
        /// </summary>
        WalletAccess = 4096,

        /// <summary>
        /// Oracle service access.
        /// </summary>
        OracleAccess = 8192,

        /// <summary>
        /// State service access.
        /// </summary>
        StateAccess = 16384,

        /// <summary>
        /// Application logs access.
        /// </summary>
        LogAccess = 32768,

        /// <summary>
        /// Memory debugging and profiling.
        /// </summary>
        DebuggingAccess = 65536,

        /// <summary>
        /// HTTP/HTTPS network access only.
        /// </summary>
        HttpsOnly = 131072,

        /// <summary>
        /// Administrative operations.
        /// </summary>
        AdminAccess = 262144,

        /// <summary>
        /// Plugin can load other plugins.
        /// </summary>
        PluginLoaderAccess = 524288,

        // Predefined permission sets
        /// <summary>
        /// Standard plugin permissions (ReadOnly + NetworkAccess).
        /// </summary>
        NetworkPlugin = ReadOnly | NetworkAccess,

        /// <summary>
        /// Service plugin permissions (ReadOnly + StorageAccess + NetworkAccess).
        /// </summary>
        ServicePlugin = ReadOnly | StorageAccess | NetworkAccess,

        /// <summary>
        /// Administrative plugin permissions (most permissions except dangerous ones).
        /// </summary>
        AdminPlugin = ReadOnly | StorageAccess | NetworkAccess | FileSystemAccess |
                     RpcPlugin | CryptographicAccess | DatabaseAccess | LogAccess,

        /// <summary>
        /// Full system access (all permissions - use with extreme caution).
        /// </summary>
        FullAccess = 0xFFFFFFFF
    }
}
