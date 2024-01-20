// Copyright (C) 2015-2024 The Neo Project.
//
// ExecutionEngine.Measurement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Diagnostics;
namespace Neo.VM
{
    partial class ExecutionEngine
    {
        private class ExecutionMeasurement
        {
            public OpCode OpCode { get; set; }
            public long Price { get; set; }
            public double PreExecutionTime { get; set; }
            public double ExecutionTime { get; set; }
            public double PostExecutionTime { get; set; }

            private Stopwatch Stopwatch { get; } = new Stopwatch();

            public double TotalExecutionTime => PreExecutionTime + ExecutionTime + PostExecutionTime;

            public override string ToString()
            {
                return $"{OpCode},{Price},{PreExecutionTime},{ExecutionTime},{PostExecutionTime},{TotalExecutionTime},{TotalExecutionTime/Price}";
            }

            public void Start(OpCode opCode)
            {
                OpCode = opCode;
                Price = OpCodePrice.OpCodePrices[opCode];
                Stopwatch.Start();
            }

            public void MeasurePreExecution()
            {
                Stopwatch.Stop();
                var ticks = Stopwatch.ElapsedTicks;
                double ticksPerSecond = Stopwatch.Frequency;
                var elapsedNanoSeconds = ticks / ticksPerSecond * 1_000_000_000;
                PreExecutionTime = elapsedNanoSeconds;
                Stopwatch.Restart();
            }

            public void MeasureExecution()
            {
                Stopwatch.Stop();
                var ticks = Stopwatch.ElapsedTicks;
                double ticksPerSecond = Stopwatch.Frequency;
                var elapsedNanoSeconds = ticks / ticksPerSecond * 1_000_000_000;
                ExecutionTime = elapsedNanoSeconds;
                Stopwatch.Restart();
            }

            public void MeasurePostExecution()
            {
                Stopwatch.Stop();
                var ticks = Stopwatch.ElapsedTicks;
                double ticksPerSecond = Stopwatch.Frequency;
                var elapsedNanoSeconds = ticks / ticksPerSecond * 1_000_000_000;
                PostExecutionTime = elapsedNanoSeconds;
                Stopwatch.Restart();
            }
        }

        private readonly List<ExecutionMeasurement> _executionTimeMeasurement = new();
    }
}
