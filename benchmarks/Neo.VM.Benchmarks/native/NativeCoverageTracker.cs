// Copyright (C) 2015-2025 The Neo Project.
//
// NativeCoverageTracker.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Benchmark.Native
{
    internal static class NativeCoverageTracker
    {
        private static readonly HashSet<string> s_covered = new();

        public static void Register(string id)
        {
            lock (s_covered)
            {
                s_covered.Add(id);
            }
        }

        public static IReadOnlyCollection<string> GetCovered()
        {
            lock (s_covered)
            {
                return s_covered.ToArray();
            }
        }
    }
}
