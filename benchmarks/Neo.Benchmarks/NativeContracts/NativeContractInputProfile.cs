// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractInputProfile.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Identifies the canonical size buckets that native contract benchmarks can target.
    /// </summary>
    public enum NativeContractInputSize
    {
        Tiny,
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// Describes a representative workload bucket for a native contract benchmark.
    /// </summary>
    /// <param name="Size">Logical size bucket identifier.</param>
    /// <param name="Name">Friendly name used when rendering benchmark cases.</param>
    /// <param name="ByteLength">Preferred byte payload size for byte/string based parameters.</param>
    /// <param name="ElementCount">Preferred collection length for array/map based parameters.</param>
    /// <param name="IntegerMagnitude">Representative magnitude for integer based parameters.</param>
    /// <param name="Description">Human readable description of the workload bucket.</param>
    public sealed record NativeContractInputProfile(
        NativeContractInputSize Size,
        string Name,
        int ByteLength,
        int ElementCount,
        BigInteger IntegerMagnitude,
        string Description)
    {
        public override string ToString() => $"{Name} ({Size})";
    }

    /// <summary>
    /// Provides the canonical set of input profiles consumed by the benchmark suite.
    /// </summary>
    public static class NativeContractInputProfiles
    {
        private static readonly ReadOnlyCollection<NativeContractInputProfile> s_defaultProfiles =
        [
            new NativeContractInputProfile(
                NativeContractInputSize.Tiny,
                "Tiny",
                ByteLength: 32,
                ElementCount: 1,
                IntegerMagnitude: new BigInteger(1_000),
                Description: "Fits in a single stack item / small hash sized input."),
            new NativeContractInputProfile(
                NativeContractInputSize.Small,
                "Small",
                ByteLength: 256,
                ElementCount: 4,
                IntegerMagnitude: BigInteger.Parse("1000000000000000000000000"),
                Description: "Representative for typical application inputs."),
            new NativeContractInputProfile(
                NativeContractInputSize.Medium,
                "Medium",
                ByteLength: 2048,
                ElementCount: 16,
                IntegerMagnitude: BigInteger.One << 256,
                Description: "Stresses allocation-heavy paths and tree traversals."),
            new NativeContractInputProfile(
                NativeContractInputSize.Large,
                "Large",
                ByteLength: 4096,
                ElementCount: 64,
                IntegerMagnitude: BigInteger.One << 512,
                Description: "Upper bound for native contract payload sizes.")
        ];

        /// <summary>
        /// Gets the shared read-only collection of default input profiles.
        /// </summary>
        public static IReadOnlyCollection<NativeContractInputProfile> Default => s_defaultProfiles;
    }
}
