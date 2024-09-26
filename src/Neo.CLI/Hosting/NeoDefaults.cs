// Copyright (C) 2015-2024 The Neo Project.
//
// NeoDefaults.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.CLI.Hosting
{
    internal static class NeoDefaults
    {
        public static readonly string StoreProviderName = "LevelDBStore";
        public static readonly string ConfigurationFileName = "config.json";

        public static readonly string ConsolePromptName = "neo>";

        public static readonly string NeoNameService = "0x50ac1c37690cc2cfc594472833cf57505d5f46de";
        public static readonly string GitHubReleasesAPI = "https://api.github.com/repos/neo-project/neo/releases";
        public static readonly string StorePathFormat = "Data_LevelDB_{0:X2}";
        public static readonly string ArchiveFileName = "chain.0.acc";
    }
}
