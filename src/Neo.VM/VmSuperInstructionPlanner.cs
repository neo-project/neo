// Copyright (C) 2015-2025 The Neo Project.
//
// VmSuperInstructionPlanner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neo.VM
{
    /// <summary>
    /// Bridges runtime trace profiling into super-instruction planning hints that can
    /// be consumed by JIT pipelines or in-process optimisations.
    /// </summary>
    public static class VmSuperInstructionPlanner
    {
        private const double MinimumHitRatio = 0.05d;
        private const int MinimumHitCount = 32;
        private const int MaxPlansPerScript = 32;
        private const int WindowSize = VmTraceProfiler.WindowSize;
        private static readonly ulong WindowMask = WindowSize >= 8 ? ulong.MaxValue : (1UL << (WindowSize * 8)) - 1UL;

        private static readonly object SyncRoot = new();
        private static readonly Dictionary<string, Dictionary<ulong, Plan>> PlansByScript = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<ulong, Plan> GlobalPlans = new();

        private static readonly HashSet<OpCode> DisallowedOpcodes = new()
        {
            OpCode.JMP,
            OpCode.JMPIF,
            OpCode.JMPIFNOT,
            OpCode.JMPEQ,
            OpCode.JMPNE,
            OpCode.JMPGT,
            OpCode.JMPGE,
            OpCode.JMPLT,
            OpCode.JMPLE,
            OpCode.JMP_L,
            OpCode.JMPIF_L,
            OpCode.JMPIFNOT_L,
            OpCode.JMPEQ_L,
            OpCode.JMPNE_L,
            OpCode.JMPGT_L,
            OpCode.JMPGE_L,
            OpCode.JMPLT_L,
            OpCode.JMPLE_L,
            OpCode.CALL,
            OpCode.CALL_L,
            OpCode.CALLA,
            OpCode.CALLT,
            OpCode.TRY,
            OpCode.TRY_L,
            OpCode.ENDTRY,
            OpCode.ENDTRY_L,
            OpCode.ASSERT,
            OpCode.ASSERTMSG,
            OpCode.THROW,
            OpCode.RET,
            OpCode.PUSHA
        };

        private sealed class Plan
        {
            internal Plan(OpCode[] sequence, double hitRatio, int hitCount)
            {
                Sequence = sequence;
                HitRatio = hitRatio;
                HitCount = hitCount;
                LastUpdatedUtc = DateTimeOffset.UtcNow;
            }

            internal OpCode[] Sequence { get; }
            internal double HitRatio { get; }
            internal int HitCount { get; }
            internal DateTimeOffset LastUpdatedUtc { get; }
        }

        private sealed class PlannerContextState
        {
            internal string? ScriptKey { get; set; }
        }

        public readonly record struct SuperInstructionSuggestion(
            string Script,
            string Sequence,
            double HitRatio,
            int HitCount,
            DateTimeOffset LastUpdatedUtc);

        /// <summary>
        /// Registers a newly generated trace profile.
        /// </summary>
        public static void RegisterTraceRecord(VmTraceProfiler.VmTraceRecord record)
        {
            if (record.Sequence.Length != WindowSize)
                return;
            if (record.HitCount < MinimumHitCount)
                return;

            var ratio = record.TotalInstructions == 0 ? 0d : (double)record.HitCount / record.TotalInstructions;
            if (ratio < MinimumHitRatio)
                return;

            if (!IsEligibleSequence(record.Sequence))
                return;

            var sequence = (OpCode[])record.Sequence.Clone();
            var key = ComputeKey(sequence);
            var scriptKey = Convert.ToHexString(record.ScriptHash);
            var plan = new Plan(sequence, ratio, record.HitCount);

            lock (SyncRoot)
            {
                if (!PlansByScript.TryGetValue(scriptKey, out var bucket))
                {
                    bucket = new Dictionary<ulong, Plan>();
                    PlansByScript[scriptKey] = bucket;
                }

                if (!bucket.ContainsKey(key) && bucket.Count >= MaxPlansPerScript)
                {
                    var weakest = bucket.OrderBy(kv => kv.Value.HitCount).First();
                    if (weakest.Value.HitCount >= plan.HitCount)
                    {
                        // Existing plans are stronger; ignore the new hint.
                        return;
                    }
                    bucket.Remove(weakest.Key);
                }

                bucket[key] = plan;

                if (!GlobalPlans.TryGetValue(key, out var existing) || existing.HitCount < plan.HitCount)
                {
                    GlobalPlans[key] = plan;
                }
            }
        }

        /// <summary>
        /// Attempts to determine whether a super-instruction plan applies at the current instruction pointer.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>The number of instructions to fold, or 0 when no plan applies.</returns>
        public static int TryGetPlanLength(ExecutionContext context)
        {
            if (PlansByScript.Count == 0 && GlobalPlans.Count == 0)
                return 0;

            var state = context.GetState<PlannerContextState>();
            if (state.ScriptKey is null)
            {
                state.ScriptKey = Convert.ToHexString(VmTraceProfiler.ComputeScriptHash(context.Script));
            }

            Span<OpCode> buffer = stackalloc OpCode[WindowSize];
            if (!TryBuildWindow(context, buffer, out var key))
                return 0;

            lock (SyncRoot)
            {
                if (TryGetPlan(state.ScriptKey, key, buffer, out var plan) ||
                    TryGetGlobalPlan(key, buffer, out plan))
                {
                    return plan!.Sequence.Length;
                }
            }

            return 0;
        }

        /// <summary>
        /// Returns the number of active plans tracked by the planner.
        /// </summary>
        public static int GetPlanCount()
        {
            lock (SyncRoot)
            {
                return PlansByScript.Values.Sum(bucket => bucket.Count);
            }
        }

        /// <summary>
        /// Exports the active plan suggestions, capped per script.
        /// </summary>
        public static IReadOnlyList<SuperInstructionSuggestion> GetPlanSuggestions(int maxPerScript = 16)
        {
            lock (SyncRoot)
            {
                var suggestions = new List<SuperInstructionSuggestion>();
                foreach (var (script, bucket) in PlansByScript)
                {
                    foreach (var plan in bucket.Values
                        .OrderByDescending(p => p.HitRatio)
                        .ThenByDescending(p => p.HitCount)
                        .Take(Math.Max(1, maxPerScript)))
                    {
                        suggestions.Add(new SuperInstructionSuggestion(
                            script,
                            string.Join(' ', plan.Sequence),
                            Math.Round(plan.HitRatio, 6),
                            plan.HitCount,
                            plan.LastUpdatedUtc));
                    }
                }
                return suggestions;
            }
        }

        private static bool TryGetPlan(string scriptKey, ulong key, ReadOnlySpan<OpCode> window, [MaybeNullWhen(false)] out Plan? plan)
        {
            if (PlansByScript.TryGetValue(scriptKey, out var bucket) &&
                bucket.TryGetValue(key, out var candidate) &&
                SequenceMatches(candidate.Sequence, window))
            {
                plan = candidate;
                return true;
            }

            plan = null;
            return false;
        }

        private static bool TryGetGlobalPlan(ulong key, ReadOnlySpan<OpCode> window, [MaybeNullWhen(false)] out Plan? plan)
        {
            if (GlobalPlans.TryGetValue(key, out var candidate) &&
                SequenceMatches(candidate.Sequence, window))
            {
                plan = candidate;
                return true;
            }

            plan = null;
            return false;
        }

        private static bool TryBuildWindow(ExecutionContext context, Span<OpCode> buffer, out ulong key)
        {
            key = 0;
            int ip = context.InstructionPointer;
            var script = context.Script;

            for (int i = 0; i < WindowSize; i++)
            {
                var instruction = script.GetInstruction(ip);
                var op = instruction.OpCode;
                if (DisallowedOpcodes.Contains(op))
                    return false;

                buffer[i] = op;
                key = ((key << 8) | (byte)op) & WindowMask;
                ip += instruction.Size;

                if (ip > script.Length && i < WindowSize - 1)
                    return false;
            }

            return true;
        }

        private static bool IsEligibleSequence(IReadOnlyList<OpCode> sequence)
        {
            foreach (var opcode in sequence)
            {
                if (DisallowedOpcodes.Contains(opcode))
                    return false;
            }
            return true;
        }

        private static bool SequenceMatches(IReadOnlyList<OpCode> planSequence, ReadOnlySpan<OpCode> window)
        {
            if (planSequence.Count != window.Length)
                return false;

            for (int i = 0; i < window.Length; i++)
            {
                if (planSequence[i] != window[i])
                    return false;
            }
            return true;
        }

        private static ulong ComputeKey(ReadOnlySpan<OpCode> sequence)
        {
            ulong key = 0;
            foreach (var opcode in sequence)
            {
                key = ((key << 8) | (byte)opcode) & WindowMask;
            }
            return key;
        }
    }
}

