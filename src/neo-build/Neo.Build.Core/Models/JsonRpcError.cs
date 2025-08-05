// Copyright (C) 2015-2025 The Neo Project.
//
// JsonRpcError.cs file belongs to the neo project and is free
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
    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        private JsonRpcError() { }

        public static JsonRpcErrorResponse CreateResponse(int code, string message) =>
            new()
            {
                Id = null,
                Error = new()
                {
                    Code = code,
                    Message = message,
                },
            };
    }
}
