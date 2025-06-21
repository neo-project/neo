// Copyright (C) 2015-2025 The Neo Project.
//
// Settings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;

namespace Neo.Plugins.SignClient
{
    public class Settings : PluginSettings
    {
        public const string SectionName = "PluginConfiguration";
        private const string DefaultEndpoint = "http://127.0.0.1:9991";

        internal const string EndpointTcp = "tcp";
        internal const string EndpointVsock = "vsock";

        /// <summary>
        /// The name of the sign client(i.e. Signer).
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The type of the endpoint. Default is "tcp", and "tcp" and "vsock" are supported now.
        /// If the type is "vsock", the "Endpoint" should be "http://contextId:port" or "https://contextId:port".
        /// </summary>
        public readonly string EndpointType;

        /// <summary>
        /// The host of the sign client(i.e. Signer).
        /// </summary>
        public readonly string Endpoint;

        /// <summary>
        /// Create a new settings instance from the configuration section.
        /// </summary>
        /// <param name="section">The configuration section.</param>
        /// <exception cref="FormatException">If the endpoint type or endpoint is invalid.</exception>
        public Settings(IConfigurationSection section) : base(section)
        {
            Name = section.GetValue("Name", "SignClient");
            EndpointType = section.GetValue("EndpointType", EndpointTcp);
            if (EndpointType != EndpointTcp && EndpointType != EndpointVsock)
                throw new FormatException($"Invalid endpoint type: {EndpointType}");

            Endpoint = section.GetValue("Endpoint", DefaultEndpoint); // Only support local host at present
            _ = GetVsockAddress(); // for check the endpoint is valid
        }

        public static Settings Default
        {
            get
            {
                var section = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [SectionName + ":Name"] = "SignClient",
                        [SectionName + ":Endpoint"] = DefaultEndpoint
                    })
                    .Build()
                    .GetSection(SectionName);
                return new Settings(section);
            }
        }

        /// <summary>
        /// Get the vsock address from the endpoint.
        /// </summary>
        /// <returns>The vsock address. If the endpoint type is not vsock, return null.</returns>
        /// <exception cref="FormatException">If the endpoint is invalid.</exception>
        internal VsockAddress? GetVsockAddress()
        {
            if (EndpointType != EndpointVsock) return null;

            const string httpScheme = "http://";
            const string httpsScheme = "https://";
            var endpoint = Endpoint;
            if (endpoint.StartsWith(httpScheme))
            {
                endpoint = endpoint.Substring(httpScheme.Length);
            }
            else if (endpoint.StartsWith(httpsScheme))
            {
                endpoint = endpoint.Substring(httpsScheme.Length);
            }

            var parts = endpoint.Split(':', 2);
            if (parts.Length != 2) throw new FormatException($"Invalid vsock endpoint: {Endpoint}");
            try
            {
                return new VsockAddress(int.Parse(parts[0]), int.Parse(parts[1]));
            }
            catch
            {
                throw new FormatException($"Invalid vsock endpoint: {Endpoint}");
            }
        }
    }
}
