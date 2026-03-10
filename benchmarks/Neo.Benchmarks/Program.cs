// Copyright (C) 2015-2026 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Running;
using Neo.Benchmarks.NativeContracts;

// List all benchmarks:
//  dotnet run -c Release --framework [for example: net9.0] -- --list flat(or tree)
// Run a specific benchmark:
//  dotnet run -c Release --framework [for example: net9.0] -- -f [benchmark name]
// Run all benchmarks:
//  dotnet run -c Release --framework [for example: net9.0] -- -f *
// Run all benchmarks of a class:
//  dotnet run -c Release --framework [for example: net9.0] -- -f '*Class*'
// More options: https://benchmarkdotnet.org/articles/guides/console-args.html
// dotnet run -c Release --framework net10.0 --project benchmarks/Neo.Benchmarks/Neo.Benchmarks.csproj -- --native-manual-run
var parsed = NativeContractManualRunner.ParseArguments(args);
if (parsed.RunManualSuite)
{
    var options = NativeContractManualRunner.CreateOptions(parsed);
    Environment.ExitCode = NativeContractManualRunner.Run(options, parsed);
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(parsed.ForwardedArgs);
