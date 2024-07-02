// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmarks.Types.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Array = Neo.VM.Types.Array;

namespace Neo.VM.Benchmark;

public class Benchmarks_Types
{
    [Params(1, 2, 4, 8, 16, 32, 64, 128)]
    public int Depth { get; set; }

    [Params(1, 2, 4, 8)]
    public int ElementsPerLevel { get; set; }

    [Benchmark]
    public void BenchNestedArrayDeepCopy()
    {
        var root = new Array(new ReferenceCounter());
        CreateNestedArray(root, Depth, ElementsPerLevel);
        _ = root.DeepCopy();
    }

    [Benchmark]
    public void BenchNestedArrayDeepCopyWithReferenceCounter()
    {
        var referenceCounter = new ReferenceCounter();
        var root = new Array(referenceCounter);
        CreateNestedArray(root, Depth, ElementsPerLevel, referenceCounter);
        _ = root.DeepCopy();
    }

    [Benchmark]
    public void BenchNestedTestArrayDeepCopy()
    {
        var root = new TestArray(new ReferenceCounter());
        CreateNestedTestArray(root, Depth, ElementsPerLevel);
        _ = root.DeepCopy();
    }

    [Benchmark]
    public void BenchNestedTestArrayDeepCopyWithReferenceCounter()
    {
        var referenceCounter = new ReferenceCounter();
        var root = new TestArray(referenceCounter);
        CreateNestedTestArray(root, Depth, ElementsPerLevel, referenceCounter);
        _ = root.DeepCopy();
    }

    private static void CreateNestedArray(Array rootArray, int depth, int elementsPerLevel = 1, ReferenceCounter referenceCounter = null)
    {
        if (depth < 0)
        {
            throw new ArgumentException("Depth must be non-negative", nameof(depth));
        }

        if (rootArray == null)
        {
            throw new ArgumentNullException(nameof(rootArray));
        }

        if (depth == 0)
        {
            return;
        }

        for (var i = 0; i < elementsPerLevel; i++)
        {
            var childArray = new Array(referenceCounter);
            rootArray.Add(childArray);
            CreateNestedArray(childArray, depth - 1, elementsPerLevel, referenceCounter);
        }
    }

    private static void CreateNestedTestArray(TestArray rootArray, int depth, int elementsPerLevel = 1, ReferenceCounter referenceCounter = null)
    {
        if (depth < 0)
        {
            throw new ArgumentException("Depth must be non-negative", nameof(depth));
        }

        if (rootArray == null)
        {
            throw new ArgumentNullException(nameof(rootArray));
        }

        if (depth == 0)
        {
            return;
        }

        for (var i = 0; i < elementsPerLevel; i++)
        {
            var childArray = new TestArray(referenceCounter);
            rootArray.Add(childArray);
            CreateNestedTestArray(childArray, depth - 1, elementsPerLevel, referenceCounter);
        }
    }
}
