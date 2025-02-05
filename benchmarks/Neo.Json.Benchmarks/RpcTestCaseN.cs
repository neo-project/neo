// Copyright (C) 2015-2025 The Neo Project.
//
// RpcTestCaseN.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Json.Benchmarks
{
    public class RpcTestCaseN
    {
        public string? Name { get; set; }

        public RpcRequestN? Request { get; set; }

        public RpcResponseN? Response { get; set; }
    }

    public class RpcRequestN
    {
        public Newtonsoft.Json.Linq.JToken? Id { get; set; }

        public string? JsonRpc { get; set; }

        public string? Method { get; set; }

        public Newtonsoft.Json.Linq.JToken[]? Params { get; set; }
    }

    public class RpcResponseN
    {
        public Newtonsoft.Json.Linq.JToken? Id { get; set; }

        public string? JsonRpc { get; set; }

        public RpcResponseErrorN? Error { get; set; }

        public Newtonsoft.Json.Linq.JToken? Result { get; set; }

        public string? RawResponse { get; set; }

    }

    public class RpcResponseErrorN
    {
        public int Code { get; set; }

        public string? Message { get; set; }

        public Newtonsoft.Json.Linq.JToken? Data { get; set; }
    }
}
