// Copyright (C) 2015-2025 The Neo Project.
//
// OracleSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Util.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace Neo.Plugins.OracleService
{
    class HttpsSettings
    {
        public TimeSpan Timeout { get; }

        public HttpsSettings(IConfigurationSection section)
        {
            Timeout = TimeSpan.FromMilliseconds(section.GetValue("Timeout", 5000));
        }
    }

    class NeoFSSettings
    {
        public string EndPoint { get; }
        public TimeSpan Timeout { get; }

        public NeoFSSettings(IConfigurationSection section)
        {
            EndPoint = section.GetValue("EndPoint", "127.0.0.1:8080");
            Timeout = TimeSpan.FromMilliseconds(section.GetValue("Timeout", 15000));
        }
    }

    class OracleSettings : IPluginSettings
    {
        public uint Network { get; }
        public Uri[] Nodes { get; }
        public TimeSpan MaxTaskTimeout { get; }
        public TimeSpan MaxOracleTimeout { get; }
        public bool AllowPrivateHost { get; }
        public string[] AllowedContentTypes { get; }
        public HttpsSettings Https { get; }
        public NeoFSSettings NeoFS { get; }
        public bool AutoStart { get; }

        public static OracleSettings Default { get; private set; }

        public UnhandledExceptionPolicy ExceptionPolicy { get; }

        private OracleSettings(IConfigurationSection section)
        {
            Network = section.GetValue("Network", 5195086u);
            Nodes = section.GetSection("Nodes").GetChildren().Select(p => new Uri(p.Get<string>(), UriKind.Absolute)).ToArray();
            MaxTaskTimeout = TimeSpan.FromMilliseconds(section.GetValue("MaxTaskTimeout", 432000000));
            MaxOracleTimeout = TimeSpan.FromMilliseconds(section.GetValue("MaxOracleTimeout", 15000));
            AllowPrivateHost = section.GetValue("AllowPrivateHost", false);
            AllowedContentTypes = section.GetSection("AllowedContentTypes").GetChildren().Select(p => p.Get<string>()).ToArray();
            ExceptionPolicy = section.GetValue("UnhandledExceptionPolicy", UnhandledExceptionPolicy.Ignore);
            if (AllowedContentTypes.Length == 0)
                AllowedContentTypes = AllowedContentTypes.Concat("application/json").ToArray();
            Https = new HttpsSettings(section.GetSection("Https"));
            NeoFS = new NeoFSSettings(section.GetSection("NeoFS"));
            AutoStart = section.GetValue("AutoStart", false);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new OracleSettings(section);
        }
    }
}
