// Copyright (C) 2015-2024 The Neo Project.
//
// VMUT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Newtonsoft.Json;

namespace Neo.Test.Types
{
    public class VMUT
    {
        [JsonProperty]
        public string Category { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public VMUTEntry[] Tests { get; set; }
    }
}
