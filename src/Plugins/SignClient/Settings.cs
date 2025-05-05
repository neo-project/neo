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
using System.Net;

namespace Neo.Plugins.SignClient
{
    public class Settings : PluginSettings
    {
        /// <summary>
        /// Only support local host at present, so host always is "127.0.0.1" or "::1" now.
        /// </summary>
        public static readonly string DefaultEndpoint = $"http://{IPAddress.Loopback}:9991";
        public const string SectionName = "PluginConfiguration";

        /// <summary>
        /// The name of the sign client(i.e. Signer).
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The host of the sign client(i.e. Signer).
        /// </summary>
        public readonly string Endpoint;

        public Settings(IConfigurationSection section) : base(section)
        {
            Name = section.GetValue("Name", "SignClient");
            Endpoint = section.GetValue("Endpoint", DefaultEndpoint);
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
    }
}
