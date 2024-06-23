// Copyright (C) 2015-2024 The Neo Project.
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
    internal class Settings : PluginSettings
    {
        public string Path { get; }
        public uint Network { get; }
        public int MaxStackSize { get; }

        public bool Debug { get; }

        public static Settings Default { get; private set; }

        private Settings(IConfigurationSection section) : base(section)
        {
            Path = section.GetValue("Path", "ApplicationLogs_{0}");
            Network = section.GetValue("Network", 5195086u);
            MaxStackSize = section.GetValue("MaxStackSize", (int)ushort.MaxValue);
            Debug = section.GetValue("Debug", false);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
