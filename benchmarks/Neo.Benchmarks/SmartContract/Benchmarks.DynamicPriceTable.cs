// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks.DynamicPriceTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Benchmark.SmartContract
{
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class Benchmarks_DynamicPriceTable
    {
        private DynamicPriceTable _table = null!;
        private static readonly DynamicPriceTable.PriceFunc Price = _ => 42;

        [GlobalSetup]
        public void Setup()
        {
            _table = new DynamicPriceTable();
            _table[OpCode.PUSH1] = Price;
        }

        [Benchmark]
        public DynamicPriceTable Clone() => _table.Clone();

        [Benchmark]
        public DynamicPriceTable.PriceFunc ReadPrice() => _table[OpCode.PUSH1];

        [Benchmark]
        public void WritePrice() => _table[OpCode.PUSH1] = Price;

        [Benchmark]
        public DynamicPriceTable CloneThenWrite()
        {
            var clone = _table.Clone();
            clone[OpCode.PUSH1] = null;
            return clone;
        }
    }

}
