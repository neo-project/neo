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
        public const int DefaultPort = 9991;
        public const string SectionName = "PluginConfiguration";

        /// <summary>
        /// The name of the sign client(i.e. Signer).
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The host of the sign client(i.e. Signer).
        /// Only support local host at present, so host always is "127.0.0.1" or "::1" now.
        /// </summary>
        public readonly string Host = "127.0.0.1";

        /// <summary>
        /// The port of the sign client(i.e. Signer).
        /// </summary>
        public readonly int Port;

        public Settings(IConfigurationSection section) : base(section)
        {
            Name = section.GetValue("Name", "SignClient");
            Port = section.GetValue("Port", DefaultPort);
        }

        public static Settings Default
        {
            get
            {
                var section = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [SectionName + ":Name"] = "SignClient",
                        [SectionName + ":Port"] = DefaultPort.ToString()
                    })
                    .Build()
                    .GetSection(SectionName);
                return new Settings(section);
            }
        }
    }
}
