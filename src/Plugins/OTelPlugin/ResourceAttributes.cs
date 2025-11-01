// Copyright (C) 2015-2025 The Neo Project.
//
// ResourceAttributes.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.OpenTelemetry
{
    /// <summary>
    /// Standard resource attributes for Neo blockchain telemetry
    /// </summary>
    public static class NeoResourceAttributes
    {
        /// <summary>
        /// Build comprehensive resource attributes for the Neo node
        /// </summary>
        public static ResourceBuilder BuildNeoResource(OTelSettings settings, NeoSystem? system)
        {
            var instanceId = string.IsNullOrWhiteSpace(settings.InstanceId)
                ? Environment.MachineName
                : settings.InstanceId;

            var attributes = new Dictionary<string, object>
            {
                // Standard OpenTelemetry semantic conventions
                ["service.name"] = settings.ServiceName,
                ["service.version"] = settings.ServiceVersion,
                ["service.instance.id"] = instanceId,
                ["service.namespace"] = "neo-blockchain",

                // Deployment environment
                ["deployment.environment"] = GetEnvironment(),

                // Host information
                ["host.name"] = Environment.MachineName,
                ["host.arch"] = Environment.Is64BitOperatingSystem ? "amd64" : "x86",
                ["host.type"] = GetHostType(),

                // Operating System
                ["os.type"] = GetOSType(),
                ["os.description"] = Environment.OSVersion.ToString(),

                // Process information
                ["process.pid"] = Environment.ProcessId,
                ["process.executable.name"] = "neo-cli",
                ["process.runtime.name"] = ".NET",
                ["process.runtime.version"] = Environment.Version.ToString(),
                ["process.command_line"] = Environment.CommandLine,

                // Neo-specific attributes
                ["neo.network"] = GetNetworkName(system),
                ["neo.network.id"] = GetNetworkId(system),
                ["neo.protocol.version"] = GetProtocolVersion(system),
                ["neo.node.type"] = GetNodeType(system),
                ["neo.consensus.enabled"] = IsConsensusNode(system).ToString().ToLower(),

                // Cloud/Container detection
                ["cloud.provider"] = DetectCloudProvider() ?? "none",
                ["container.runtime"] = DetectContainerRuntime() ?? "none",

                // Custom labels for filtering and grouping
                ["neo.deployment.region"] = Environment.GetEnvironmentVariable("NEO_REGION") ?? "unknown",
                ["neo.deployment.datacenter"] = Environment.GetEnvironmentVariable("NEO_DATACENTER") ?? "unknown",
                ["neo.deployment.cluster"] = Environment.GetEnvironmentVariable("NEO_CLUSTER") ?? "default"
            };

            var resourceBuilder = ResourceBuilder.CreateEmpty();

            // Add all attributes, filtering out nulls
            foreach (var attr in attributes.Where(a => a.Value != null))
            {
                resourceBuilder.AddAttributes(new[] { new KeyValuePair<string, object>(attr.Key, attr.Value) });
            }

            return resourceBuilder;
        }

        private static string GetEnvironment()
        {
            return Environment.GetEnvironmentVariable("NEO_ENV")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "production";
        }

        private static string GetOSType()
        {
            if (OperatingSystem.IsWindows()) return "windows";
            if (OperatingSystem.IsLinux()) return "linux";
            if (OperatingSystem.IsMacOS()) return "darwin";
            return "unknown";
        }

        private static string GetHostType()
        {
            // Detect if running in container or VM
            if (IsRunningInContainer()) return "container";
            if (IsRunningInVM()) return "vm";
            return "physical";
        }

        private static string GetNetworkName(NeoSystem? system)
        {
            if (system == null) return "unknown";

            return $"network-{system.Settings.Network}";
        }

        private static int GetNetworkId(NeoSystem? system)
        {
            return (int)(system?.Settings.Network ?? 0);
        }

        private static string GetProtocolVersion(NeoSystem? system)
        {
            if (system == null) return "unknown";

            // Neo doesn't expose protocol version directly, use a placeholder
            // This could be enhanced if Neo exposes this information
            return "3.0";
        }

        private static string GetNodeType(NeoSystem? system)
        {
            if (system == null) return "unknown";

            // Determine node type based on configuration
            if (IsConsensusNode(system)) return "consensus";
            if (IsRpcNode(system)) return "rpc";
            return "relay";
        }

        private static bool IsConsensusNode(NeoSystem? system)
        {
            // Check if node is configured as consensus node
            // This would need actual implementation based on Neo's consensus configuration
            return false;
        }

        private static bool IsRpcNode(NeoSystem? system)
        {
            // Check if RPC server is enabled
            // This would need actual implementation based on RPC plugin configuration
            return Environment.GetEnvironmentVariable("NEO_RPC_ENABLED") == "true";
        }

        private static string? DetectCloudProvider()
        {
            // AWS
            if (Environment.GetEnvironmentVariable("AWS_EXECUTION_ENV") != null ||
                Environment.GetEnvironmentVariable("AWS_REGION") != null)
                return "aws";

            // Azure
            if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") != null ||
                Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") != null)
                return "azure";

            // GCP
            if (Environment.GetEnvironmentVariable("GCP_PROJECT") != null ||
                Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") != null)
                return "gcp";

            return null;
        }

        private static string? DetectContainerRuntime()
        {
            if (IsRunningInDocker()) return "docker";
            if (IsRunningInKubernetes()) return "kubernetes";
            return null;
        }

        private static bool IsRunningInContainer()
        {
            return IsRunningInDocker() || IsRunningInKubernetes();
        }

        private static bool IsRunningInDocker()
        {
            return System.IO.File.Exists("/.dockerenv") ||
                   Environment.GetEnvironmentVariable("DOCKER_CONTAINER") == "true";
        }

        private static bool IsRunningInKubernetes()
        {
            return Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") != null;
        }

        private static bool IsRunningInVM()
        {
            // Simple heuristic - can be improved
            try
            {
                var manufacturer = Environment.GetEnvironmentVariable("CHASSIS_VENDOR");
                if (manufacturer != null)
                {
                    manufacturer = manufacturer.ToLower();
                    return manufacturer.Contains("vmware") ||
                           manufacturer.Contains("virtualbox") ||
                           manufacturer.Contains("kvm") ||
                           manufacturer.Contains("qemu");
                }
            }
            catch { }

            return false;
        }
    }
}
