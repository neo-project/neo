// Copyright (C) 2015-2025 The Neo Project.
//
// JsonRpcResponse.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text.Json.Serialization;

namespace Neo.Build.Core.Models
{
    public class JsonRpcResponse<T>
        where T : class?
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc => "2.0";

        [JsonPropertyName("result")]
        public T? Result { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
}
