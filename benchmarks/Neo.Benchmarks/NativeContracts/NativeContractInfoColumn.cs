// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractInfoColumn.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Custom column for exposing native contract metadata in BenchmarkDotNet tables.
    /// </summary>
    public sealed class NativeContractInfoColumn : IColumn
    {
        private readonly string _id;
        private readonly string _columnName;
        private readonly string _legend;
        private readonly Func<NativeContractBenchmarkCase, string> _selector;

        public NativeContractInfoColumn(string id, string columnName, string legend, Func<NativeContractBenchmarkCase, string> selector)
        {
            _id = id ?? throw new ArgumentNullException(nameof(id));
            _columnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            _legend = legend ?? columnName;
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        public string Id => _id;

        public string ColumnName => _columnName;

        public string Legend => _legend;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(benchmarkCase);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(benchmarkCase);

        private string GetValue(BenchmarkCase benchmarkCase)
        {
            if (!TryGetCase(benchmarkCase, out var nativeCase))
                return "n/a";
            return _selector(nativeCase);
        }

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public bool IsAvailable(Summary summary) => true;

        public UnitType UnitType => UnitType.Dimensionless;

        public ColumnCategory Category => ColumnCategory.Job;

        public int PriorityInCategory => 0;

        public bool AlwaysShow => true;

        public bool IsNumeric => false;

        private static bool TryGetCase(BenchmarkCase benchmarkCase, out NativeContractBenchmarkCase nativeCase)
        {
            if (benchmarkCase.Parameters["Case"] is NativeContractBenchmarkCase c)
            {
                nativeCase = c;
                return true;
            }

            nativeCase = null;
            return false;
        }
    }
}
