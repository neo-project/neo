// Copyright (C) 2015-2025 The Neo Project.
//
// OpcodeCoverageReport.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.IO;
using System.Text;

namespace Neo.VM.Benchmark.OpCode
{
    internal static class OpcodeCoverageReport
    {
        public static IReadOnlyCollection<VM.OpCode> GetUncoveredOpcodes()
        {
            var covered = OpcodeScenarioFactory.GetSupportedOpcodes();
            return Enum.GetValues<VM.OpCode>()
                .Where(op => op != VM.OpCode.ABORT && !covered.Contains(op))
                .ToArray();
        }

        public static void WriteCoverageTable(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var builder = new StringBuilder();
            builder.AppendLine("Opcode,Covered");
            var covered = OpcodeScenarioFactory.GetSupportedOpcodes();
            foreach (var opcode in Enum.GetValues<VM.OpCode>().OrderBy(op => op))
            {
                builder.Append(opcode);
                builder.Append(',');
                builder.Append(covered.Contains(opcode) ? "yes" : "no");
                builder.AppendLine();
            }
            File.WriteAllText(path, builder.ToString());
        }

        public static void WriteMissingList(string path, IReadOnlyCollection<VM.OpCode> missing)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var writer = new StreamWriter(path, append: false);
            writer.WriteLine("Opcode");
            foreach (var opcode in missing.OrderBy(static op => op))
            {
                writer.WriteLine(opcode);
            }
        }
    }
}
