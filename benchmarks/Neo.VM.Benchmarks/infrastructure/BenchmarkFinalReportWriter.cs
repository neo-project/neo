// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkFinalReportWriter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Benchmark.Native;
using Neo.VM.Benchmark.OpCode;
using Neo.VM.Benchmark.Syscalls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using VmOpCode = Neo.VM.OpCode;

namespace Neo.VM.Benchmark.Infrastructure
{
    internal static class BenchmarkFinalReportWriter
    {
        internal sealed record ReportSummary(
            string SummaryPath,
            string? CombinedMetricsPath,
            IReadOnlyCollection<(BenchmarkComponent Component, string Path)> MetricSources,
            IReadOnlyCollection<(string Category, string Path)> CoverageSources,
            IReadOnlyCollection<VmOpCode> MissingOpcodes,
            IReadOnlyCollection<string> MissingSyscalls,
            IReadOnlyCollection<string> MissingNative,
            string OpcodeMissingPath,
            string InteropMissingPath,
            string? ScalingPath);

        public static ReportSummary Write(string artifactsRoot,
            IReadOnlyCollection<VmOpCode> missingOpcodes,
            IReadOnlyCollection<string> missingSyscalls,
            IReadOnlyCollection<string> missingNative)
        {
            Directory.CreateDirectory(artifactsRoot);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            var metricSources = BenchmarkArtifactRegistry.GetMetricArtifacts();
            var coverageSources = BenchmarkArtifactRegistry.GetCoverageArtifacts();
            var combinedMetricsPath = MergeMetrics(artifactsRoot, timestamp, metricSources);

            var opcodeCoveragePath = Path.Combine(artifactsRoot, "opcode-coverage.csv");
            OpcodeCoverageReport.WriteCoverageTable(opcodeCoveragePath);
            BenchmarkArtifactRegistry.RegisterCoverage("opcode-coverage", opcodeCoveragePath);
            coverageSources = BenchmarkArtifactRegistry.GetCoverageArtifacts();

            var opcodeMissingPath = Path.Combine(artifactsRoot, "opcode-missing.csv");
            OpcodeCoverageReport.WriteMissingList(opcodeMissingPath, missingOpcodes);
            BenchmarkArtifactRegistry.RegisterCoverage("opcode-missing", opcodeMissingPath);
            coverageSources = BenchmarkArtifactRegistry.GetCoverageArtifacts();

            var interopMissingPath = Path.Combine(artifactsRoot, "interop-missing.csv");
            InteropCoverageReport.WriteReport(interopMissingPath, missingSyscalls, missingNative);
            BenchmarkArtifactRegistry.RegisterCoverage("interop-missing", interopMissingPath);
            coverageSources = BenchmarkArtifactRegistry.GetCoverageArtifacts();

            var scalingPath = WriteScalingReport(artifactsRoot, combinedMetricsPath);

            var summaryPath = Path.Combine(artifactsRoot, $"benchmark-summary-{timestamp}.txt");
            WriteSummary(summaryPath, combinedMetricsPath, scalingPath, metricSources, coverageSources, missingOpcodes, missingSyscalls, missingNative, opcodeMissingPath, interopMissingPath);

            return new ReportSummary(summaryPath, combinedMetricsPath, metricSources, coverageSources,
                missingOpcodes, missingSyscalls, missingNative, opcodeMissingPath, interopMissingPath, scalingPath);
        }

        public static void PrintToConsole(ReportSummary summary)
        {
            Console.WriteLine();
            Console.WriteLine("================ Benchmark Summary ================");
            Console.WriteLine(summary.SummaryPath);
            Console.WriteLine();

            if (summary.MetricSources.Count > 0)
            {
                Console.WriteLine("Metrics");
                foreach (var (component, path) in summary.MetricSources)
                    Console.WriteLine($"- {component,-14} {path}");
                if (!string.IsNullOrEmpty(summary.CombinedMetricsPath))
                    Console.WriteLine($"- Combined        {summary.CombinedMetricsPath}");
                if (!string.IsNullOrEmpty(summary.ScalingPath))
                    Console.WriteLine($"- Scaling         {summary.ScalingPath}");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Metrics");
                Console.WriteLine("- No metric artifacts were produced.");
                Console.WriteLine();
            }

            Console.WriteLine("Coverage");
            Console.WriteLine($"- Missing opcodes : {summary.MissingOpcodes.Count}");
            Console.WriteLine($"  Details         : {summary.OpcodeMissingPath}");
            Console.WriteLine($"- Missing syscalls: {summary.MissingSyscalls.Count}");
            Console.WriteLine($"- Missing natives : {summary.MissingNative.Count}");
            Console.WriteLine($"  Interop summary : {summary.InteropMissingPath}");
            foreach (var (category, path) in summary.CoverageSources)
            {
                Console.WriteLine($"  {category,-17} {path}");
            }
            Console.WriteLine("===================================================");
            Console.WriteLine();
        }

        private static string? MergeMetrics(string root, string timestamp, IReadOnlyCollection<(BenchmarkComponent Component, string Path)> metricSources)
        {
            if (metricSources.Count == 0)
                return null;

            var combinedPath = Path.Combine(root, $"benchmark-metrics-{timestamp}.csv");
            bool headerWritten = false;

            using var writer = new StreamWriter(combinedPath, append: false);
            foreach (var (_, path) in metricSources)
            {
                if (!File.Exists(path))
                    continue;

                using var reader = new StreamReader(path);
                string? header = reader.ReadLine();
                if (header is null)
                    continue;

                if (!headerWritten)
                {
                    writer.WriteLine(header);
                    headerWritten = true;
                }

                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    writer.WriteLine(line);
                }
            }

            if (!headerWritten)
            {
                writer.Close();
                File.Delete(combinedPath);
                return null;
            }

            return combinedPath;
        }

        private static void WriteSummary(
            string summaryPath,
            string? combinedMetricsPath,
            string? scalingPath,
            IReadOnlyCollection<(BenchmarkComponent Component, string Path)> metricSources,
            IReadOnlyCollection<(string Category, string Path)> coverageSources,
            IReadOnlyCollection<VmOpCode> missingOpcodes,
            IReadOnlyCollection<string> missingSyscalls,
            IReadOnlyCollection<string> missingNative,
            string opcodeMissingPath,
            string interopMissingPath)
        {
            using var writer = new StreamWriter(summaryPath, append: false);
            writer.WriteLine($"NEO Benchmark Summary ({DateTime.UtcNow:O})");
            writer.WriteLine();

            writer.WriteLine("Metrics");
            if (metricSources.Count == 0)
            {
                writer.WriteLine("- No metric artifacts were produced.");
            }
            else
            {
                foreach (var (component, path) in metricSources)
                    writer.WriteLine($"- {component,-14} {path}");
                if (!string.IsNullOrEmpty(combinedMetricsPath))
                    writer.WriteLine($"- Combined        {combinedMetricsPath}");
                if (!string.IsNullOrEmpty(scalingPath))
                    writer.WriteLine($"- Scaling         {scalingPath}");
            }

            writer.WriteLine();
            writer.WriteLine("Coverage");
            writer.WriteLine($"- Missing opcodes : {missingOpcodes.Count}");
            writer.WriteLine($"  Details         : {opcodeMissingPath}");
            writer.WriteLine($"- Missing syscalls: {missingSyscalls.Count}");
            writer.WriteLine($"- Missing natives : {missingNative.Count}");
            writer.WriteLine($"  Interop summary : {interopMissingPath}");
            foreach (var (category, path) in coverageSources)
                writer.WriteLine($"- {category,-17} {path}");
        }

        private static string? WriteScalingReport(string artifactsRoot, string? combinedMetricsPath)
        {
            if (string.IsNullOrEmpty(combinedMetricsPath) || !File.Exists(combinedMetricsPath))
                return null;

            var measurements = new List<ScalingMeasurement>();
            using (var reader = new StreamReader(combinedMetricsPath))
            {
                var header = reader.ReadLine();
                if (header is null)
                    return null;

                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (ScalingMeasurement.TryParse(line, out var measurement))
                        measurements.Add(measurement);
                }
            }

            if (measurements.Count == 0)
                return null;

            var scalingPath = Path.Combine(artifactsRoot, "benchmark-scaling.csv");
            using var writer = new StreamWriter(scalingPath, append: false);
            writer.WriteLine("Component,ScenarioId,OperationKind,OperationId,Complexity,BaselineNsPerIteration,SingleNsPerIteration,SaturatedNsPerIteration,IterationScale,BaselineNsPerByte,SingleNsPerByte,SaturatedNsPerByte,ByteScale,BaselineAllocatedPerIteration,SingleAllocatedPerIteration,SaturatedAllocatedPerIteration,AllocatedIterationScale,BaselineAllocatedPerByte,SingleAllocatedPerByte,SaturatedAllocatedPerByte,AllocatedByteScale,BaselineAvgStackDepth,SingleAvgStackDepth,SaturatedAvgStackDepth,StackDepthScale,BaselineAvgAltStackDepth,SingleAvgAltStackDepth,SaturatedAvgAltStackDepth,AltStackDepthScale,BaselineGasPerIteration,SingleGasPerIteration,SaturatedGasPerIteration,GasIterationScale,BaselineTotalDataBytes,SingleTotalDataBytes,SaturatedTotalDataBytes,BaselineTotalAllocatedBytes,SingleTotalAllocatedBytes,SaturatedTotalAllocatedBytes,BaselineTotalGas,SingleTotalGas,SaturatedTotalGas");

            foreach (var group in measurements.GroupBy(m => (m.Component, m.ScenarioId, m.OperationKind, m.OperationId, m.Complexity)))
            {
                if (!group.Any())
                    continue;

                var baseline = group.FirstOrDefault(m => m.Variant == BenchmarkVariant.Baseline) ?? group.First();
                var single = group.FirstOrDefault(m => m.Variant == BenchmarkVariant.Single) ?? baseline;
                var saturated = group.FirstOrDefault(m => m.Variant == BenchmarkVariant.Saturated) ?? single;

                var iterationScale = single.NanosecondsPerIteration > 0 && saturated.NanosecondsPerIteration > 0
                    ? saturated.NanosecondsPerIteration / single.NanosecondsPerIteration
                    : 0;
                var byteScale = single.NanosecondsPerByte > 0 && saturated.NanosecondsPerByte > 0
                    ? saturated.NanosecondsPerByte / single.NanosecondsPerByte
                    : 0;
                var allocatedIterationScale = single.AllocatedBytesPerIteration > 0 && saturated.AllocatedBytesPerIteration > 0
                    ? saturated.AllocatedBytesPerIteration / single.AllocatedBytesPerIteration
                    : 0;
                var allocatedByteScale = single.AllocatedBytesPerByte > 0 && saturated.AllocatedBytesPerByte > 0
                    ? saturated.AllocatedBytesPerByte / single.AllocatedBytesPerByte
                    : 0;
                var stackDepthScale = single.AverageStackDepth > 0 && saturated.AverageStackDepth > 0
                    ? saturated.AverageStackDepth / single.AverageStackDepth
                    : 0;
                var altStackDepthScale = single.AverageAltStackDepth > 0 && saturated.AverageAltStackDepth > 0
                    ? saturated.AverageAltStackDepth / single.AverageAltStackDepth
                    : 0;
                var gasIterationScale = single.GasPerIteration > 0 && saturated.GasPerIteration > 0
                    ? saturated.GasPerIteration / single.GasPerIteration
                    : 0;

                writer.WriteLine(string.Join(',',
                    group.Key.Component,
                    group.Key.ScenarioId,
                    group.Key.OperationKind,
                    group.Key.OperationId,
                    group.Key.Complexity,
                    baseline.NanosecondsPerIteration.ToString("F2", CultureInfo.InvariantCulture),
                    single.NanosecondsPerIteration.ToString("F2", CultureInfo.InvariantCulture),
                    saturated.NanosecondsPerIteration.ToString("F2", CultureInfo.InvariantCulture),
                    iterationScale.ToString("F4", CultureInfo.InvariantCulture),
                    baseline.NanosecondsPerByte.ToString("F4", CultureInfo.InvariantCulture),
                    single.NanosecondsPerByte.ToString("F4", CultureInfo.InvariantCulture),
                    saturated.NanosecondsPerByte.ToString("F4", CultureInfo.InvariantCulture),
                    byteScale.ToString("F4", CultureInfo.InvariantCulture),
                    baseline.AllocatedBytesPerIteration.ToString("F2", CultureInfo.InvariantCulture),
                    single.AllocatedBytesPerIteration.ToString("F2", CultureInfo.InvariantCulture),
                    saturated.AllocatedBytesPerIteration.ToString("F2", CultureInfo.InvariantCulture),
                    allocatedIterationScale.ToString("F4", CultureInfo.InvariantCulture),
                    baseline.AllocatedBytesPerByte.ToString("F6", CultureInfo.InvariantCulture),
                    single.AllocatedBytesPerByte.ToString("F6", CultureInfo.InvariantCulture),
                    saturated.AllocatedBytesPerByte.ToString("F6", CultureInfo.InvariantCulture),
                    allocatedByteScale.ToString("F4", CultureInfo.InvariantCulture),
                    baseline.AverageStackDepth.ToString("F2", CultureInfo.InvariantCulture),
                    single.AverageStackDepth.ToString("F2", CultureInfo.InvariantCulture),
                    saturated.AverageStackDepth.ToString("F2", CultureInfo.InvariantCulture),
                    stackDepthScale.ToString("F4", CultureInfo.InvariantCulture),
                    baseline.AverageAltStackDepth.ToString("F2", CultureInfo.InvariantCulture),
                    single.AverageAltStackDepth.ToString("F2", CultureInfo.InvariantCulture),
                    saturated.AverageAltStackDepth.ToString("F2", CultureInfo.InvariantCulture),
                    altStackDepthScale.ToString("F4", CultureInfo.InvariantCulture),
                    baseline.GasPerIteration.ToString("F6", CultureInfo.InvariantCulture),
                    single.GasPerIteration.ToString("F6", CultureInfo.InvariantCulture),
                    saturated.GasPerIteration.ToString("F6", CultureInfo.InvariantCulture),
                    gasIterationScale.ToString("F4", CultureInfo.InvariantCulture),
                    baseline.TotalDataBytes.ToString(CultureInfo.InvariantCulture),
                    single.TotalDataBytes.ToString(CultureInfo.InvariantCulture),
                    saturated.TotalDataBytes.ToString(CultureInfo.InvariantCulture),
                    baseline.TotalAllocatedBytes.ToString(CultureInfo.InvariantCulture),
                    single.TotalAllocatedBytes.ToString(CultureInfo.InvariantCulture),
                    saturated.TotalAllocatedBytes.ToString(CultureInfo.InvariantCulture),
                    baseline.TotalGasConsumed.ToString(CultureInfo.InvariantCulture),
                    single.TotalGasConsumed.ToString(CultureInfo.InvariantCulture),
                    saturated.TotalGasConsumed.ToString(CultureInfo.InvariantCulture)));
            }

            return scalingPath;
        }

        private sealed record ScalingMeasurement(
            BenchmarkComponent Component,
            string ScenarioId,
            BenchmarkOperationKind OperationKind,
            string OperationId,
            ScenarioComplexity Complexity,
            BenchmarkVariant Variant,
            double NanosecondsPerIteration,
            double NanosecondsPerByte,
            double AllocatedBytesPerIteration,
            double AllocatedBytesPerByte,
            long TotalDataBytes,
            long TotalAllocatedBytes,
            double AverageStackDepth,
            int PeakStackDepth,
            double AverageAltStackDepth,
            int PeakAltStackDepth,
            long TotalGasConsumed,
            double GasPerIteration)
        {
            public static bool TryParse(string line, out ScalingMeasurement measurement)
            {
                var parts = line.Split(',', StringSplitOptions.None);
                if (parts.Length < 28)
                {
                    measurement = default!;
                    return false;
                }

                try
                {
                    var component = Enum.Parse<BenchmarkComponent>(parts[0], ignoreCase: false);
                    var scenarioId = parts[1];
                    var operationKind = Enum.Parse<BenchmarkOperationKind>(parts[2], ignoreCase: false);
                    var operationId = parts[3];
                    var complexity = Enum.Parse<ScenarioComplexity>(parts[4], ignoreCase: false);
                    var variant = Enum.Parse<BenchmarkVariant>(parts[5], ignoreCase: false);
                    var totalDataBytes = long.Parse(parts[10], CultureInfo.InvariantCulture);
                    var nanosecondsPerIteration = double.Parse(parts[15], CultureInfo.InvariantCulture);
                    var nanosecondsPerByte = double.Parse(parts[16], CultureInfo.InvariantCulture);
                    var totalAllocatedBytes = long.Parse(parts[18], CultureInfo.InvariantCulture);
                    var allocatedPerIteration = double.Parse(parts[19], CultureInfo.InvariantCulture);
                    var allocatedPerByte = double.Parse(parts[20], CultureInfo.InvariantCulture);
                    var averageStack = double.Parse(parts[22], CultureInfo.InvariantCulture);
                    var peakStack = int.Parse(parts[23], CultureInfo.InvariantCulture);
                    var averageAltStack = double.Parse(parts[24], CultureInfo.InvariantCulture);
                    var peakAltStack = int.Parse(parts[25], CultureInfo.InvariantCulture);
                    var totalGas = long.Parse(parts[26], CultureInfo.InvariantCulture);
                    var gasPerIteration = double.Parse(parts[27], CultureInfo.InvariantCulture);

                    measurement = new ScalingMeasurement(component, scenarioId, operationKind, operationId, complexity, variant, nanosecondsPerIteration, nanosecondsPerByte, allocatedPerIteration, allocatedPerByte, totalDataBytes, totalAllocatedBytes, averageStack, peakStack, averageAltStack, peakAltStack, totalGas, gasPerIteration);
                    return true;
                }
                catch
                {
                    measurement = default!;
                    return false;
                }
            }
        }
    }
}
