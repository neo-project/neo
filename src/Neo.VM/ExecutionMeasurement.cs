// Copyright (C) 2015-2024 The Neo Project.
//
// ExecutionMeasurement.cs file belongs to the neo project and is free
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

namespace Neo.VM
{
    public class ExecutionMeasurement
    {

        private readonly List<ExecutionMeasurementEntry> _executionTimeMeasurement = new();

        /// <summary>
        /// Execution measurement description
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Gas left method
        /// </summary>
        public Func<long> GasLeft { get; init; } = () => 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="description">Description</param>
        /// <param name="gasLeft">Gas left</param>
        public ExecutionMeasurement(string description, Func<long> gasLeft)
        {
            Description = description;
            GasLeft = gasLeft;

            string dir = $"./measurement/{Description}";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// Create new ExecutionMeasurementEntry
        /// </summary>
        /// <returns></returns>
        public ExecutionMeasurementEntry NewMeasurement()
        {
            var ret = new ExecutionMeasurementEntry(GasLeft);
            _executionTimeMeasurement.Add(ret);
            return ret;
        }

        /// <summary>
        /// Dump to file
        /// </summary>
        public void Dump()
        {
            using var writer = new StreamWriter($"./measurement/{Description}/{_executionTimeMeasurement.Sum(p => p.TotalExecutionTime)}.txt", append: true);
            _executionTimeMeasurement.ForEach(p => writer.WriteLine(p.ToString()));
        }
    }
}
