// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkArtifactRegistry.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Tracks benchmark artifact files so we can produce a consolidated report once all suites finish.
    /// </summary>
    internal static class BenchmarkArtifactRegistry
    {
        private static readonly object s_sync = new();
        private static readonly List<(BenchmarkComponent Component, string Path)> s_metricArtifacts = new();
        private static readonly List<(string Category, string Path)> s_coverageArtifacts = new();

        public static void Reset()
        {
            lock (s_sync)
            {
                s_metricArtifacts.Clear();
                s_coverageArtifacts.Clear();
            }
        }

        public static void RegisterMetrics(BenchmarkComponent component, string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            lock (s_sync)
            {
                if (!s_metricArtifacts.Any(entry => entry.Path == path))
                    s_metricArtifacts.Add((component, path));
            }
        }

        public static void RegisterCoverage(string category, string path)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(path))
                return;

            lock (s_sync)
            {
                if (!s_coverageArtifacts.Any(entry => entry.Path == path))
                    s_coverageArtifacts.Add((category, path));
            }
        }

        public static IReadOnlyList<(BenchmarkComponent Component, string Path)> GetMetricArtifacts()
        {
            lock (s_sync)
            {
                return s_metricArtifacts.OrderBy(static entry => entry.Component).ThenBy(static entry => entry.Path).ToArray();
            }
        }

        public static IReadOnlyList<(string Category, string Path)> GetCoverageArtifacts()
        {
            lock (s_sync)
            {
                return s_coverageArtifacts.OrderBy(static entry => entry.Category).ThenBy(static entry => entry.Path).ToArray();
            }
        }

        public static void CollectFromDisk(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return;

            CollectDirectory(root);

            var parent = Path.GetDirectoryName(root);
            if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                return;

            foreach (var jobDir in Directory.EnumerateDirectories(parent, "Neo.VM.Benchmarks-*", SearchOption.TopDirectoryOnly))
            {
                var jobArtifacts = Path.Combine(jobDir, "BenchmarkArtifacts");
                if (Directory.Exists(jobArtifacts))
                    CollectDirectory(jobArtifacts);
            }
        }

        private static void CollectDirectory(string directory)
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*-metrics-*.csv", SearchOption.AllDirectories))
            {
                RegisterMetrics(InferComponent(file), file);
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*-missing.csv", SearchOption.AllDirectories))
            {
                var category = Path.GetFileNameWithoutExtension(file);
                RegisterCoverage(category ?? string.Empty, file);
            }
        }

        private static BenchmarkComponent InferComponent(string path)
        {
            var fileName = Path.GetFileName(path) ?? string.Empty;
            if (fileName.Contains("opcode", StringComparison.OrdinalIgnoreCase))
                return BenchmarkComponent.Opcode;
            if (fileName.Contains("syscall", StringComparison.OrdinalIgnoreCase))
                return BenchmarkComponent.Syscall;
            if (fileName.Contains("native", StringComparison.OrdinalIgnoreCase))
                return BenchmarkComponent.NativeContract;
            return BenchmarkComponent.Opcode;
        }
    }
}
