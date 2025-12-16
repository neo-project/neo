// Copyright (C) 2015-2025 The Neo Project.
//
// TestUPnPServerConfig.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.UnitTests.Network
{
    internal class TestUPnPServerConfig
    {
        public string ServiceType => "WANIPConnection:1";
        public string Prefix => "http://127.0.0.1:5431/";
        public string ServiceUrl => "http://127.0.0.1:5431/dyndev/uuid:0000e068-20a0-00e0-20a0-48a8000808e0";
        public string ControlUrl => "http://127.0.0.1:5431/uuid:0000e068-20a0-00e0-20a0-48a802086048/" + ServiceType;
    }
}
