// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmarks_DeepCopy.cs file belongs to the neo project and is free
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

public class Benchmarks_DeepCopy
{
    public IEnumerable<(int Depth, int ElementsPerLevel)> ParamSource()
    {
        int[] depths = [2, 4];
        int[] elementsPerLevel = [2, 4, 6];

        foreach (var depth in depths)
        {
            foreach (var elements in elementsPerLevel)
            {
                if (depth <= 8 || elements <= 2)
                {
                    yield return (depth, elements);
                }
            }
        }
    }

    [ParamsSource(nameof(ParamSource))]
    public (int Depth, int ElementsPerLevel) Params;

    [Benchmark]
    public void BenchNestedArrayDeepCopy()
    {
        var root = new Array(new ReferenceCounter());
        CreateNestedArray(root, Params.Depth, Params.ElementsPerLevel);
        _ = root.DeepCopy();
    }

    [Benchmark]
    public void BenchNestedArrayDeepCopyWithReferenceCounter()
    {
        var referenceCounter = new ReferenceCounter();
        var root = new Array(referenceCounter);
        CreateNestedArray(root, Params.Depth, Params.ElementsPerLevel, referenceCounter);
        _ = root.DeepCopy();
    }

    [Benchmark]
    public void BenchNestedTestArrayDeepCopy()
    {
        var root = new TestArray(new ReferenceCounter());
        CreateNestedTestArray(root, Params.Depth, Params.ElementsPerLevel);
        _ = root.DeepCopy();
    }

    [Benchmark]
    public void BenchNestedTestArrayDeepCopyWithReferenceCounter()
    {
        var referenceCounter = new ReferenceCounter();
        var root = new TestArray(referenceCounter);
        CreateNestedTestArray(root, Params.Depth, Params.ElementsPerLevel, referenceCounter);
        _ = root.DeepCopy();
    }

    private static void CreateNestedArray(Array? rootArray, int depth, int elementsPerLevel = 1, IReferenceCounter? referenceCounter = null)
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

    private static void CreateNestedTestArray(TestArray rootArray, int depth, int elementsPerLevel = 1, IReferenceCounter referenceCounter = null)
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


// | Method                                           | Params | Mean         | Error       | StdDev      |
// |------------------------------------------------- |------- |-------------:|------------:|------------:|
// | BenchNestedArrayDeepCopy                         | (2, 2) |           NA |          NA |          NA |
// | BenchNestedArrayDeepCopyWithReferenceCounter     | (2, 2) |   1,149.8 ns |    21.83 ns |    24.26 ns |
// | BenchNestedTestArrayDeepCopy                     | (2, 2) |     632.0 ns |    11.56 ns |    12.37 ns |
// | BenchNestedTestArrayDeepCopyWithReferenceCounter | (2, 2) |   1,142.2 ns |    14.77 ns |    13.09 ns |
// | BenchNestedArrayDeepCopy                         | (2, 4) |           NA |          NA |          NA |
// | BenchNestedArrayDeepCopyWithReferenceCounter     | (2, 4) |   3,668.6 ns |    45.15 ns |    42.23 ns |
// | BenchNestedTestArrayDeepCopy                     | (2, 4) |   1,596.6 ns |    23.94 ns |    22.39 ns |
// | BenchNestedTestArrayDeepCopyWithReferenceCounter | (2, 4) |   3,681.1 ns |    60.30 ns |    53.46 ns |
// | BenchNestedArrayDeepCopy                         | (2, 6) |           NA |          NA |          NA |
// | BenchNestedArrayDeepCopyWithReferenceCounter     | (2, 6) |   7,373.6 ns |    85.36 ns |    79.85 ns |
// | BenchNestedTestArrayDeepCopy                     | (2, 6) |   3,144.5 ns |    36.97 ns |    34.58 ns |
// | BenchNestedTestArrayDeepCopyWithReferenceCounter | (2, 6) |   7,369.9 ns |   129.31 ns |   120.95 ns |
// | BenchNestedArrayDeepCopy                         | (4, 2) |           NA |          NA |          NA |
// | BenchNestedArrayDeepCopyWithReferenceCounter     | (4, 2) |   5,411.1 ns |    51.05 ns |    47.75 ns |
// | BenchNestedTestArrayDeepCopy                     | (4, 2) |   1,979.6 ns |    27.34 ns |    25.57 ns |
// | BenchNestedTestArrayDeepCopyWithReferenceCounter | (4, 2) |   5,247.7 ns |    47.92 ns |    42.48 ns |
// | BenchNestedArrayDeepCopy                         | (4, 4) |           NA |          NA |          NA |
// | BenchNestedArrayDeepCopyWithReferenceCounter     | (4, 4) |  58,111.1 ns | 1,105.46 ns | 1,085.71 ns |
// | BenchNestedTestArrayDeepCopy                     | (4, 4) |  18,572.0 ns |   205.89 ns |   192.59 ns |
// | BenchNestedTestArrayDeepCopyWithReferenceCounter | (4, 4) |  58,180.0 ns |   329.65 ns |   275.28 ns |
// | BenchNestedArrayDeepCopy                         | (4, 6) |           NA |          NA |          NA |
// | BenchNestedArrayDeepCopyWithReferenceCounter     | (4, 6) | 304,681.5 ns | 3,336.16 ns | 2,957.42 ns |
// | BenchNestedTestArrayDeepCopy                     | (4, 6) |  95,279.8 ns |   888.40 ns |   831.01 ns |
// | BenchNestedTestArrayDeepCopyWithReferenceCounter | (4, 6) | 310,658.1 ns | 6,192.65 ns | 5,792.61 ns |
