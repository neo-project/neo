// Copyright (C) 2015-2025 The Neo Project.
//
// VmTraceProfiler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Neo.VM
{
    /// <summary>
    /// Observability hooks that record hot opcode traces per script.
    /// </summary>
    public static class VmTraceProfiler
    {
        public readonly record struct VmTraceRecord(byte[] ScriptHash, OpCode[] Sequence, int HitCount, int TotalInstructions);

        public static event Action<VmTraceRecord>? TraceGenerated;

        internal static bool IsEnabled => TraceGenerated != null;

        internal const int WindowSize = 6;
        private static readonly ulong _windowMask = WindowSize >= 8 ? ulong.MaxValue : (1UL << (WindowSize * 8)) - 1UL;
        private static readonly ConditionalWeakTable<ExecutionContext, TraceContext> _contexts = new();

        internal static TraceContext GetTraceContext(ExecutionContext context)
        {
            return _contexts.GetValue(context, _ => new TraceContext());
        }

        internal static void FinalizeTrace(ExecutionContext context)
        {
            if (!IsEnabled) return;
            if (!_contexts.TryGetValue(context, out var traceContext))
                return;
            if (traceContext.TryCreateRecord(context.Script, out var record))
            {
                TraceGenerated?.Invoke(record);
            }
            _contexts.Remove(context);
        }

        internal sealed class TraceContext
        {
            private ulong _rollingKey;
            private int _stepsObserved;
            private int _instructionCount;
            private readonly Dictionary<ulong, int> _hotSequences = new();

            internal void Record(OpCode opcode)
            {
                _instructionCount++;
                _rollingKey = ((_rollingKey << 8) | (byte)opcode) & _windowMask;
                if (_stepsObserved < WindowSize - 1)
                {
                    _stepsObserved++;
                    return;
                }

                if (_hotSequences.TryGetValue(_rollingKey, out var count))
                    _hotSequences[_rollingKey] = count + 1;
                else
                    _hotSequences[_rollingKey] = 1;
            }

            internal bool TryCreateRecord(Script script, out VmTraceRecord record)
            {
                record = default;
                if (_instructionCount < WindowSize)
                    return false;

                ulong bestKey = 0;
                int bestCount = 0;
                foreach (var (key, count) in _hotSequences)
                {
                    if (count > bestCount)
                    {
                        bestKey = key;
                        bestCount = count;
                    }
                }

                if (bestCount == 0)
                    return false;

                var sequence = DecodeSequence(bestKey);
                var scriptHash = ComputeScriptHash(script);
                record = new VmTraceRecord(scriptHash, sequence, bestCount, _instructionCount);
                return true;
            }
        }

        private static OpCode[] DecodeSequence(ulong key)
        {
            var sequence = new OpCode[WindowSize];
            for (int i = WindowSize - 1; i >= 0; i--)
            {
                sequence[i] = (OpCode)(key & 0xFF);
                key >>= 8;
            }
            return sequence;
        }

        internal static byte[] ComputeScriptHash(Script script)
        {
            var data = (ReadOnlyMemory<byte>)script;
            Span<byte> sha256 = stackalloc byte[32];
            SHA256.HashData(data.Span, sha256);
            using var ripemd = new Neo.VM.Cryptography.VmRIPEMD160Managed();
            return ripemd.ComputeHash(sha256.ToArray());
        }
    }
}
