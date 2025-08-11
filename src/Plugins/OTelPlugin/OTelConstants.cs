// Copyright (C) 2015-2025 The Neo Project.
//
// OTelConstants.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.OpenTelemetry
{
    public static class OTelConstants
    {
        public const string DefaultServiceName = "neo-node";
        public const string DefaultEndpoint = "http://localhost:4317";
        public const string DefaultPath = "/metrics";
        public const string ProtocolGrpc = "grpc";
        public const string ProtocolHttpProtobuf = "http/protobuf";
        public const int DefaultPrometheusPort = 9090;
        public const int DefaultTimeout = 10000;
        public const int DefaultMetricsInterval = 10000;
    }
}
