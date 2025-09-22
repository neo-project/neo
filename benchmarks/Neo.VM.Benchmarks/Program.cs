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
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Neo.VM.Benchmark;
using Neo.VM.Benchmark.Infrastructure;
using Neo.VM.Benchmark.Native;
using Neo.VM.Benchmark.OpCode;
using Neo.VM.Benchmark.Syscalls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

var runnerArgs = FilterSpecialArguments(args, out var runPocs);
var artifactsRoot = ResolveArtifactsRoot();
EnsureBenchmarkEnvironment();

if (runPocs)
{
    RunProofOfConcepts();
    return;
}

RunBenchmarks(runnerArgs, artifactsRoot);

static string[] FilterSpecialArguments(string[] args, out bool runPocs)
{
    var remaining = new List<string>(args.Length);
    runPocs = false;

    foreach (var arg in args)
    {
        if (string.Equals(arg, "--pocs", StringComparison.OrdinalIgnoreCase))
        {
            runPocs = true;
            continue;
        }

        remaining.Add(arg);
    }

    return remaining.ToArray();
}

static string ResolveArtifactsRoot()
{
    var root = Environment.GetEnvironmentVariable("NEO_BENCHMARK_ARTIFACTS")
               ?? Path.Combine(AppContext.BaseDirectory, "BenchmarkArtifacts");
    Directory.CreateDirectory(root);
    Environment.SetEnvironmentVariable("NEO_BENCHMARK_ARTIFACTS", root);
    return root;
}

static void RunProofOfConcepts()
{
    var benchmarkType = typeof(Benchmarks_PoCs);
    var instance = Activator.CreateInstance(benchmarkType)
                   ?? throw new InvalidOperationException($"Unable to create instance of {benchmarkType.FullName}.");

    benchmarkType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .FirstOrDefault(m => m.GetCustomAttribute<GlobalSetupAttribute>() != null)?
        .Invoke(instance, null);

    var methods = benchmarkType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.DeclaringType == benchmarkType && !m.GetCustomAttributes<GlobalSetupAttribute>().Any());
    foreach (var method in methods)
    {
        method.Invoke(instance, null);
    }
}

static void RunBenchmarks(string[] benchmarkArgs, string artifactsRoot)
{
    BenchmarkArtifactRegistry.Reset();

    var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
    var config = ManualConfig
        .Create(DefaultConfig.Instance)
        .WithArtifactsPath(artifactsRoot)
        .WithOptions(ConfigOptions.DisableOptimizationsValidator | ConfigOptions.KeepBenchmarkFiles);
    if (benchmarkArgs.Length > 0)
    {
        switcher.Run(benchmarkArgs, config);
    }
    else
    {
        switcher.RunAllJoined(config);
    }

    BenchmarkArtifactRegistry.CollectFromDisk(artifactsRoot);

    var missingOpcodes = OpcodeCoverageReport.GetUncoveredOpcodes();
    var missingSyscalls = SyscallCoverageReport.GetMissing();
    var missingNative = NativeCoverageReport.GetMissing();
    if (BenchmarkArtifactRegistry.GetMetricArtifacts().Count == 0)
    {
        Console.WriteLine("[Benchmark] No metrics detected from BenchmarkDotNet run, executing manual pass...");
        ManualSuiteRunner.RunAll();
        BenchmarkArtifactRegistry.CollectFromDisk(artifactsRoot);
    }

    var report = BenchmarkFinalReportWriter.Write(artifactsRoot, missingOpcodes, missingSyscalls, missingNative);
    BenchmarkFinalReportWriter.PrintToConsole(report);

    var hasGaps = missingOpcodes.Count > 0 || missingSyscalls.Count > 0 || missingNative.Count > 0;
    if (hasGaps)
    {
        Console.WriteLine("Benchmark run completed with missing coverage entries.");
        Environment.ExitCode = 1;
    }
    else
    {
        Console.WriteLine("Benchmark run completed with full coverage.");
        Environment.ExitCode = 0;
    }
}

static void EnsureBenchmarkEnvironment()
{
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEO_VM_BENCHMARK")))
        Environment.SetEnvironmentVariable("NEO_VM_BENCHMARK", "1");
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEO_BENCHMARK_COVERAGE")))
        Environment.SetEnvironmentVariable("NEO_BENCHMARK_COVERAGE", "1");
}
