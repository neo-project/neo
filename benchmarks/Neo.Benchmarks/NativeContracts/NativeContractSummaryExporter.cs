// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractSummaryExporter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Emits an aggregated report that highlights native contract performance distribution.
    /// </summary>
    public sealed class NativeContractSummaryExporter : IExporter
    {
        private readonly NativeContractBenchmarkSuite _suite;

        public NativeContractSummaryExporter(NativeContractBenchmarkSuite suite)
        {
            _suite = suite ?? throw new ArgumentNullException(nameof(suite));
        }

        public string Name => "native-contract-summary";

        public string FileExtension => "txt";

        public void ExportToLog(Summary summary, ILogger logger)
        {
            var payload = BuildSummary(summary);
            logger?.WriteLine(payload);
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger logger)
        {
            var payload = BuildSummary(summary);
            Directory.CreateDirectory(summary.ResultsDirectoryPath);
            var path = Path.Combine(summary.ResultsDirectoryPath, "native-contract-summary.txt");
            File.WriteAllText(path, payload);
            logger?.WriteLineInfo($"Native contract summary exported to {path}");
            yield return path;
        }

        private string BuildSummary(Summary summary)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Native Contract Benchmark Summary");
            builder.AppendLine($"Generated at {DateTimeOffset.UtcNow:u}");
            builder.AppendLine();

            if (_suite.Diagnostics.Count > 0)
            {
                builder.AppendLine("Skipped Scenarios:");
                foreach (var diagnostic in _suite.Diagnostics)
                    builder.AppendLine($"  - {diagnostic}");
                builder.AppendLine();
            }

            var grouped = new Dictionary<string, List<(NativeContractBenchmarkCase Case, BenchmarkReport Report)>>(StringComparer.Ordinal);
            var executedCaseIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var report in summary.Reports)
            {
                if (!TryGetCase(report.BenchmarkCase, out var nativeCase))
                    continue;

                executedCaseIds.Add(nativeCase.UniqueId);

                if (!grouped.TryGetValue(nativeCase.ContractName, out var list))
                {
                    list = new List<(NativeContractBenchmarkCase, BenchmarkReport)>();
                    grouped[nativeCase.ContractName] = list;
                }
                list.Add((nativeCase, report));
            }

            var measuredScenarioCount = 0;
            foreach (var contract in grouped)
                measuredScenarioCount += contract.Value.Count;

            builder.AppendLine($"Discovered {_suite.Cases.Count} scenario(s); executed {executedCaseIds.Count} scenario(s) using {DescribeJob(NativeContractBenchmarkOptions.Job)} job profile.");
            builder.AppendLine($"BenchmarkDotNet produced {summary.Reports.Count()} report(s) covering {measuredScenarioCount} measured scenario(s) across {grouped.Count} contract(s).");
            builder.AppendLine();

            foreach (var contractGroup in grouped.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                builder.AppendLine(contractGroup.Key);

                foreach (var entry in contractGroup.Value.OrderBy(p => p.Case.MethodName, StringComparer.Ordinal))
                {
                    var caseInfo = entry.Case;
                    var stats = entry.Report.ResultStatistics;
                    var meanText = stats is null ? "n/a" : FormatStatistic(stats.Mean);
                    var stdDevText = stats is null ? "n/a" : FormatStatistic(stats.StandardDeviation);
                    builder.AppendLine(
                        $"  - {caseInfo.MethodName} [{caseInfo.Profile.Size}] " +
                        $"Mean: {meanText} ns | StdDev: {stdDevText} ns | " +
                        $"CpuFee: {caseInfo.CpuFee} | StorageFee: {caseInfo.StorageFee} | CallFlags: {caseInfo.RequiredCallFlags}");
                }

                var meanValues = contractGroup.Value
                    .Select(p => p.Report.ResultStatistics?.Mean)
                    .Where(v => v.HasValue && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value))
                    .Select(v => v.Value)
                    .ToList();
                var aggregateMeanText = meanValues.Count == 0 ? "n/a" : FormatStatistic(meanValues.Average());
                builder.AppendLine($"    Aggregate mean: {aggregateMeanText} ns");
                builder.AppendLine($"    Scenarios measured: {contractGroup.Value.Count}");
                builder.AppendLine();
            }

            if (grouped.Count == 0)
                builder.AppendLine("No benchmark cases executed. Verify configuration and supported method coverage.");

            return builder.ToString();
        }

        private static string FormatStatistic(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "n/a";
            return value.ToString("N1", CultureInfo.InvariantCulture);
        }

        private static bool TryGetCase(BenchmarkCase benchmarkCase, out NativeContractBenchmarkCase nativeCase)
        {
            if (benchmarkCase.Parameters["Case"] is NativeContractBenchmarkCase c)
            {
                nativeCase = c;
                return true;
            }

            nativeCase = null;
            return false;
        }

        private static string DescribeJob(NativeContractBenchmarkJobMode jobMode) => jobMode switch
        {
            NativeContractBenchmarkJobMode.Quick => "Quick",
            NativeContractBenchmarkJobMode.Short => "Short",
            _ => "Default"
        };
    }
}
