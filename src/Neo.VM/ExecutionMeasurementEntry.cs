// Copyright (C) 2015-2024 The Neo Project.
//
// ExecutionMeasurementEntry.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;

namespace Neo.VM
{
    public class ExecutionMeasurementEntry
    {
        private long _startingGas = 0;
        private readonly Stopwatch _watch = new();

        public OpCode OpCode { get; private set; }
        public long Price { get; private set; }
        public double PreExecutionTime { get; private set; }
        public double ExecutionTime { get; private set; }
        public double PostExecutionTime { get; private set; }
        public Func<long> GasLeft { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gasLeft">Gas left</param>
        public ExecutionMeasurementEntry(Func<long> gasLeft)
        {
            GasLeft = gasLeft;
        }

        public double TotalExecutionTime => PreExecutionTime + ExecutionTime + PostExecutionTime;

        public void Start(Instruction instruction)
        {
            OpCode = instruction.OpCode;
            _startingGas = GasLeft();
            _watch.Start();
        }

        public void MeasurePreExecution()
        {
            _watch.Stop();
            var ticks = _watch.ElapsedTicks;
            double ticksPerSecond = Stopwatch.Frequency;
            var elapsedNanoSeconds = ticks / ticksPerSecond * 1_000_000_000;
            PreExecutionTime = elapsedNanoSeconds;
            _watch.Restart();
        }

        public void MeasureExecution()
        {
            _watch.Stop();
            var ticks = _watch.ElapsedTicks;
            double ticksPerSecond = Stopwatch.Frequency;
            var elapsedNanoSeconds = ticks / ticksPerSecond * 1_000_000_000;
            ExecutionTime = elapsedNanoSeconds;
            _watch.Restart();
        }

        public void MeasurePostExecution()
        {
            _watch.Stop();
            var ticks = _watch.ElapsedTicks;
            double ticksPerSecond = Stopwatch.Frequency;
            var elapsedNanoSeconds = ticks / ticksPerSecond * 1_000_000_000;
            PostExecutionTime = elapsedNanoSeconds;
            Price = _startingGas - GasLeft();

            _watch.Restart();
        }

        public override string ToString()
        {
            return $"{OpCode},{Price},{PreExecutionTime},{ExecutionTime},{PostExecutionTime},{TotalExecutionTime},{TotalExecutionTime / Price}";
        }
    }
}
