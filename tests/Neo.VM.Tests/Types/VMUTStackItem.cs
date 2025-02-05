// Copyright (C) 2015-2025 The Neo Project.
//
// VMUTStackItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neo.Test.Types
{
    public class VMUTStackItem
    {
        [JsonProperty]
        public VMUTStackItemType Type { get; set; }

        [JsonProperty]
        public JToken Value { get; set; }
    }
}
