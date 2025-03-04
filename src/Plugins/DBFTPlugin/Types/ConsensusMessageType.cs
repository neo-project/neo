// Copyright (C) 2015-2025 The Neo Project.
//
// ConsensusMessageType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.DBFTPlugin.Types
{
    public enum ConsensusMessageType : byte
    {
        ChangeView = 0x00,

        PrepareRequest = 0x20,
        PrepareResponse = 0x21,
        Commit = 0x30,

        RecoveryRequest = 0x40,
        RecoveryMessage = 0x41,
    }
}
