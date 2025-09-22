// Copyright (C) 2015-2025 The Neo Project.
//
// InteropCoverageReport.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.VM.Benchmark.Infrastructure
{
    internal static class InteropCoverageReport
    {
        public static IReadOnlyCollection<string> MissingSyscalls(IReadOnlyCollection<string> covered)
        {
            var all = ApplicationEngine.Services.Values
                .Where(d => d.Hardfork is null)
                .Select(d => d.Name)
                .ToHashSet(StringComparer.Ordinal);
            all.ExceptWith(covered);
            return all.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        }

        public static IReadOnlyCollection<string> MissingNativeMethods(IReadOnlyCollection<string> covered)
        {
            var all = NativeContract.Contracts
                .SelectMany(contract => contract.GetContractState(ProtocolSettings.Default, 0).Manifest.Abi.Methods
                    .Select(m => $"{contract.Name}:{m.Name}"))
                .ToHashSet(StringComparer.Ordinal);
            all.ExceptWith(covered);
            return all.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        }

        public static void WriteReport(string path, IReadOnlyCollection<string> missingSyscalls, IReadOnlyCollection<string> missingNatives)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var writer = new StreamWriter(path, append: false);
            writer.WriteLine("Category,Identifier");
            foreach (var syscall in missingSyscalls)
                writer.WriteLine($"syscall,{syscall}");
            foreach (var native in missingNatives)
                writer.WriteLine($"native,{native}");
        }
    }
}
