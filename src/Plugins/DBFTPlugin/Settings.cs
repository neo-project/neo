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

namespace Neo.Plugins.DBFTPlugin
{
    public class Settings : PluginSettings
    {
        public string RecoveryLogs { get; }
        public bool IgnoreRecoveryLogs { get; }
        public bool AutoStart { get; }
        public uint Network { get; }
        public uint MaxBlockSize { get; }
        public long MaxBlockSystemFee { get; }

        public Settings(IConfigurationSection section) : base(section)
        {
            RecoveryLogs = section.GetValue("RecoveryLogs", "ConsensusState");
            IgnoreRecoveryLogs = section.GetValue("IgnoreRecoveryLogs", false);
            AutoStart = section.GetValue("AutoStart", false);
            Network = section.GetValue("Network", 5195086u);
            MaxBlockSize = section.GetValue("MaxBlockSize", 262144u);
            MaxBlockSystemFee = section.GetValue("MaxBlockSystemFee", 150000000000L);
        }
    }
}
