// Copyright (C) 2015-2025 The Neo Project.
//
// RpcTestCase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Json.Benchmarks
{
    public class RpcTestCase
    {
        public string? Name { get; set; }

        public RpcRequest? Request { get; set; }

        public RpcResponse? Response { get; set; }
    }

    public class RpcRequest
    {
        public JToken? Id { get; set; }

        public string? JsonRpc { get; set; }

        public string? Method { get; set; }

        public JToken[]? Params { get; set; }
    }

    public class RpcResponse
    {
        public JToken? Id { get; set; }

        public string? JsonRpc { get; set; }

        public RpcResponseError? Error { get; set; }

        public JToken? Result { get; set; }

        public string? RawResponse { get; set; }

    }

    public class RpcResponseError
    {
        public int Code { get; set; }

        public string? Message { get; set; }

        public JToken? Data { get; set; }
    }
}
