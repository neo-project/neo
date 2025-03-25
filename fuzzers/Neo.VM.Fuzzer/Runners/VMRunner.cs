// Copyright (C) 2015-2025 The Neo Project.
//
// VMRunner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Fuzzer.Utils;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.VM.Fuzzer.Runners
{
    /// <summary>
    /// Executes Neo VM scripts and tracks execution results
    /// </summary>
    public class VMRunner
    {
        private readonly HashSet<string> _coverage = new();
        private readonly int _timeoutMs;
        private readonly bool _detectDOS;
        private readonly double _dosThreshold;
        private readonly bool _trackMemory;
        private readonly bool _trackOpcodes;
        private readonly DOSDetector? _dosDetector;

        /// <summary>
        /// Creates a new VM runner
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds for script execution</param>
        /// <param name="detectDOS">Whether to detect potential DOS vectors</param>
        /// <param name="dosThreshold">Threshold for flagging potential DOS vectors (0.0-1.0)</param>
        /// <param name="trackMemory">Whether to track memory usage</param>
        /// <param name="trackOpcodes">Whether to track execution time per opcode</param>
        public VMRunner(int timeoutMs = 5000, bool detectDOS = false, double dosThreshold = 0.8, bool trackMemory = false, bool trackOpcodes = true)
        {
            _timeoutMs = timeoutMs;
            _detectDOS = detectDOS;
            _dosThreshold = dosThreshold;
            _trackMemory = trackMemory;
            _trackOpcodes = trackOpcodes;

            if (_detectDOS)
            {
                _dosDetector = new DOSDetector(_dosThreshold, _trackMemory, _trackOpcodes);
            }
        }

        /// <summary>
        /// Executes a script and returns the execution result
        /// </summary>
        /// <param name="script">The script bytes to execute</param>
        /// <returns>An execution result with details about the execution</returns>
        public ExecutionResult Execute(byte[] script)
        {
            var result = new ExecutionResult();
            _coverage.Clear();

            if (_detectDOS)
            {
                _dosDetector?.Reset();
            }

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(_timeoutMs);

            try
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        using var engine = new InstrumentedExecutionEngine();
                        InstrumentEngine(engine, result);

                        engine.LoadScript(script);
                        engine.Execute();

                        result.State = engine.State;
                        result.FinalStack = engine.ResultStack.ToArray();
                        result.Success = engine.State == VMState.HALT;
                    }
                    catch (Exception ex)
                    {
                        result.Exception = ex;
                        result.ExceptionType = ex.GetType().Name;
                        result.ExceptionMessage = ex.Message;
                        result.Crashed = true;
                        result.Success = false;
                    }
                }, cts.Token);

                if (!task.Wait(_timeoutMs))
                {
                    result.TimedOut = true;
                    result.Success = false;
                }
            }
            catch (OperationCanceledException)
            {
                result.TimedOut = true;
                result.Success = false;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.ExceptionType = ex.GetType().Name;
                result.ExceptionMessage = ex.Message;
                result.Crashed = true;
                result.Success = false;
            }

            return result;
        }

        /// <summary>
        /// Instruments the execution engine with event handlers and trackers
        /// </summary>
        private void InstrumentEngine(InstrumentedExecutionEngine engine, ExecutionResult result)
        {
            // Track execution time
            var stopwatch = Stopwatch.StartNew();

            // Set up DOS detection if enabled
            if (_detectDOS && _dosDetector != null)
            {
                engine.OnStepEvent += _dosDetector.OnStep;
                engine.OnFaultEvent += _dosDetector.OnFault;
            }

            // Track coverage
            engine.OnStepEvent += (sender, e) =>
            {
                _coverage.Add($"Opcode:{e.OpCode}");
            };

            // Record execution time when finished
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            };

            // Handle normal completion
            engine.OnExecutionCompleted += (sender, e) =>
            {
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.Coverage = engine.Coverage.ToList();

                // Perform DOS analysis if enabled
                if (_detectDOS)
                {
                    int totalInstructions = engine.InstructionsExecuted;
                    var opcodeExecutionTimes = engine.OpcodeExecutionTimes;
                    result.DOSAnalysis = _dosDetector?.Analyze(
                        totalInstructions,
                        opcodeExecutionTimes,
                        result.ExecutionTimeMs);
                }
            };

            // Handle faults
            engine.OnFaultEvent += (sender, e) =>
            {
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.ExceptionType = e.ExceptionType;
                result.Crashed = true;
                result.Coverage = engine.Coverage.ToList();

                // Perform DOS analysis if enabled (even for crashed scripts)
                if (_detectDOS)
                {
                    int totalInstructions = engine.InstructionsExecuted;
                    var opcodeExecutionTimes = engine.OpcodeExecutionTimes;
                    result.DOSAnalysis = _dosDetector?.Analyze(
                        totalInstructions,
                        opcodeExecutionTimes,
                        result.ExecutionTimeMs);
                }
            };
        }

        /// <summary>
        /// Checks if executing a script found new coverage
        /// </summary>
        public bool FoundNewCoverage(byte[] script, ExecutionResult result)
        {
            if (result.TimedOut || !result.Success) return false;

            var previousCoverage = new HashSet<string>(_coverage);
            Execute(script);
            return _coverage.Except(previousCoverage).Any();
        }

        /// <summary>
        /// Gets the current coverage
        /// </summary>
        public HashSet<string> GetCoverage()
        {
            return new HashSet<string>(_coverage);
        }
    }

    /// <summary>
    /// Result of executing a script
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Gets or sets the final VM state
        /// </summary>
        public VMState State { get; set; }

        /// <summary>
        /// Gets or sets whether the execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets whether the execution timed out
        /// </summary>
        public bool TimedOut { get; set; }

        /// <summary>
        /// Gets or sets whether the execution crashed
        /// </summary>
        public bool Crashed { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds
        /// </summary>
        public double ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the exception that occurred during execution
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets the type of exception that occurred
        /// </summary>
        public string? ExceptionType { get; set; }

        /// <summary>
        /// Gets or sets the exception message
        /// </summary>
        public string? ExceptionMessage { get; set; }

        /// <summary>
        /// Gets or sets the final stack contents
        /// </summary>
        public Neo.VM.Types.StackItem[]? FinalStack { get; set; } = System.Array.Empty<Neo.VM.Types.StackItem>();

        /// <summary>
        /// Gets or sets the coverage information
        /// </summary>
        public List<string> Coverage { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the DOS analysis result
        /// </summary>
        public DOSDetector.DOSAnalysisResult? DOSAnalysis { get; set; }
    }
}
