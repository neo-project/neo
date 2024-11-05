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

namespace Neo.Plugins.DBFTPlugin
{
    public class Settings : PluginSettings
    {
        public string RecoveryLogs { get; private set; }
        public bool IgnoreRecoveryLogs { get; private set; }
        public bool AutoStart { get; private set; }
        public uint Network { get; private set; }
        public uint MaxBlockSize { get; private set; }
        public long MaxBlockSystemFee { get; private set; }

        // Modified constructor with default values
        public Settings(IConfigurationSection section = null)
            : base(section)
        {
            // Set default values
            RecoveryLogs = "ConsensusState";
            IgnoreRecoveryLogs = false;
            AutoStart = false;
            Network = 5195086u;
            MaxBlockSize = 262144u;
            MaxBlockSystemFee = 150000000000L;

            // Override defaults if section is provided
            if (section != null)
            {
                RecoveryLogs = section.GetValue("RecoveryLogs", RecoveryLogs);
                IgnoreRecoveryLogs = section.GetValue("IgnoreRecoveryLogs", IgnoreRecoveryLogs);
                AutoStart = section.GetValue("AutoStart", AutoStart);
                Network = section.GetValue("Network", Network);
                MaxBlockSize = section.GetValue("MaxBlockSize", MaxBlockSize);
                MaxBlockSystemFee = section.GetValue("MaxBlockSystemFee", MaxBlockSystemFee);
            }
        }
    }
}
