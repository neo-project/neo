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

namespace Neo.Plugins.ApplicationLogs
{
    internal class ApplicationLogsSettings : IPluginSettings
    {
        public string Path { get; }
        public uint Network { get; }
        public int MaxStackSize { get; }

        public bool Debug { get; }

        public static ApplicationLogsSettings Default { get; private set; } = default!;

        public UnhandledExceptionPolicy ExceptionPolicy { get; }

        private ApplicationLogsSettings(IConfigurationSection section)
        {
            Path = section.GetValue("Path", "ApplicationLogs_{0}");
            Network = section.GetValue("Network", 5195086u);
            MaxStackSize = section.GetValue("MaxStackSize", (int)ushort.MaxValue);
            Debug = section.GetValue("Debug", false);
            ExceptionPolicy = section.GetValue("UnhandledExceptionPolicy", UnhandledExceptionPolicy.Ignore);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new ApplicationLogsSettings(section);
        }
    }
}
