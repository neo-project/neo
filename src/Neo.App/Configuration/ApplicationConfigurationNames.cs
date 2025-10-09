// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationConfigurationNames.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.App.Configuration
{
    internal class ApplicationConfigurationNames
    {
        public static readonly string SectionName = "ApplicationConfiguration";

        // Peer to Peer Configuration
        public static readonly string P2PSectionName = "P2P";
        public static readonly string ListenKey = $"{SectionName}:{P2PSectionName}:Listen";
        public static readonly string PortKey = $"{SectionName}:{P2PSectionName}:Port";
        public static readonly string EnableCompressionKey = $"{SectionName}:{P2PSectionName}:EnableCompression";
        public static readonly string MinDesiredConnectionsKey = $"{SectionName}:{P2PSectionName}:MinDesiredConnections";
        public static readonly string MaxConnectionsKey = $"{SectionName}:{P2PSectionName}:MaxConnections";
        public static readonly string MaxKnownHashesKey = $"{SectionName}:{P2PSectionName}:MaxKnownHashes";
        public static readonly string MaxConnectionsPerAddressKey = $"{SectionName}:{P2PSectionName}:MaxConnectionsPerAddress";

        // Storage Configuration
        public static readonly string StorageSectionName = "Storage";
        public static readonly string StoreEngineKey = $"{SectionName}:{StorageSectionName}:Engine";
        public static readonly string StorePathKey = $"{SectionName}:{StorageSectionName}:Path";
    }
}
