// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmarks_DeepCopyRCV2.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark
{
    public class Benchmarks_DeepCopyRCV2 : Benchmarks_DeepCopyRCV1
    {
        public override IReferenceCounter CreateReferenceCounter()
        {
            return new ReferenceCounterV2();
        }
    }
}