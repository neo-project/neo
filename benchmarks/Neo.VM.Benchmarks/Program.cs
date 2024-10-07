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
using Neo.VM.Benchmark;
using Neo.VM.Benchmark.OpCode;
using System.Reflection;

// Flag to determine if running benchmark or running methods
// If `NEO_VM_BENCHMARK` environment variable is set, run benchmark no matter.
var runBenchmark = true;

// Define the benchmark or execute class
var benchmarkType = typeof(OpCode_ReverseN);


/*
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
 |                                                                           |
 |                    DO NOT MODIFY THE CODE BELOW                           |
 |                                                                           |
 |              All configuration should be done above this line             |
 |                                                                           |
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
*/

if (Environment.GetEnvironmentVariable("NEO_VM_BENCHMARK") != null || runBenchmark)
{
    BenchmarkRunner.Run(benchmarkType);
}
else
{
    var instance = Activator.CreateInstance(benchmarkType);
    var setupMethod = benchmarkType.GetMethod("Setup", BindingFlags.Public | BindingFlags.Instance);
    if (setupMethod != null)
    {
        setupMethod.Invoke(instance, null);
    }

    var methods = benchmarkType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

    foreach (var method in methods)
    {
        if (method.DeclaringType == benchmarkType && method.Name != "Setup")
        {
            method.Invoke(instance, null);
        }
    }
}
