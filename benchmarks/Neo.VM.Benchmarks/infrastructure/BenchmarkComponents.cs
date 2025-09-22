// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkComponents.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Identifies the high level component a scenario exercises.
    /// </summary>
    public enum BenchmarkComponent
    {
        Opcode,
        Syscall,
        NativeContract
    }

    /// <summary>
    /// Identifies the BenchmarkDotNet variant a scenario may execute.
    /// </summary>
    public enum BenchmarkVariant
    {
        Baseline,
        Single,
        Saturated
    }

    /// <summary>
    /// Identifies the kind of operation being measured.
    /// </summary>
    public enum BenchmarkOperationKind
    {
        Instruction,
        Syscall,
        NativeMethod
    }
}
