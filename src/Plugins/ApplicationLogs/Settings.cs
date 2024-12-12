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
        public bool AutoStart { get; }
        public string Path { get; }
        public int MaxStackSize { get; }

        public bool Debug { get; }

        public static Settings Current { get; private set; }

        private Settings(IConfigurationSection section) : base(section)
        {
            AutoStart = section.GetValue("AutoStart", false);
            Path = section.GetValue("Path", "ApplicationLogs_{0}");
            MaxStackSize = section.GetValue("MaxStackSize", (int)ushort.MaxValue);
            Debug = section.GetValue("Debug", false);
        }

        public static void Load(IConfigurationSection section)
        {
            Current = new Settings(section);
        }
    }
}
