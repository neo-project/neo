// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractBenchmarkOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

#nullable enable

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Reads environment-driven filters that control which native contract benchmark scenarios are materialised.
    /// </summary>
    internal static class NativeContractBenchmarkOptions
    {
        private sealed record Pattern(string Raw, Regex Regex)
        {
            public bool IsMatch(string value) => Regex.IsMatch(value);
        }

        private sealed record Options(
            IReadOnlyCollection<NativeContractInputSize> AllowedSizes,
            IReadOnlyCollection<Pattern> ContractFilters,
            IReadOnlyCollection<Pattern> MethodFilters,
            int? CaseLimit,
            NativeContractBenchmarkJobMode Job,
            IReadOnlyList<string> Diagnostics);

        private static readonly object s_syncRoot = new();
        private static Lazy<Options> s_options = CreateLazy();

        public static IReadOnlyList<string> Diagnostics => s_options.Value.Diagnostics;

        public static NativeContractBenchmarkJobMode Job => s_options.Value.Job;

        public static bool IsSizeAllowed(NativeContractInputSize size)
        {
            var options = s_options.Value;
            return options.AllowedSizes.Count == 0 || options.AllowedSizes.Contains(size);
        }

        public static bool ShouldInclude(NativeContractBenchmarkCase benchmarkCase, out string? reason)
        {
            var options = s_options.Value;

            if (options.ContractFilters.Count > 0 && !options.ContractFilters.Any(pattern => pattern.IsMatch(benchmarkCase.ContractName)))
            {
                reason = $"Skipped {benchmarkCase.MethodDisplayName}: filtered by NEO_NATIVE_BENCH_CONTRACT.";
                return false;
            }

            if (options.MethodFilters.Count > 0 && !options.MethodFilters.Any(pattern => pattern.IsMatch(benchmarkCase.MethodName)))
            {
                reason = $"Skipped {benchmarkCase.MethodDisplayName}: filtered by NEO_NATIVE_BENCH_METHOD.";
                return false;
            }

            reason = null;
            return true;
        }

        public static IReadOnlyList<NativeContractBenchmarkCase> ApplyLimit(
            IReadOnlyList<NativeContractBenchmarkCase> cases,
            List<string> diagnostics)
        {
            var options = s_options.Value;
            if (options.CaseLimit is null || cases.Count <= options.CaseLimit.Value)
                return cases;

            diagnostics.Add($"Limited native contract benchmarks to {options.CaseLimit.Value} case(s) via NEO_NATIVE_BENCH_LIMIT.");
            return cases.Take(options.CaseLimit.Value).ToList();
        }

        public static void Reload()
        {
            lock (s_syncRoot)
            {
                s_options = CreateLazy();
            }
        }

        private static Lazy<Options> CreateLazy() => new(Create, LazyThreadSafetyMode.ExecutionAndPublication);

        private static Options Create()
        {
            List<string> diagnostics = [];

            var sizes = ParseSizes(Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_SIZES"), diagnostics);
            var contractFilters = ParsePatterns(Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_CONTRACT"), diagnostics);
            var methodFilters = ParsePatterns(Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_METHOD"), diagnostics);
            int? limit = ParseLimit(Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_LIMIT"), diagnostics);
            var job = ParseJob(Environment.GetEnvironmentVariable("NEO_NATIVE_BENCH_JOB"), diagnostics);

            return new Options(sizes, contractFilters, methodFilters, limit, job, diagnostics);
        }

        private static IReadOnlyCollection<NativeContractInputSize> ParseSizes(string? value, List<string> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<NativeContractInputSize>();

            HashSet<NativeContractInputSize> sizes = new();
            foreach (var token in Split(value))
            {
                if (Enum.TryParse<NativeContractInputSize>(token, true, out var size))
                {
                    sizes.Add(size);
                }
                else
                {
                    diagnostics.Add($"Ignored unknown value '{token}' in NEO_NATIVE_BENCH_SIZES. Accepted values: {string.Join(", ", Enum.GetNames<NativeContractInputSize>())}.");
                }
            }
            return new ReadOnlyCollection<NativeContractInputSize>(sizes.ToList());
        }

        private static IReadOnlyCollection<Pattern> ParsePatterns(string? value, List<string> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<Pattern>();

            List<Pattern> patterns = [];
            foreach (var token in Split(value))
            {
                try
                {
                    var regex = WildcardToRegex(token);
                    patterns.Add(new Pattern(token, regex));
                }
                catch (ArgumentException)
                {
                    diagnostics.Add($"Ignored invalid wildcard '{token}' in benchmark filter.");
                }
            }
            return new ReadOnlyCollection<Pattern>(patterns);
        }

        private static int? ParseLimit(string? value, List<string> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, out var limit) && limit > 0)
                return limit;

            diagnostics.Add($"Ignored NEO_NATIVE_BENCH_LIMIT='{value}'. Expected a positive integer.");
            return null;
        }

        private static NativeContractBenchmarkJobMode ParseJob(string? value, List<string> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(value))
                return NativeContractBenchmarkJobMode.Default;

            var token = value.Trim();
            switch (token.ToLowerInvariant())
            {
                case "quick":
                case "fast":
                    diagnostics.Add("Using Quick job profile via NEO_NATIVE_BENCH_JOB.");
                    return NativeContractBenchmarkJobMode.Quick;
                case "short":
                case "ci":
                    diagnostics.Add("Using Short job profile via NEO_NATIVE_BENCH_JOB.");
                    return NativeContractBenchmarkJobMode.Short;
                case "default":
                case "full":
                case "standard":
                    diagnostics.Add("Using Default job profile via NEO_NATIVE_BENCH_JOB.");
                    return NativeContractBenchmarkJobMode.Default;
                default:
                    diagnostics.Add($"Ignored unknown job profile '{token}' in NEO_NATIVE_BENCH_JOB. Supported values: Quick, Short, Default.");
                    return NativeContractBenchmarkJobMode.Default;
            }
        }

        private static IEnumerable<string> Split(string value) =>
            value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private static Regex WildcardToRegex(string pattern)
        {
            var escaped = Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".");
            return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    internal enum NativeContractBenchmarkJobMode
    {
        Default,
        Short,
        Quick
    }
}
