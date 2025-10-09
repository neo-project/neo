// Copyright (C) 2015-2025 The Neo Project.
//
// HostConfigurationNames.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.App.Configuration
{
    internal class HostConfigurationNames
    {
        public static readonly string SectionName = "Host";

        public static readonly string WindowsServiceKey = $"{SectionName}:WindowsService";
        public static readonly string SystemdServiceKey = $"{SectionName}:SystemdService";
        public static readonly string UserInteractiveKey = $"{SectionName}:UserInteractive";
    }
}
