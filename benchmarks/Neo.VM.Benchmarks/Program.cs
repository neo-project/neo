// Copyright (C) 2015-2025 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Neo.VM.Benchmark;
using System.Reflection;

// Flag to determine if running benchmark or running methods
// If `NEO_VM_BENCHMARK` environment variable is set, run benchmark no matter.
var runBenchmark = true;

// Define the benchmark or execute class
var benchmarkType = typeof(Benchmarks_PoCs);

/*
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
 |                                                                           |
 |                    DO NOT MODIFY THE CODE BELOW                           |
 |                                                                           |
 |              All configuration should be done above this line             |
 |                                                                           |
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
*/

// Explanation:
// Benchmark methods must contain no parameters to be valid.
// This is because we need to be able to invoke these methods repeatedly
// without any external input. All necessary data should be set up in the Setup method
// or as properties of the benchmark class.

// Example:

// [Benchmark]
// public void BenchmarkMethod()
// {
//     // Benchmark code here
// }
if (Environment.GetEnvironmentVariable("NEO_VM_BENCHMARK") != null || runBenchmark)
{
    BenchmarkRunner.Run(benchmarkType);
}
else
{
    var instance = Activator.CreateInstance(benchmarkType);
    var setupMethod = benchmarkType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .FirstOrDefault(m => m.GetCustomAttribute<GlobalSetupAttribute>() != null);
    if (setupMethod != null)
    {
        setupMethod.Invoke(instance, null);
    }

    var methods = benchmarkType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
    foreach (var method in methods)
    {
        if (method.DeclaringType == benchmarkType && !method.GetCustomAttributes<GlobalSetupAttribute>().Any())
        {
            method.Invoke(instance, null);
        }
    }
}
