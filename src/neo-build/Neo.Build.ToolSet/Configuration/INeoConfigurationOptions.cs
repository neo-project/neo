// Copyright (C) 2015-2025 The Neo Project.
//
// INeoEnvironment.cs file belongs to the neo project and is free
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
    internal interface INeoConfigurationOptions
    {
        public NeoSystemNetworkOptions NetworkOptions { get; set; }
        public NeoSystemStorageOptions StorageOptions { get; set; }
        public ProtocolOptions ProtocolOptions { get; set; }
        public ApplicationEngineOptions ApplicationEngineOptions { get; set; }
        public DBFTOptions DBFTOptions { get; set; }
    }
}
