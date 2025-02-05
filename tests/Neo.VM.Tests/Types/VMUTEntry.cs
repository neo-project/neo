// Copyright (C) 2015-2025 The Neo Project.
//
// VMUTEntry.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Test.Converters;
using Newtonsoft.Json;

namespace Neo.Test.Types
{
    public class VMUTEntry
    {
        [JsonProperty(Order = 1)]
        public string Name { get; set; }

        [JsonProperty(Order = 2), JsonConverter(typeof(ScriptConverter))]
        public byte[] Script { get; set; }

        [JsonProperty(Order = 3)]
        public VMUTStep[] Steps { get; set; }
    }
}
