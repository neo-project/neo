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

// Flag to determine if running benchmark or running methods
#define BENCHMARK

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Neo.VM.Benchmark;
using Neo.VM.Benchmark.NativeContract.CryptoLib;
using Neo.VM.Benchmark.OpCode;
using System.Reflection;

// Define the benchmark or execute class
var benchmarkType = typeof(Benchmarks_Convert);

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
#if BENCHMARK
BenchmarkRunner.Run(benchmarkType);
#else
var instance = Activator.CreateInstance(benchmarkType);

var allMethods = benchmarkType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
var setupMethod = allMethods
    .FirstOrDefault(m => m.GetCustomAttribute<GlobalSetupAttribute>() != null);
if (setupMethod != null)
{
    setupMethod.Invoke(instance, null);
}

var iterationSetup = allMethods
    .FirstOrDefault(m => m.GetCustomAttribute<IterationSetupAttribute>() != null);
if (iterationSetup != null)
{
    iterationSetup.Invoke(instance, null);
}

var methods = allMethods.Where(m => m.GetCustomAttribute<BenchmarkAttribute>() != null && !m.GetCustomAttributes<GlobalSetupAttribute>().Any());

foreach (var method in methods.Where(p => p.GetCustomAttribute<GenerateTestsAttribute>() == null))
{

    try
    {
        method.Invoke(instance, null);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
#endif
