// Copyright (C) 2015-2024 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Running;

namespace Neo.Json.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<JsonBenchmark>();
        }
    }
}

/// | Method                 | Mean     | Error    | StdDev    | Median   | Gen0    | Gen1    | Gen2    | Allocated |
/// |----------------------- |---------:|---------:|----------:|---------:|--------:|--------:|--------:|----------:|
/// | Newtonsoft_Deserialize | 627.4 us |  9.10 us |   8.07 us | 627.6 us | 79.1016 | 53.7109 |       - | 978.52 KB |
/// | NeoJson_Deserialize    | 635.8 us | 41.54 us | 122.49 us | 720.1 us | 73.2422 | 36.1328 | 36.1328 | 919.45 KB |
