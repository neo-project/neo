// Copyright (C) 2015-2025 The Neo Project.
//
// ExecutionEngineEventSource.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Neo.VM
{
    /// <summary>
    /// Provides EventSource-backed counters for VM dispatch, stack depth and reference sweeps.
    /// </summary>
    [EventSource(Name = "Neo.VM.Execution")]
    internal sealed class ExecutionEngineEventSource : EventSource
    {
        public static readonly ExecutionEngineEventSource Log = new();

        private readonly IncrementingEventCounter? _instructionRate;
        private readonly EventCounter? _instructionDuration;
        private readonly IncrementingEventCounter? _referenceSweepRate;

        private ExecutionEngineEventSource()
        {
            _instructionRate = new IncrementingEventCounter("vm-instruction-rate", this)
            {
                DisplayName = "VM instructions/sec",
                DisplayUnits = "ops/s"
            };

            _instructionDuration = new EventCounter("vm-instruction-duration", this)
            {
                DisplayName = "VM dispatch latency",
                DisplayUnits = "ms"
            };

            _referenceSweepRate = new IncrementingEventCounter("vm-ref-sweep-rate", this)
            {
                DisplayName = "Reference sweeps/sec",
                DisplayUnits = "ops/s"
            };
        }

        internal TelemetryRegistration? Register(ExecutionEngine engine)
        {
            if (!IsEnabled()) return null;
            return new TelemetryRegistration(this, engine);
        }

        internal void InstructionDispatched(double elapsedMilliseconds)
        {
            _instructionRate?.Increment();
            _instructionDuration?.WriteMetric((float)elapsedMilliseconds);
        }

        internal void ReferenceSweepCompleted()
        {
            _referenceSweepRate?.Increment();
        }

        internal sealed class TelemetryRegistration : IDisposable
        {
            private readonly ExecutionEngineEventSource _eventSource;
            private readonly ExecutionEngine _engine;
            private readonly PollingCounter? _evaluationStackDepth;
            private readonly PollingCounter? _invocationStackDepth;
            private readonly PollingCounter? _resultStackDepth;

            private long _lastEvaluationDepth;
            private long _lastInvocationDepth;
            private long _lastResultDepth;

            internal TelemetryRegistration(ExecutionEngineEventSource eventSource, ExecutionEngine engine)
            {
                _eventSource = eventSource;
                _engine = engine;
                _evaluationStackDepth = new PollingCounter("vm-evaluation-stack-depth", _eventSource, () => Interlocked.Read(ref _lastEvaluationDepth))
                {
                    DisplayName = "Evaluation stack depth",
                    DisplayUnits = "items"
                };
                _invocationStackDepth = new PollingCounter("vm-invocation-stack-depth", _eventSource, () => Interlocked.Read(ref _lastInvocationDepth))
                {
                    DisplayName = "Invocation stack depth",
                    DisplayUnits = "frames"
                };
                _resultStackDepth = new PollingCounter("vm-result-stack-depth", _eventSource, () => Interlocked.Read(ref _lastResultDepth))
                {
                    DisplayName = "Result stack depth",
                    DisplayUnits = "items"
                };
            }

            internal void BeforeInstruction()
            {
                // No-op currently, hook reserved for future use.
            }

            internal void AfterInstruction()
            {
                var evalDepth = _engine.CurrentContext?.EvaluationStack.Count ?? 0;
                var invocationDepth = _engine.InvocationStack.Count;
                var resultDepth = _engine.ResultStack.Count;

                Interlocked.Exchange(ref _lastEvaluationDepth, evalDepth);
                Interlocked.Exchange(ref _lastInvocationDepth, invocationDepth);
                Interlocked.Exchange(ref _lastResultDepth, resultDepth);
            }

            public void Dispose()
            {
                _evaluationStackDepth?.Dispose();
                _invocationStackDepth?.Dispose();
                _resultStackDepth?.Dispose();
            }
        }
    }
}
