// Copyright (C) 2015-2025 The Neo Project.
//
// VmTraceProfileStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Neo.Plugins.OpenTelemetry
{
    internal sealed class VmTraceProfileStore : IDisposable
    {
        private readonly string _outputFile;
        private readonly object _syncRoot = new();
        private readonly Dictionary<string, TraceProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly Timer _flushTimer;
        private bool _disposed;

        internal VmTraceProfileStore(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            _outputFile = Path.Combine(outputDirectory, "vm-trace-profiles.json");
            VmTraceProfiler.TraceGenerated += OnTraceGenerated;
            _flushTimer = new Timer(_ => Flush(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private void OnTraceGenerated(VmTraceProfiler.VmTraceRecord record)
        {
            VmSuperInstructionPlanner.RegisterTraceRecord(record);

            var scriptHash = Convert.ToHexString(record.ScriptHash);
            var sequence = string.Join(" ", record.Sequence);
            var ratio = record.TotalInstructions == 0 ? 0d : (double)record.HitCount / record.TotalInstructions;
            lock (_syncRoot)
            {
                if (!_profiles.TryGetValue(scriptHash, out var profile) || record.HitCount > profile.HitCount)
                {
                    _profiles[scriptHash] = new TraceProfile(scriptHash, sequence, record.HitCount, record.TotalInstructions, ratio, DateTimeOffset.UtcNow);
                }
            }
        }

        internal double GetMaxHotRatio()
        {
            lock (_syncRoot)
            {
                return _profiles.Count == 0 ? 0d : _profiles.Values.Max(p => p.HitRatio);
            }
        }

        internal int GetProfileCount()
        {
            lock (_syncRoot)
            {
                return _profiles.Count;
            }
        }

        internal double GetMaxHotHits()
        {
            lock (_syncRoot)
            {
                return _profiles.Count == 0 ? 0d : _profiles.Values.Max(p => (double)p.HitCount);
            }
        }

        internal IReadOnlyList<TraceProfile> GetTopProfiles(int maxCount)
        {
            lock (_syncRoot)
            {
                if (_profiles.Count == 0) return Array.Empty<TraceProfile>();
                return _profiles.Values
                    .OrderByDescending(p => p.HitRatio)
                    .ThenByDescending(p => p.HitCount)
                    .Take(Math.Max(1, maxCount))
                    .ToArray();
            }
        }

        public void Flush()
        {
            TraceProfile[] snapshot;
            lock (_syncRoot)
            {
                snapshot = _profiles.Values.OrderByDescending(p => p.HitRatio).ThenByDescending(p => p.HitCount).ToArray();
            }

            var payload = new
            {
                generated_at_utc = DateTimeOffset.UtcNow,
                window = VmTraceProfilerRecordWindow,
                entries = snapshot
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_outputFile, json);

            var suggestions = VmSuperInstructionPlanner.GetPlanSuggestions();
            var superPayload = new
            {
                generated_at_utc = DateTimeOffset.UtcNow,
                window = VmTraceProfilerRecordWindow,
                plan_count = VmSuperInstructionPlanner.GetPlanCount(),
                entries = suggestions.Select(s => new
                {
                    script = s.Script,
                    sequence = s.Sequence,
                    hit_ratio = s.HitRatio,
                    hit_count = s.HitCount,
                    last_updated_utc = s.LastUpdatedUtc
                }).ToArray()
            };

            var superJson = JsonSerializer.Serialize(superPayload, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(_outputFile) ?? string.Empty, "vm-superinstructions.json"), superJson);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            VmTraceProfiler.TraceGenerated -= OnTraceGenerated;
            _flushTimer.Dispose();
            Flush();
        }

        private static int VmTraceProfilerRecordWindow => 6;

        internal sealed record TraceProfile(
            string ScriptHash,
            string HotSequence,
            int HitCount,
            int TotalInstructions,
            double HitRatio,
            DateTimeOffset LastUpdatedUtc);
    }
}
