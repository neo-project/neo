// Copyright (C) 2015-2025 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Neo.VM.Benchmark;
using System.Reflection;

// Define the benchmark or execute class
if (Environment.GetEnvironmentVariable("NEO_VM_BENCHMARK") != null)
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
else
{
    var benchmarkType = typeof(Benchmarks_PoCs);
    var instance = Activator.CreateInstance(benchmarkType);
    benchmarkType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .FirstOrDefault(m => m.GetCustomAttribute<GlobalSetupAttribute>() != null)?
        .Invoke(instance, null); // setup

    var methods = benchmarkType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.DeclaringType == benchmarkType && !m.GetCustomAttributes<GlobalSetupAttribute>().Any());
    foreach (var method in methods)
    {
        method.Invoke(instance, null);
    }
}
