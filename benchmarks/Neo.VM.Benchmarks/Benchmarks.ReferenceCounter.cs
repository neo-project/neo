// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmarks.POC.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Test;
using Neo.Test.Extensions;
using Neo.Test.Types;
using System.Text;

namespace Neo.VM.Benchmark
{
    public class Benchmarks_ReferenceCounter : VMJsonTestBase
    {
        [Benchmark]
        public void V2()
        {
            Run<ReferenceCounterV2>();
        }

        [Benchmark]
        public void V1()
        {
            Run<ReferenceCounter>();
        }

        private void Run<T>() where T : IReferenceCounter, new()
        {
            var path = Path.GetFullPath("../../../../../../../../../tests/Neo.VM.Tests/Tests");

            foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
            {
                var realFile = Path.GetFullPath(file);
                var json = File.ReadAllText(realFile, Encoding.UTF8);
                var ut = json.DeserializeJson<VMUT>();

                try
                {
                    ExecuteTest<T>(ut);
                }
                catch (Exception ex)
                {
                    throw new AggregateException("Error in file: " + realFile, ex);
                }
            }
        }
    }
}
