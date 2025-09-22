// Copyright (C) 2015-2025 The Neo Project.
//
// NativeCoverageReport.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Native;
using Neo.VM.Benchmark.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Benchmark.Native
{
    internal static class NativeCoverageReport
    {
        public static IReadOnlyCollection<string> GetMissing()
        {
            var covered = NativeCoverageTracker.GetCovered();
            return InteropCoverageReport.MissingNativeMethods(covered);
        }
    }
}
