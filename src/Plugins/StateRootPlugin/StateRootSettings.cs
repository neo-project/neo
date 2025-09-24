// Copyright (C) 2015-2025 The Neo Project.
//
// StateRootSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;

namespace Neo.Plugins.StateRootPlugin
{
    internal class StateRootSettings : IPluginSettings
    {
        public string Path { get; }
        public bool FullState { get; }
        public uint Network { get; }
        public bool AutoVerify { get; }

        public static StateRootSettings Default { get; private set; }

        public UnhandledExceptionPolicy ExceptionPolicy { get; }

        private StateRootSettings(IConfigurationSection section)
        {
            Path = section.GetValue("Path", "Data_MPT_{0}");
            FullState = section.GetValue("FullState", false);
            Network = section.GetValue("Network", 5195086u);
            AutoVerify = section.GetValue("AutoVerify", false);
            ExceptionPolicy = section.GetValue("UnhandledExceptionPolicy", UnhandledExceptionPolicy.StopPlugin);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new StateRootSettings(section);
        }
    }
}
