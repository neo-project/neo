// Copyright (C) 2015-2025 The Neo Project.
//
// ScenarioProfiles.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Describes the set of standard workload tiers used by benchmarks.
    /// </summary>
    public enum ScenarioComplexity
    {
        Micro,
        Standard,
        Stress
    }

    /// <summary>
    /// Structural data for a workload profile.
    /// </summary>
    public readonly record struct ScenarioProfile(int Iterations, int DataLength, int CollectionLength)
    {
        public static ScenarioProfile For(ScenarioComplexity complexity) => complexity switch
        {
            ScenarioComplexity.Micro => new ScenarioProfile(Iterations: 8, DataLength: 32, CollectionLength: 8),
            ScenarioComplexity.Standard => new ScenarioProfile(Iterations: 64, DataLength: 512, CollectionLength: 64),
            ScenarioComplexity.Stress => new ScenarioProfile(Iterations: 512, DataLength: 16 * 1024, CollectionLength: 256),
            _ => throw new ArgumentOutOfRangeException(nameof(complexity), complexity, null)
        };

        public bool IsEmpty => Iterations == 0 && DataLength == 0 && CollectionLength == 0;

        public ScenarioProfile With(int? iterations = null, int? dataLength = null, int? collectionLength = null)
        {
            return new ScenarioProfile(
                iterations ?? Iterations,
                dataLength ?? DataLength,
                collectionLength ?? CollectionLength);
        }

        public ScenarioProfile Scale(double iterationFactor = 1d, double dataFactor = 1d, double collectionFactor = 1d)
        {
            static int ScaleValue(int value, double factor) => factor switch
            {
                1d => value,
                _ => (int)Math.Max(0, Math.Round(value * factor, MidpointRounding.AwayFromZero))
            };

            return new ScenarioProfile(
                ScaleValue(Iterations, iterationFactor),
                ScaleValue(DataLength, dataFactor),
                ScaleValue(CollectionLength, collectionFactor));
        }

        public ScenarioProfile EnsureMinimum(int minIterations = 0, int minDataLength = 0, int minCollectionLength = 0)
        {
            return new ScenarioProfile(
                Math.Max(Iterations, minIterations),
                Math.Max(DataLength, minDataLength),
                Math.Max(CollectionLength, minCollectionLength));
        }
    }
}
