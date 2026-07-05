// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractBenchmarkConfig.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// BenchmarkDotNet configuration tailored for native contract benchmarks.
    /// </summary>
    public sealed class NativeContractBenchmarkConfig : ManualConfig
    {
        public NativeContractBenchmarkConfig(NativeContractBenchmarkSuite suite)
        {
            AddColumn(new NativeContractInfoColumn("contract", "Contract", "Native contract name", c => c.ContractName));
            AddColumn(new NativeContractInfoColumn("method", "Method", "Native method name", c => c.MethodName));
            AddColumn(new NativeContractInfoColumn("profile", "Profile", "Input size profile", c => $"{c.Profile.Size}"));
            AddColumn(new NativeContractInfoColumn("scenario", "Scenario", "Scenario label", c => c.ScenarioName));
            AddColumn(new NativeContractInfoColumn("parameters", "Inputs", "Generated parameter summary", c => c.ParameterSummary));
            AddColumn(new NativeContractInfoColumn("cpufee", "CpuFee", "Declared CPU fee", c => c.CpuFee.ToString()));
            AddColumn(new NativeContractInfoColumn("storagefee", "StorageFee", "Declared storage fee", c => c.StorageFee.ToString()));

            AddLogger(ConsoleLogger.Unicode);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddExporter(new NativeContractSummaryExporter(suite));
            AddExporter(MarkdownExporter.Default);
            AddValidator(JitOptimizationsValidator.DontFailOnError);
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddJob(CreateJob());
            SummaryStyle = SummaryStyle.Default.WithMaxParameterColumnWidth(60);
            Options |= ConfigOptions.DisableOptimizationsValidator | ConfigOptions.KeepBenchmarkFiles;
        }

        private static Job CreateJob()
        {
            return NativeContractBenchmarkOptions.Job switch
            {
                NativeContractBenchmarkJobMode.Quick => CreateQuickJob(),
                NativeContractBenchmarkJobMode.Short => CreateShortJob(),
                _ => CreateDefaultJob()
            };
        }

        internal static Job CreateQuickJob()
        {
            return Job.Dry
                .WithLaunchCount(1)
                .WithWarmupCount(1)
                .WithIterationCount(1)
                .WithInvocationCount(1)
                .WithUnrollFactor(1)
                .WithId("QuickNative");
        }

        internal static Job CreateShortJob()
        {
            return Job.ShortRun
                .WithId("ShortNative");
        }

        internal static Job CreateDefaultJob()
        {
            return Job.Default
                .WithId("DefaultNative");
        }
    }
}
