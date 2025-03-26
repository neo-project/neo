// Copyright (C) 2015-2025 The Neo Project.
//
// DOSDetector.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Fuzzer.Runners;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Neo.VM.Fuzzer.Utils
{
    /// <summary>
    /// Detects potential Denial of Service (DOS) vectors in Neo VM scripts
    /// </summary>
    public class DOSDetector
    {
        private readonly Dictionary<OpCode, List<long>> _opcodeExecutionTimes = new();
        private readonly Dictionary<OpCode, int> _opcodeExecutionCounts = new();
        private readonly List<int> _stackDepthSamples = new();
        private readonly List<StateSnapshot> _stateSnapshots = new();
        private readonly Stopwatch _stopwatch = new();
        private int _totalInstructions = 0;
        private int _maxStackDepth = 0;
        private long _totalExecutionTime = 0;
        private readonly double _dosThreshold;
        private readonly bool _trackMemory;
        private readonly bool _trackOpcodes;

        /// <summary>
        /// Represents a snapshot of the VM state at a point in time
        /// </summary>
        private class StateSnapshot
        {
            public OpCode CurrentOpCode { get; set; }
            public int InstructionPointer { get; set; }
            public int StackDepth { get; set; }
            public long Timestamp { get; set; }

            public override bool Equals(object? obj)
            {
                if (obj is not StateSnapshot other) return false;
                return CurrentOpCode == other.CurrentOpCode &&
                       InstructionPointer == other.InstructionPointer &&
                       StackDepth == other.StackDepth;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(CurrentOpCode, InstructionPointer, StackDepth);
            }
        }

        /// <summary>
        /// Results of DOS detection analysis
        /// </summary>
        public class DOSAnalysisResult
        {
            public bool IsPotentialDOSVector { get; set; }
            public double DOSScore { get; set; }
            public string DetectionReason { get; set; } = string.Empty;
            public Dictionary<string, object> Metrics { get; set; } = new();
            public List<string> Recommendations { get; set; } = new();
        }

        /// <summary>
        /// Creates a new DOS detector
        /// </summary>
        /// <param name="dosThreshold">Threshold for flagging potential DOS vectors (0.0-1.0)</param>
        /// <param name="trackMemory">Whether to track memory usage</param>
        /// <param name="trackOpcodes">Whether to track execution time per opcode</param>
        public DOSDetector(double dosThreshold = 0.8, bool trackMemory = false, bool trackOpcodes = true)
        {
            _dosThreshold = dosThreshold;
            _trackMemory = trackMemory;
            _trackOpcodes = trackOpcodes;
            _stopwatch.Start(); // Start the stopwatch immediately to track execution time
        }

        /// <summary>
        /// Handles the OnStep event from the execution engine
        /// </summary>
        public void OnStep(object sender, StepEventArgs e)
        {
            if (sender is not ExecutionEngine engine) return;

            _totalInstructions++;

            // Track stack depth
            int stackDepth = e.StackSize;
            _stackDepthSamples.Add(stackDepth);

            if (stackDepth > _maxStackDepth)
            {
                _maxStackDepth = stackDepth;
            }

            // Take a snapshot of the current state
            _stateSnapshots.Add(new StateSnapshot
            {
                CurrentOpCode = e.OpCode,
                InstructionPointer = e.InstructionPointer,
                StackDepth = stackDepth,
                Timestamp = _stopwatch.ElapsedTicks
            });
        }

        /// <summary>
        /// Handles exceptions during execution
        /// </summary>
        public void OnFault(object sender, FaultEventArgs e)
        {
            // Record the exception for analysis
            _stopwatch.Stop();

            // Add the exception to state snapshots for analysis
            _stateSnapshots.Add(new StateSnapshot
            {
                CurrentOpCode = OpCode.RET,
                InstructionPointer = e.InstructionPointer,
                StackDepth = 0,
                Timestamp = _stopwatch.ElapsedTicks
            });
        }

        /// <summary>
        /// Resets the detector for a new execution
        /// </summary>
        public void Reset()
        {
            _opcodeExecutionTimes.Clear();
            _opcodeExecutionCounts.Clear();
            _stackDepthSamples.Clear();
            _stateSnapshots.Clear();
            _totalInstructions = 0;
            _maxStackDepth = 0;
            _totalExecutionTime = 0;
            _stopwatch.Reset();
            _stopwatch.Start(); // Restart the stopwatch for the new execution
        }

        /// <summary>
        /// Analyzes the execution metrics to detect potential DOS vectors
        /// </summary>
        /// <param name="instructionCount">Total number of instructions executed</param>
        /// <param name="opcodeExecutionTimes">Dictionary of opcode execution times</param>
        /// <param name="totalExecutionTimeMs">Total execution time in milliseconds</param>
        /// <returns>Analysis result with DOS detection information</returns>
        public DOSAnalysisResult Analyze(
            int instructionCount,
            IReadOnlyDictionary<OpCode, List<long>> opcodeExecutionTimes,
            double totalExecutionTimeMs)
        {
            var result = new DOSAnalysisResult
            {
                IsPotentialDOSVector = false,
                DOSScore = 0.0,
                DetectionReason = string.Empty,
                Metrics = new Dictionary<string, object>
                {
                    ["TotalInstructions"] = _totalInstructions,
                    ["MaxStackDepth"] = _maxStackDepth,
                    ["UniqueOpcodes"] = _opcodeExecutionCounts.Count,
                    ["TotalExecutionTimeMs"] = totalExecutionTimeMs
                },
                Recommendations = new List<string>()
            };

            double score = 0.0;
            List<string> detectionReasons = new();

            // Check for high instruction count - LOWERED THRESHOLD FROM 5000 to 100
            if (_totalInstructions > 100)
            {
                double instructionScore = Math.Min(0.5, _totalInstructions / 1000.0);
                score += instructionScore;
                detectionReasons.Add($"High instruction count: {_totalInstructions}");
                result.Metrics["InstructionScore"] = instructionScore;
                result.Recommendations.Add("Consider adding instruction count limits to prevent excessive execution");
            }

            // Check for potential infinite loops
            var loopScore = DetectPotentialInfiniteLoops(result, detectionReasons);
            score += loopScore;
            result.Metrics["LoopScore"] = loopScore;

            // Check for excessive stack usage - LOWERED THRESHOLD FROM 50 to 5
            if (_maxStackDepth > 5)
            {
                double stackScore = Math.Min(0.3, _maxStackDepth / 50.0);
                score += stackScore;
                detectionReasons.Add($"Excessive stack depth: {_maxStackDepth}");
                result.Metrics["StackScore"] = stackScore;
                result.Recommendations.Add("Consider adding stack depth limits to prevent stack overflow attacks");
            }

            // Check for slow opcodes - LOWERED THRESHOLD FROM 0.2ms to 0.05ms and from 5 executions to 2
            if (opcodeExecutionTimes != null && opcodeExecutionTimes.Count > 0)
            {
                var slowOpcodes = new Dictionary<OpCode, double>();
                double totalOpTime = 0;

                foreach (var kvp in opcodeExecutionTimes)
                {
                    if (kvp.Value.Count == 0) continue;

                    double avgTime = kvp.Value.Average();
                    totalOpTime += avgTime * _opcodeExecutionCounts.GetValueOrDefault(kvp.Key, 0);

                    // Identify particularly slow opcodes
                    if (avgTime > 0.05 && _opcodeExecutionCounts.GetValueOrDefault(kvp.Key, 0) > 2)
                    {
                        slowOpcodes[kvp.Key] = avgTime;
                    }
                }

                if (slowOpcodes.Count > 0)
                {
                    double opcodeScore = Math.Min(0.4, slowOpcodes.Count / 5.0);
                    score += opcodeScore;

                    var topSlowOpcodes = slowOpcodes
                        .OrderByDescending(kvp => kvp.Value * _opcodeExecutionCounts.GetValueOrDefault(kvp.Key, 0))
                        .Take(3)
                        .ToList();

                    detectionReasons.Add($"Slow opcodes: {string.Join(", ", topSlowOpcodes.Select(kvp => kvp.Key))}");
                    result.Metrics["SlowOpcodes"] = topSlowOpcodes.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
                    result.Metrics["OpcodeScore"] = opcodeScore;

                    result.Recommendations.Add("Optimize usage of slow opcodes or consider adding execution time limits");
                }
            }

            // Check for long execution time - LOWERED THRESHOLD FROM 500ms to 10ms
            if (totalExecutionTimeMs > 10)
            {
                double timeScore = Math.Min(0.5, totalExecutionTimeMs / 100.0);
                score += timeScore;
                detectionReasons.Add($"Long execution time: {totalExecutionTimeMs:F2}ms");
                result.Metrics["TimeScore"] = timeScore;
                result.Recommendations.Add("Consider adding execution time limits to prevent long-running scripts");
            }

            // Normalize score to 0.0-1.0 range
            result.DOSScore = Math.Min(1.0, score);

            // Determine if this is a potential DOS vector
            result.IsPotentialDOSVector = result.DOSScore >= _dosThreshold;

            // Set detection reason
            if (detectionReasons.Count > 0)
            {
                result.DetectionReason = string.Join("; ", detectionReasons);
            }

            return result;
        }

        private double DetectPotentialInfiniteLoops(DOSAnalysisResult result, List<string> detectionReasons)
        {
            // Look for repeated state patterns - LOWERED THRESHOLD FROM 10 to 5 repetitions and from 30% to 20% ratio
            var repeatedStates = _stateSnapshots
                .GroupBy(s => new { s.CurrentOpCode, s.InstructionPointer, s.StackDepth })
                .Where(g => g.Count() > 5) // More than 5 identical states
                .OrderByDescending(g => g.Count())
                .ToList();

            if (repeatedStates.Any())
            {
                var topRepeatedState = repeatedStates.First();
                double repetitionRatio = (double)topRepeatedState.Count() / _totalInstructions;

                if (repetitionRatio > 0.2) // More than 20% of instructions are the same state
                {
                    detectionReasons.Add($"Potential infinite loop at IP: {topRepeatedState.Key.InstructionPointer}");

                    // Add loop information to metrics
                    result.Metrics["PotentialLoops"] = repeatedStates
                        .Take(3)
                        .Select(g => new
                        {
                            InstructionPointer = g.Key.InstructionPointer,
                            OpCode = g.Key.CurrentOpCode,
                            RepetitionCount = g.Count(),
                            RepetitionRatio = (double)g.Count() / _totalInstructions
                        })
                        .ToList();

                    result.Recommendations.Add("Check for potential infinite loops and add proper termination conditions");

                    return Math.Min(0.6, repetitionRatio);
                }
            }

            return 0.0;
        }
    }
}
