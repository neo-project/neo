// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractManualRunner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

#nullable enable

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Provides a lightweight fallback harness that executes native contract benchmark cases without BenchmarkDotNet.
    /// </summary>
    internal static class NativeContractManualRunner
    {
        private const string DefaultOutputDirectory = "BenchmarkDotNet.Artifacts/manual";

        internal sealed record ManualRunnerArguments(
            bool RunManualSuite,
            string[] ForwardedArgs,
            string? OutputOverride,
            int? IterationOverride,
            int? WarmupOverride,
            bool? VerboseOverride,
            string? ContractFilter,
            string? MethodFilter,
            string? SizeFilter,
            string? LimitOverride,
            string? JobOverride);

        internal sealed record NativeContractManualRunnerOptions(
            string OutputDirectory,
            int Iterations,
            int WarmupIterations,
            int TrimPercentage,
            bool Verbose);

        private sealed record ManualResult(
            NativeContractBenchmarkCase Case,
            double? MeanMilliseconds,
            double? StdDevMilliseconds,
            double? MinMilliseconds,
            double? MaxMilliseconds,
            double? MedianMilliseconds,
            double? Percentile95Milliseconds,
            string? Error);

        private readonly struct SampleStatistics
        {
            public SampleStatistics(double mean, double stdDev, double min, double max, double median, double percentile95)
            {
                Mean = mean;
                StdDev = stdDev;
                Min = min;
                Max = max;
                Median = median;
                Percentile95 = percentile95;
            }

            public double Mean { get; }
            public double StdDev { get; }
            public double Min { get; }
            public double Max { get; }
            public double Median { get; }
            public double Percentile95 { get; }
        }

        public static ManualRunnerArguments ParseArguments(ReadOnlySpan<string> args)
        {
            List<string> forwarded = new();
            bool manual = false;
            string? output = null;
            int? iterations = null;
            int? warmup = null;
            bool? verbose = null;
            string? contractFilter = null;
            string? methodFilter = null;
            string? sizeFilter = null;
            string? limitOverride = null;
            string? jobOverride = null;

            for (int i = 0; i < args.Length; i++)
            {
                var current = args[i];
                if (IsSwitch(current, "--native-manual-run"))
                {
                    manual = true;
                    continue;
                }

                if (IsSwitch(current, "--native-output"))
                {
                    manual = true;
                    output = ReadValue(args, ref i, "--native-output");
                    continue;
                }

                if (IsSwitch(current, "--native-iterations"))
                {
                    manual = true;
                    iterations = ParsePositiveInt(ReadValue(args, ref i, "--native-iterations"), "--native-iterations");
                    continue;
                }

                if (IsSwitch(current, "--native-warmup"))
                {
                    manual = true;
                    warmup = ParseNonNegativeInt(ReadValue(args, ref i, "--native-warmup"), "--native-warmup");
                    continue;
                }

                if (IsSwitch(current, "--native-verbose"))
                {
                    manual = true;
                    verbose = true;
                    continue;
                }

                if (IsSwitch(current, "--native-contract"))
                {
                    manual = true;
                    contractFilter = ReadValue(args, ref i, "--native-contract");
                    continue;
                }

                if (IsSwitch(current, "--native-method"))
                {
                    manual = true;
                    methodFilter = ReadValue(args, ref i, "--native-method");
                    continue;
                }

                if (IsSwitch(current, "--native-sizes"))
                {
                    manual = true;
                    sizeFilter = ReadValue(args, ref i, "--native-sizes");
                    continue;
                }

                if (IsSwitch(current, "--native-limit"))
                {
                    manual = true;
                    limitOverride = ReadValue(args, ref i, "--native-limit");
                    continue;
                }

                if (IsSwitch(current, "--native-job"))
                {
                    manual = true;
                    jobOverride = ReadValue(args, ref i, "--native-job");
                    continue;
                }

                forwarded.Add(current);
            }

            return new ManualRunnerArguments(
                manual,
                forwarded.ToArray(),
                output,
                iterations,
                warmup,
                verbose,
                contractFilter,
                methodFilter,
                sizeFilter,
                limitOverride,
                jobOverride);
        }

        public static NativeContractManualRunnerOptions CreateOptions(ManualRunnerArguments arguments)
        {
            var profileToken = arguments.JobOverride
                ?? Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_JOB")
                ?? Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_PROFILE");
            var profile = ResolveProfile(profileToken);

            var iterations = arguments.IterationOverride
                ?? TryParseEnvironmentInt("NEO_NATIVE_BENCH_ITERATIONS")
                ?? profile.Iterations;

            var warmup = arguments.WarmupOverride
                ?? TryParseEnvironmentInt("NEO_NATIVE_BENCH_WARMUP")
                ?? profile.Warmup;

            var verbose = arguments.VerboseOverride
                ?? ParseEnvironmentBool("NEO_NATIVE_BENCH_VERBOSE", defaultValue: false);

            var output = arguments.OutputOverride ?? DefaultOutputDirectory;
            if (!Path.IsPathRooted(output))
                output = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, output));

            return new NativeContractManualRunnerOptions(
                output,
                iterations,
                warmup,
                profile.TrimPercentage,
                verbose);
        }

        public static int Run(NativeContractManualRunnerOptions options, ManualRunnerArguments arguments)
        {
            using var scope = ApplyFilterOverrides(arguments);
            using var suite = NativeContractBenchmarkSuite.CreateDefault();
            if (suite.Cases.Count == 0)
            {
                Console.Error.WriteLine("[ManualRunner] No native contract scenarios discovered. Check filter environment variables.");
                DumpDiagnostics(suite.Diagnostics);
                return 1;
            }

            Console.WriteLine($"[ManualRunner] Executing {suite.Cases.Count} scenario(s) with {options.Iterations} iteration(s), {options.WarmupIterations} warmup pass(es), trim={options.TrimPercentage}%.");

            List<ManualResult> results = new(suite.Cases.Count);

            foreach (var benchmarkCase in suite.Cases)
            {
                var invoker = suite.CreateInvoker(benchmarkCase);
                try
                {
                    for (int i = 0; i < options.WarmupIterations; i++)
                        invoker.Invoke();

                    var samples = new double[options.Iterations];
                    for (int i = 0; i < options.Iterations; i++)
                    {
                        var sw = Stopwatch.StartNew();
                        invoker.Invoke();
                        sw.Stop();
                        samples[i] = sw.Elapsed.TotalMilliseconds;
                    }

                    var statistics = CalculateStatistics(samples, options.TrimPercentage);
                    var result = new ManualResult(
                        benchmarkCase,
                        statistics.Mean,
                        statistics.StdDev,
                        statistics.Min,
                        statistics.Max,
                        statistics.Median,
                        statistics.Percentile95,
                        null);
                    results.Add(result);

                    if (options.Verbose)
                    {
                        Console.WriteLine($"[ManualRunner] {benchmarkCase.MethodDisplayName} [{benchmarkCase.Profile.Size}] " +
                            $"Mean={statistics.Mean:n3} ms | Median={statistics.Median:n3} ms | P95={statistics.Percentile95:n3} ms | StdDev={statistics.StdDev:n3} ms | Min={statistics.Min:n3} ms | Max={statistics.Max:n3} ms");
                    }
                }
                catch (Exception ex)
                {
                    var baseException = ex;
                    while (baseException.InnerException is not null)
                        baseException = baseException.InnerException;

                    var message = $"{baseException.GetType().Name}: {baseException.Message}";
                    Console.Error.WriteLine($"[ManualRunner] {benchmarkCase.MethodDisplayName} [{benchmarkCase.Profile.Size}] failed: {message}");
                    if (options.Verbose && baseException.StackTrace is not null)
                        Console.Error.WriteLine(baseException.StackTrace);
                    results.Add(new ManualResult(benchmarkCase, null, null, null, null, null, null, message));
                }
            }

            var summaryPath = WriteSummary(options, suite, results);
            var jsonPath = WriteJsonSummary(options, suite, results);
            var htmlPath = WriteHtmlSummary(options, suite, results);
            Console.WriteLine($"[ManualRunner] Summary written to {summaryPath}");
            Console.WriteLine($"[ManualRunner] JSON report written to {jsonPath}");
            Console.WriteLine($"[ManualRunner] HTML report written to {htmlPath}");
            return 0;
        }

        private static string WriteSummary(
            NativeContractManualRunnerOptions options,
            NativeContractBenchmarkSuite suite,
            IReadOnlyCollection<ManualResult> results)
        {
            Directory.CreateDirectory(options.OutputDirectory);
            var path = Path.Combine(options.OutputDirectory, "manual-native-contract-summary.txt");

            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            writer.WriteLine("Manual Native Contract Benchmark Summary");
            writer.WriteLine($"Generated at {DateTimeOffset.UtcNow:u}");
            writer.WriteLine($"Iterations per case: {options.Iterations} (warmup {options.WarmupIterations})");
            writer.WriteLine($"Discovered {suite.Cases.Count} scenario(s); executed {results.Count} scenario(s).");
            writer.WriteLine($"Filters -> Contract: {Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_CONTRACT") ?? "(none)"}, " +
                $"Method: {Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_METHOD") ?? "(none)"}, " +
                $"Sizes: {Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_SIZES") ?? "(all)"}.");
            writer.WriteLine();

            if (suite.Diagnostics.Count > 0)
            {
                writer.WriteLine("Diagnostics:");
                foreach (var diagnostic in suite.Diagnostics)
                    writer.WriteLine($"  - {diagnostic}");
                writer.WriteLine();
            }

            var grouped = results
                .GroupBy(r => r.Case.ContractName, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal);

            foreach (var contractGroup in grouped)
            {
                writer.WriteLine(contractGroup.Key);
                foreach (var entry in contractGroup
                    .OrderBy(r => r.Case.MethodName, StringComparer.Ordinal)
                    .ThenBy(r => r.Case.Profile.Size))
                {
                    var caseInfo = entry.Case;
                    if (entry.Error is not null)
                    {
                        writer.WriteLine(
                            $"  - {caseInfo.MethodName} [{caseInfo.Profile.Size}] FAILED: {entry.Error}");
                    }
                    else
                    {
                        writer.WriteLine(
                            $"  - {caseInfo.MethodName} [{caseInfo.Profile.Size}] Mean: {ToNanoseconds(entry.MeanMilliseconds!.Value)} ns | " +
                            $"Median: {ToNanoseconds(entry.MedianMilliseconds!.Value)} ns | P95: {ToNanoseconds(entry.Percentile95Milliseconds!.Value)} ns | " +
                            $"StdDev: {ToNanoseconds(entry.StdDevMilliseconds!.Value)} ns | CpuFee: {caseInfo.CpuFee} | StorageFee: {caseInfo.StorageFee} | {caseInfo.RequiredCallFlags}");
                    }
                }

                var (aggMean, aggMedian, aggP95) = AggregateContractMetrics(contractGroup);
                writer.WriteLine(
                    $"    Aggregate Mean: {ToNanoseconds(aggMean)} ns | Median: {ToNanoseconds(aggMedian)} ns | P95: {ToNanoseconds(aggP95)} ns");
                writer.WriteLine();
            }

            if (!grouped.Any())
                writer.WriteLine("No benchmark cases executed. Verify configuration and input filters.");

            var successful = results
                .Where(r => r.MeanMilliseconds.HasValue)
                .OrderByDescending(r => r.MeanMilliseconds!.Value)
                .Take(10)
                .ToList();

            if (successful.Count > 0)
            {
                writer.WriteLine("Top scenarios by mean duration:");
                foreach (var entry in successful)
                {
                    writer.WriteLine(
                        $"  - {entry.Case.MethodDisplayName} [{entry.Case.Profile.Size}] " +
                        $"{ToNanoseconds(entry.MeanMilliseconds!.Value):N1} ns");
                }
            }

            return path;
        }

        private static string WriteJsonSummary(
            NativeContractManualRunnerOptions options,
            NativeContractBenchmarkSuite suite,
            IReadOnlyCollection<ManualResult> results)
        {
            Directory.CreateDirectory(options.OutputDirectory);
            var path = Path.Combine(options.OutputDirectory, "manual-native-contract-summary.json");

            var payload = new
            {
                generatedAt = DateTimeOffset.UtcNow,
                iterations = options.Iterations,
                warmupIterations = options.WarmupIterations,
                trimPercentage = options.TrimPercentage,
                diagnostics = suite.Diagnostics,
                filters = new
                {
                    contract = Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_CONTRACT"),
                    method = Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_METHOD"),
                    sizes = Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_SIZES"),
                    limit = Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_LIMIT")
                },
                contractAggregates = results
                    .Where(r => r.MeanMilliseconds.HasValue)
                    .GroupBy(r => r.Case.ContractName)
                    .Select(g =>
                    {
                        var (mean, median, p95) = AggregateContractMetrics(g);
                        return new
                        {
                            contract = g.Key,
                            caseCount = g.Count(),
                            meanNanoseconds = ToNanoseconds(mean),
                            medianNanoseconds = ToNanoseconds(median),
                            percentile95Nanoseconds = ToNanoseconds(p95)
                        };
                    })
                    .ToList(),
                cases = results.Select(r => new
                {
                    contract = r.Case.ContractName,
                    method = r.Case.MethodName,
                    profile = r.Case.Profile.Size.ToString(),
                    scenario = r.Case.ScenarioName,
                    cpuFee = r.Case.CpuFee,
                    storageFee = r.Case.StorageFee,
                    callFlags = r.Case.RequiredCallFlags.ToString(),
                    parameterSummary = r.Case.ParameterSummary,
                    meanNanoseconds = r.MeanMilliseconds.HasValue ? ToNanoseconds(r.MeanMilliseconds.Value) : (double?)null,
                    stdDevNanoseconds = r.StdDevMilliseconds.HasValue ? ToNanoseconds(r.StdDevMilliseconds.Value) : (double?)null,
                    minNanoseconds = r.MinMilliseconds.HasValue ? ToNanoseconds(r.MinMilliseconds.Value) : (double?)null,
                    maxNanoseconds = r.MaxMilliseconds.HasValue ? ToNanoseconds(r.MaxMilliseconds.Value) : (double?)null,
                    medianNanoseconds = r.MedianMilliseconds.HasValue ? ToNanoseconds(r.MedianMilliseconds.Value) : (double?)null,
                    percentile95Nanoseconds = r.Percentile95Milliseconds.HasValue ? ToNanoseconds(r.Percentile95Milliseconds.Value) : (double?)null,
                    error = r.Error
                })
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(path, json, Encoding.UTF8);
            return path;
        }

        private static string WriteHtmlSummary(
            NativeContractManualRunnerOptions options,
            NativeContractBenchmarkSuite suite,
            IReadOnlyCollection<ManualResult> results)
        {
            Directory.CreateDirectory(options.OutputDirectory);
            var path = Path.Combine(options.OutputDirectory, "manual-native-contract-summary.html");

            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            writer.WriteLine("<!DOCTYPE html>");
            writer.WriteLine("<html lang=\"en\"><head>");
            writer.WriteLine("<meta charset=\"utf-8\"/>");
            writer.WriteLine("<title>Native Contract Benchmarks</title>");
            writer.WriteLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:2rem;}table{border-collapse:collapse;width:100%;margin-bottom:1.5rem;}th,td{border:1px solid #ccc;padding:0.4rem;text-align:left;}th{background:#f5f5f5;}caption{font-weight:bold;margin-bottom:0.5rem;}</style>");
            writer.WriteLine("</head><body>");
            writer.WriteLine($"<h1>Native Contract Benchmarks</h1>");
            writer.WriteLine($"<p>Generated at {DateTimeOffset.UtcNow:u}. Iterations: {options.Iterations}, Warmup: {options.WarmupIterations}, Trim: {options.TrimPercentage}%.</p>");

            var grouped = results
                .GroupBy(r => r.Case.ContractName, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal);

            foreach (var contractGroup in grouped)
            {
                writer.WriteLine($"<h2>{contractGroup.Key}</h2>");
                writer.WriteLine("<table>");
                writer.WriteLine("<thead><tr>" +
                    "<th>Method</th><th>Profile</th><th>Mean (µs)</th><th>Median (µs)</th><th>P95 (µs)</th><th>StdDev (µs)</th><th>Min (µs)</th><th>Max (µs)</th><th>Status</th>" +
                    "</tr></thead><tbody>");

                bool isGray = false;
                var methodGroups = contractGroup
                    .GroupBy(r => r.Case.MethodName, StringComparer.Ordinal)
                    .OrderBy(g => g.Key, StringComparer.Ordinal);

                foreach (var methodGroup in methodGroups)
                {
                    isGray = !isGray;
                    bool highlight = ShouldHighlightVariance(methodGroup);

                    foreach (var entry in methodGroup.OrderBy(r => r.Case.Profile.Size))
                    {
                        var styleParts = new List<string>();
                        if (isGray)
                            styleParts.Add("background:#f9f9f9;");
                        if (highlight)
                            styleParts.Add("color:#b00020;font-weight:bold;");
                        var rowStyle = styleParts.Count > 0 ? $" style=\"{string.Concat(styleParts)}\"" : string.Empty;

                        if (entry.MeanMilliseconds.HasValue)
                        {
                            writer.WriteLine($"<tr{rowStyle}>" +
                                $"<td>{entry.Case.MethodName}</td>" +
                                $"<td>{entry.Case.Profile.Size}</td>" +
                                $"<td>{ToMicroseconds(entry.MeanMilliseconds.Value):n3}</td>" +
                                $"<td>{ToMicroseconds((entry.MedianMilliseconds ?? entry.MeanMilliseconds).Value):n3}</td>" +
                                $"<td>{ToMicroseconds((entry.Percentile95Milliseconds ?? entry.MeanMilliseconds).Value):n3}</td>" +
                                $"<td>{ToMicroseconds(entry.StdDevMilliseconds!.Value):n3}</td>" +
                                $"<td>{ToMicroseconds(entry.MinMilliseconds!.Value):n3}</td>" +
                                $"<td>{ToMicroseconds(entry.MaxMilliseconds!.Value):n3}</td>" +
                                "<td>OK</td>" +
                                "</tr>");
                        }
                        else
                        {
                            writer.WriteLine($"<tr{rowStyle}>" +
                                $"<td>{entry.Case.MethodName}</td>" +
                                $"<td>{entry.Case.Profile.Size}</td>" +
                                "<td colspan=\"6\">n/a</td>" +
                                $"<td>{entry.Error}</td>" +
                                "</tr>");
                        }
                    }
                }
                writer.WriteLine("</tbody></table>");
            }

            writer.WriteLine("</body></html>");
            return path;
        }

        // No engine-specific extraction needed currently.

        private static double ToNanoseconds(double milliseconds) => milliseconds * 1_000_000.0;
        private static double ToMicroseconds(double milliseconds) => milliseconds * 1_000.0;

        private static void DumpDiagnostics(IReadOnlyList<string> diagnostics)
        {
            foreach (var line in diagnostics)
                Console.Error.WriteLine($"  - {line}");
        }

        private static bool IsSwitch(string value, string option) =>
            string.Equals(value, option, StringComparison.OrdinalIgnoreCase);

        private static string ReadValue(ReadOnlySpan<string> args, ref int index, string option)
        {
            if (index + 1 >= args.Length)
                throw new ArgumentException($"Expected value after {option}.");
            return args[++index];
        }

        private static int ParsePositiveInt(string value, string option)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
                throw new ArgumentException($"{option} expects a positive integer value.");
            return parsed;
        }

        private static int ParseNonNegativeInt(string value, string option)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
                throw new ArgumentException($"{option} expects a non-negative integer value.");
            return parsed;
        }

        private static int ParseEnvironmentInt(string name, int defaultValue, int min)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed >= min)
                return parsed;

            Console.Error.WriteLine($"[ManualRunner] Ignored invalid value '{value}' for {name}. Using {defaultValue}.");
            return defaultValue;
        }

        private static bool ParseEnvironmentBool(string name, bool defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static int? TryParseEnvironmentInt(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private sealed record ManualRunnerProfile(string Name, int Iterations, int Warmup, int TrimPercentage);

        private static ManualRunnerProfile ResolveProfile(string? token)
        {
            var normalized = token?.Trim().ToLowerInvariant();
            return normalized switch
            {
                "quick" or "fast" => new ManualRunnerProfile("Quick", 5, 0, 0),
                "short" or "ci" => new ManualRunnerProfile("Short", 15, 2, 5),
                "thorough" or "full" => new ManualRunnerProfile("Thorough", 40, 5, 15),
                "default" or "standard" => new ManualRunnerProfile("Balanced", 20, 3, 10),
                _ => new ManualRunnerProfile("Balanced", 20, 3, 10)
            };
        }

        private static SampleStatistics CalculateStatistics(double[] samples, int trimPercentage)
        {
            if (samples is null || samples.Length == 0)
                return new SampleStatistics(0, 0, 0, 0, 0, 0);

            Array.Sort(samples);
            double min = samples[0];
            double max = samples[^1];
            double median = samples.Length % 2 == 0
                ? (samples[samples.Length / 2 - 1] + samples[samples.Length / 2]) / 2.0
                : samples[samples.Length / 2];
            double percentile95 = samples[(int)Math.Floor(0.95 * (samples.Length - 1))];

            int trimCount = 0;
            if (trimPercentage > 0)
            {
                trimCount = (int)Math.Floor(samples.Length * (trimPercentage / 100.0));
                trimCount = Math.Min(trimCount, (samples.Length - 1) / 2);
            }

            int usableLength = samples.Length - (trimCount * 2);
            if (usableLength <= 0)
                usableLength = samples.Length;

            int start = trimCount;
            double sum = 0;
            for (int i = start; i < start + usableLength; i++)
                sum += samples[i];
            double mean = sum / usableLength;

            double variance = 0;
            for (int i = start; i < start + usableLength; i++)
            {
                var delta = samples[i] - mean;
                variance += delta * delta;
            }
            double stdDev = Math.Sqrt(variance / usableLength);

            return new SampleStatistics(mean, stdDev, min, max, median, percentile95);
        }

        private static (double Mean, double Median, double Percentile95) AggregateContractMetrics(IEnumerable<ManualResult> results)
        {
            var entries = results
                .Where(r => r.MeanMilliseconds.HasValue)
                .ToList();

            if (entries.Count == 0)
                return (0, 0, 0);

            double mean = entries.Average(r => r.MeanMilliseconds!.Value);

            double median = entries
                .Select(r => r.MedianMilliseconds ?? r.MeanMilliseconds!.Value)
                .Average();

            double p95 = entries
                .Select(r => r.Percentile95Milliseconds ?? r.MeanMilliseconds!.Value)
                .Average();

            return (mean, median, p95);
        }

        private static bool ShouldHighlightVariance(IEnumerable<ManualResult> methodResults)
        {
            var results = methodResults.ToList();
            var tiny = results.FirstOrDefault(r => r.Case.Profile.Size == NativeContractInputSize.Tiny);
            var large = results.FirstOrDefault(r => r.Case.Profile.Size == NativeContractInputSize.Large);
            if (tiny?.MeanMilliseconds is null || large?.MeanMilliseconds is null)
                return false;

            var tinyMean = tiny.MeanMilliseconds.Value;
            var largeMean = large.MeanMilliseconds.Value;
            if (tinyMean <= 0 || largeMean <= 0)
                return false;

            var ratio = tinyMean > largeMean ? tinyMean / largeMean : largeMean / tinyMean;
            return ratio >= 2.0;
        }

        private static IDisposable ApplyFilterOverrides(ManualRunnerArguments arguments)
        {
            Dictionary<string, string?> overrides = new();

            void Capture(string? candidate, string key)
            {
                if (candidate is not null)
                    overrides[key] = candidate;
            }

            Capture(arguments.ContractFilter, "NEO_NATIVE_BENCH_CONTRACT");
            Capture(arguments.MethodFilter, "NEO_NATIVE_BENCH_METHOD");
            Capture(arguments.SizeFilter, "NEO_NATIVE_BENCH_SIZES");
            Capture(arguments.LimitOverride, "NEO_NATIVE_BENCH_LIMIT");
            Capture(arguments.JobOverride, "NEO_NATIVE_BENCH_JOB");

            if (overrides.Count == 0)
                return NullScope.Instance;

            return new EnvOverrideScope(overrides);
        }

        private sealed class EnvOverrideScope : IDisposable
        {
            private readonly Dictionary<string, string?> _previous = new();
            private bool _disposed;

            public EnvOverrideScope(Dictionary<string, string?> overrides)
            {
                foreach (var key in overrides.Keys)
                    _previous[key] = Environment.GetEnvironmentVariable(key);

                foreach (var (key, value) in overrides)
                    Environment.SetEnvironmentVariable(key, value);

                NativeContractBenchmarkOptions.Reload();
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                foreach (var (key, value) in _previous)
                    Environment.SetEnvironmentVariable(key, value);

                NativeContractBenchmarkOptions.Reload();
                _disposed = true;
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
