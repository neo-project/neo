// Copyright (C) 2015-2025 The Neo Project.
//
// SecurityConstants.cs file belongs to the neo project and is free
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
    /// Centralized constants for the Neo plugin security system.
    /// </summary>
    public static class SecurityConstants
    {
        /// <summary>
        /// Cache-related constants.
        /// </summary>
        public static class Cache
        {
            /// <summary>
            /// Default timeout for permission cache entries in minutes.
            /// </summary>
            public const int PermissionCacheTimeoutMinutes = 5;

            /// <summary>
            /// Default timeout for policy cache entries in minutes.
            /// </summary>
            public const int PolicyCacheTimeoutMinutes = 10;

            /// <summary>
            /// Maximum number of entries in cache before cleanup.
            /// </summary>
            public const int MaxCacheSize = 1000;

            /// <summary>
            /// Interval between cache cleanup operations in minutes.
            /// </summary>
            public const int CacheCleanupIntervalMinutes = 2;

            /// <summary>
            /// High performance cache size for optimized scenarios.
            /// </summary>
            public const int HighPerformanceCacheSize = 1000;

            /// <summary>
            /// Low latency cache size for minimal memory usage.
            /// </summary>
            public const int LowLatencyCacheSize = 500;

            /// <summary>
            /// Default cache size for standard scenarios.
            /// </summary>
            public const int DefaultCacheSize = 100;
        }

        /// <summary>
        /// Memory-related constants in bytes.
        /// </summary>
        public static class Memory
        {
            /// <summary>
            /// Default maximum memory usage per plugin (256 MB).
            /// </summary>
            public const long DefaultMaxMemoryBytes = 256L * 1024 * 1024;

            /// <summary>
            /// Restrictive memory limit for untrusted plugins (64 MB).
            /// </summary>
            public const long RestrictiveMaxMemoryBytes = 64L * 1024 * 1024;

            /// <summary>
            /// Permissive memory limit for trusted plugins (1 GB).
            /// </summary>
            public const long PermissiveMaxMemoryBytes = 1024L * 1024 * 1024;

            /// <summary>
            /// Minimum memory limit to prevent extremely low allocations (1 MB).
            /// </summary>
            public const long MinMemoryLimitBytes = 1024 * 1024;

            /// <summary>
            /// No-GC region size for high performance scenarios (1 MB).
            /// </summary>
            public const int NoGcRegionSizeBytes = 1024 * 1024;

            /// <summary>
            /// Memory threshold for low latency mode (2 GB).
            /// </summary>
            public const long LowLatencyMemoryThresholdBytes = 2L * 1024 * 1024 * 1024;

            /// <summary>
            /// Fallback total system memory estimate (1 GB).
            /// </summary>
            public const long FallbackTotalMemoryBytes = 1024L * 1024 * 1024;
        }

        /// <summary>
        /// CPU and performance constants.
        /// </summary>
        public static class Performance
        {
            /// <summary>
            /// Default maximum CPU usage percentage per plugin.
            /// </summary>
            public const double DefaultMaxCpuPercent = 25.0;

            /// <summary>
            /// Restrictive CPU limit for untrusted plugins.
            /// </summary>
            public const double RestrictiveMaxCpuPercent = 10.0;

            /// <summary>
            /// Permissive CPU limit for trusted plugins.
            /// </summary>
            public const double PermissiveMaxCpuPercent = 50.0;

            /// <summary>
            /// CPU warning threshold (70%).
            /// </summary>
            public const double CpuWarningThreshold = 0.7;

            /// <summary>
            /// Memory warning threshold (80%).
            /// </summary>
            public const double MemoryWarningThreshold = 0.8;

            /// <summary>
            /// Minimum processor count for high performance mode.
            /// </summary>
            public const int HighPerformanceMinProcessorCount = 8;

            /// <summary>
            /// Multiplier for calculating max concurrent operations based on processor count.
            /// </summary>
            public const int MaxConcurrentOperationsMultiplier = 2;
        }

        /// <summary>
        /// Thread-related constants.
        /// </summary>
        public static class Threading
        {
            /// <summary>
            /// Default maximum number of threads per plugin.
            /// </summary>
            public const int DefaultMaxThreads = 10;

            /// <summary>
            /// Restrictive thread limit for untrusted plugins.
            /// </summary>
            public const int RestrictiveMaxThreads = 5;

            /// <summary>
            /// Permissive thread limit for trusted plugins.
            /// </summary>
            public const int PermissiveMaxThreads = 20;
        }

        /// <summary>
        /// Timeout constants in seconds.
        /// </summary>
        public static class Timeouts
        {
            /// <summary>
            /// Default execution timeout per plugin operation.
            /// </summary>
            public const int DefaultExecutionTimeoutSeconds = 300;

            /// <summary>
            /// Restrictive execution timeout for untrusted plugins.
            /// </summary>
            public const int RestrictiveExecutionTimeoutSeconds = 60;

            /// <summary>
            /// Permissive execution timeout for trusted plugins.
            /// </summary>
            public const int PermissiveExecutionTimeoutSeconds = 600;

            /// <summary>
            /// Default coordination timeout for thread-safe operations.
            /// </summary>
            public const int DefaultCoordinationTimeoutSeconds = 30;

            /// <summary>
            /// Exclusive access timeout for critical sections.
            /// </summary>
            public const int ExclusiveAccessTimeoutSeconds = 60;

            /// <summary>
            /// Processing task wait timeout.
            /// </summary>
            public const int ProcessingTaskWaitTimeoutSeconds = 5;

            /// <summary>
            /// High performance resource cache timeout.
            /// </summary>
            public const int HighPerformanceResourceCacheTimeoutSeconds = 30;

            /// <summary>
            /// Default resource cache timeout.
            /// </summary>
            public const int DefaultResourceCacheTimeoutSeconds = 10;
        }

        /// <summary>
        /// Monitoring interval constants in milliseconds.
        /// </summary>
        public static class Monitoring
        {
            /// <summary>
            /// High performance minimum monitoring interval.
            /// </summary>
            public const int HighPerformanceMinIntervalMs = 10000;

            /// <summary>
            /// Low latency minimum monitoring interval.
            /// </summary>
            public const int LowLatencyMinIntervalMs = 30000;

            /// <summary>
            /// Default minimum monitoring interval.
            /// </summary>
            public const int DefaultMinIntervalMs = 5000;

            /// <summary>
            /// Resource check interval for policies.
            /// </summary>
            public const int ResourceCheckIntervalMs = 5000;

            /// <summary>
            /// Container minimum monitoring interval.
            /// </summary>
            public const int ContainerMinIntervalMs = 2000;

            /// <summary>
            /// Minimum acceptable monitoring check interval.
            /// </summary>
            public const int MinMonitoringCheckIntervalMs = 100;
        }

        /// <summary>
        /// File system constants.
        /// </summary>
        public static class FileSystem
        {
            /// <summary>
            /// Default maximum file size in bytes (100 MB).
            /// </summary>
            public const long DefaultMaxFileSizeBytes = 100L * 1024 * 1024;

            /// <summary>
            /// Default maximum total number of files.
            /// </summary>
            public const int DefaultMaxTotalFiles = 1000;
        }

        /// <summary>
        /// Network-related constants.
        /// </summary>
        public static class Network
        {
            /// <summary>
            /// Default allowed network ports for plugins.
            /// </summary>
            public static readonly int[] DefaultAllowedPorts = { 80, 443, 10332, 10333, 20332, 20333 };

            /// <summary>
            /// Default maximum concurrent network connections.
            /// </summary>
            public const int DefaultMaxConnections = 10;

            /// <summary>
            /// SMB port number.
            /// </summary>
            public const int SmbPort = 445;

            /// <summary>
            /// RPC port number.
            /// </summary>
            public const int RpcPort = 135;
        }

        /// <summary>
        /// Event and logging constants.
        /// </summary>
        public static class Events
        {
            /// <summary>
            /// Maximum number of events to keep in memory.
            /// </summary>
            public const int MaxEventsInMemory = 10000;

            /// <summary>
            /// Event flush interval in seconds.
            /// </summary>
            public const int EventFlushIntervalSeconds = 30;

            /// <summary>
            /// Event cleanup batch ratio (1/10 of max events).
            /// </summary>
            public const int EventCleanupBatchRatio = 10;

            /// <summary>
            /// Maximum resource data points to keep.
            /// </summary>
            public const int MaxResourceDataPoints = 1000;

            /// <summary>
            /// Maximum resource snapshots in history.
            /// </summary>
            public const int MaxSnapshotsHistory = 60;
        }

        /// <summary>
        /// State management constants.
        /// </summary>
        public static class StateManagement
        {
            /// <summary>
            /// State cleanup interval in minutes.
            /// </summary>
            public const int StateCleanupIntervalMinutes = 10;

            /// <summary>
            /// Expired state threshold in minutes.
            /// </summary>
            public const int ExpiredStateThresholdMinutes = 30;
        }

        /// <summary>
        /// Version constants.
        /// </summary>
        public static class Versions
        {
            /// <summary>
            /// Minimum supported .NET version major number.
            /// </summary>
            public const int MinDotNetVersionMajor = 5;
        }

        /// <summary>
        /// Conversion constants.
        /// </summary>
        public static class Conversion
        {
            /// <summary>
            /// Multiplier to convert seconds to milliseconds.
            /// </summary>
            public const int MillisecondsPerSecond = 1000;
        }
    }
}