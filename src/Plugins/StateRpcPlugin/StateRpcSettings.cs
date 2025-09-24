// Copyright (C) 2015-2025 The Neo Project.
//
// StateRpcSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;

namespace Neo.Plugins.StateRpcPlugin
{
    internal class StateRpcSettings : IPluginSettings
    {
        public bool FullState { get; }
        public uint Network { get; }
        public int MaxFindResultItems { get; }

        public static StateRpcSettings Default { get; private set; }

        public UnhandledExceptionPolicy ExceptionPolicy { get; }

        private StateRpcSettings(IConfigurationSection section)
        {
            FullState = section.GetValue("FullState", false);
            Network = section.GetValue("Network", 5195086u);
            MaxFindResultItems = section.GetValue("MaxFindResultItems", 100);
            ExceptionPolicy = section.GetValue("UnhandledExceptionPolicy", UnhandledExceptionPolicy.StopPlugin);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new StateRpcSettings(section);
        }
    }
}
