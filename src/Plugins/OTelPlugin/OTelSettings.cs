// Copyright (C) 2015-2025 The Neo Project.
//
// OTelSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Plugins;
using System;
using System.Reflection;

namespace Neo.Plugins.OpenTelemetry
{
    public class OTelSettings
    {
        public bool Enabled { get; init; }
        public string ServiceName { get; init; }
        public string ServiceVersion { get; init; }
        public string InstanceId { get; init; }
        public UnhandledExceptionPolicy UnhandledExceptionPolicy { get; init; }
        public MetricsSettings Metrics { get; init; }
        public TracesSettings Traces { get; init; }
        public LogsSettings Logs { get; init; }
        public OtlpExporterSettings OtlpExporter { get; init; }

        public OTelSettings(IConfigurationSection section)
        {
            Enabled = section.GetValue("Enabled", true);
            ServiceName = section.GetValue("ServiceName", OTelConstants.DefaultServiceName);
            ServiceVersion = section.GetValue("ServiceVersion", OTelSettingsExtensions.GetDefaultServiceVersion());
            InstanceId = section.GetValue("InstanceId", string.Empty);

            var policyString = section.GetValue("UnhandledExceptionPolicy", "StopPlugin");
            UnhandledExceptionPolicy = Enum.TryParse<UnhandledExceptionPolicy>(policyString, out var policy)
                ? policy
                : UnhandledExceptionPolicy.StopPlugin;

            Metrics = new MetricsSettings(section.GetSection("Metrics"));
            Traces = new TracesSettings(section.GetSection("Traces"));
            Logs = new LogsSettings(section.GetSection("Logs"));
            OtlpExporter = new OtlpExporterSettings(section.GetSection("OtlpExporter"));
        }

        public static OTelSettings Default => new(new ConfigurationBuilder().Build().GetSection("Empty"));
    }

    public class TracesSettings
    {
        public bool Enabled { get; init; }
        public ConsoleExporterSettings ConsoleExporter { get; init; }

        public TracesSettings(IConfigurationSection section)
        {
            Enabled = section.GetValue("Enabled", false);
            ConsoleExporter = new ConsoleExporterSettings(section.GetSection("ConsoleExporter"));
        }
    }

    public class LogsSettings
    {
        public bool Enabled { get; init; }
        public ConsoleExporterSettings ConsoleExporter { get; init; }

        public LogsSettings(IConfigurationSection section)
        {
            Enabled = section.GetValue("Enabled", false);
            ConsoleExporter = new ConsoleExporterSettings(section.GetSection("ConsoleExporter"));
        }
    }

    public class PrometheusExporterSettings
    {
        public bool Enabled { get; init; }
        public int Port { get; init; }
        public string Path { get; init; }

        public PrometheusExporterSettings(IConfigurationSection section)
        {
            Enabled = section.GetValue("Enabled", false);
            Port = section.GetValue("Port", OTelConstants.DefaultPrometheusPort);
            Path = section.GetValue("Path", OTelConstants.DefaultPath);

            // Validate port
            if (Port < 1 || Port > 65535)
                throw new ArgumentException($"Invalid Prometheus port: {Port}. Must be between 1 and 65535.");
        }
    }

    public class ConsoleExporterSettings
    {
        public bool Enabled { get; init; }

        public ConsoleExporterSettings(IConfigurationSection section)
        {
            Enabled = section.GetValue("Enabled", false);
        }
    }

    public class OtlpExporterSettings
    {
        public bool Enabled { get; init; }
        public string Endpoint { get; init; }
        public string Protocol { get; init; }
        public int Timeout { get; init; }
        public string Headers { get; init; }
        public bool ExportMetrics { get; init; }
        public bool ExportTraces { get; init; }
        public bool ExportLogs { get; init; }

        public OtlpExporterSettings(IConfigurationSection section)
        {
            Enabled = section.GetValue("Enabled", false);
            Endpoint = section.GetValue("Endpoint", OTelConstants.DefaultEndpoint);
            Protocol = section.GetValue("Protocol", OTelConstants.ProtocolGrpc);
            Timeout = section.GetValue("Timeout", OTelConstants.DefaultTimeout);
            Headers = section.GetValue("Headers", string.Empty);
            ExportMetrics = section.GetValue("ExportMetrics", true);
            ExportTraces = section.GetValue("ExportTraces", false);
            ExportLogs = section.GetValue("ExportLogs", false);

            // Validate endpoint
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri))
                throw new ArgumentException($"Invalid OTLP endpoint: {Endpoint}");

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException($"OTLP endpoint must use HTTP or HTTPS scheme: {Endpoint}");

            // Validate protocol
            if (Protocol != OTelConstants.ProtocolGrpc && Protocol != OTelConstants.ProtocolHttpProtobuf)
                throw new ArgumentException($"Invalid OTLP protocol: {Protocol}. Must be '{OTelConstants.ProtocolGrpc}' or '{OTelConstants.ProtocolHttpProtobuf}'.");

            // Validate timeout
            if (Timeout < 0)
                throw new ArgumentException($"Invalid OTLP timeout: {Timeout}. Must be non-negative.");

            // Sanitize headers
            Headers = SanitizeHeaders(Headers);
        }

        private static string SanitizeHeaders(string headers)
        {
            // Remove any potential injection attempts
            return headers?.Replace('\n', ' ').Replace('\r', ' ') ?? string.Empty;
        }
    }

    internal static class OTelSettingsExtensions
    {
        internal static string GetDefaultServiceVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
    }
}
