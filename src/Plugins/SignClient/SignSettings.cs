// Copyright (C) 2015-2025 The Neo Project.
//
// SignSettings.cs file belongs to the neo project and is free
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
    public class SignSettings : IPluginSettings
    {
        public const string SectionName = "PluginConfiguration";
        private const string DefaultEndpoint = "http://127.0.0.1:9991";

        /// <summary>
        /// The name of the sign client(i.e. Signer).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The host of the sign client(i.e. Signer).
        /// The "Endpoint" should be "vsock://contextId:port" if use vsock.
        /// The "Endpoint" should be "http://host:port" or "https://host:port" if use tcp.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Create a new settings instance from the configuration section.
        /// </summary>
        /// <param name="section">The configuration section.</param>
        /// <exception cref="FormatException">If the endpoint type or endpoint is invalid.</exception>
        public SignSettings(IConfigurationSection section)
        {
            Name = section.GetValue("Name", "SignClient");
            Endpoint = section.GetValue("Endpoint", DefaultEndpoint); // Only support local host at present
            ExceptionPolicy = section.GetValue("UnhandledExceptionPolicy", UnhandledExceptionPolicy.Ignore);
            _ = GetVsockAddress(); // for check the endpoint is valid
        }

        public static SignSettings Default
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
                return new SignSettings(section);
            }
        }

        public UnhandledExceptionPolicy ExceptionPolicy { get; }

        /// <summary>
        /// Get the vsock address from the endpoint.
        /// </summary>
        /// <returns>The vsock address. If the endpoint type is not vsock, return null.</returns>
        /// <exception cref="FormatException">If the endpoint is invalid.</exception>
        internal VsockAddress? GetVsockAddress()
        {
            var uri = new Uri(Endpoint); // UriFormatException is a subclass of FormatException
            if (uri.Scheme != "vsock") return null;
            try
            {
                return new VsockAddress(int.Parse(uri.Host), uri.Port);
            }
            catch
            {
                throw new FormatException($"Invalid vsock endpoint: {Endpoint}");
            }
        }
    }
}
