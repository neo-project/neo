// Copyright (C) 2015-2025 The Neo Project.
//
// Settings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;

namespace Neo.Plugins.StateService
{
    internal class Settings : PluginSettings
    {
        public string Path { get; }
        public bool FullState { get; }
        public uint Network { get; }
        public bool AutoVerify { get; }
        public int MaxFindResultItems { get; }

        public static Settings Default { get; private set; }

        private Settings(IConfigurationSection section) : base(section)
        {
            Path = section.GetValue("Path", "Data_MPT_{0}");
            FullState = section.GetValue("FullState", false);
            Network = section.GetValue("Network", 5195086u);
            AutoVerify = section.GetValue("AutoVerify", false);
            MaxFindResultItems = section.GetValue("MaxFindResultItems", 100);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
