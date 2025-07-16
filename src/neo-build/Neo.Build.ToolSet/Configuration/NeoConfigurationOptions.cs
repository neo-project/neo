// Copyright (C) 2015-2025 The Neo Project.
//
// NeoConfigurationOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.ToolSet.Options;

namespace Neo.Build.ToolSet.Configuration
{
    internal class NeoConfigurationOptions : INeoConfigurationOptions
    {
        public NeoSystemNetworkOptions NetworkOptions { get; set; } = new();
        public NeoSystemStorageOptions StorageOptions { get; set; } = new();
        public ProtocolOptions ProtocolOptions { get; set; } = new();
        public ApplicationEngineOptions ApplicationEngineOptions { get; set; } = new();
        public DBFTOptions DBFTOptions { get; set; } = new();
    }
}
