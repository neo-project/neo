// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmarks_Convert.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Benchmark.OpCode;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.VM.Benchmark
{
    public class Benchmarks_Convert
    {
        private Dictionary<StackItemType, List<StackItem>> testItemsByType;

        [IterationSetup]
        public void Setup()
        {
            testItemsByType = CreateTestItemsByType();
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetTypeConversionPairs))]
        public void BenchConvertTo(StackItemType fromType, StackItemType toType)
        {
            foreach (var item in testItemsByType[fromType])
            {
                try
                {
                    _ = item.ConvertTo(toType);
                }
                catch (Exception)
                {
                    // Ignore invalid casts as they're expected for some conversions
                }
            }
        }

        public IEnumerable<object[]> GetTypeConversionPairs()
        {
            var types = (StackItemType[])Enum.GetValues(typeof(StackItemType));
            foreach (var fromType in types)
            {
                foreach (var toType in types)
                {
                    yield return [fromType, toType];
                }
            }
        }

        private Dictionary<StackItemType, List<StackItem>> CreateTestItemsByType()
        {
            var referenceCounter = new ReferenceCounter();
            var result = new Dictionary<StackItemType, List<StackItem>>();

            foreach (StackItemType type in Enum.GetValues(typeof(StackItemType)))
            {
                result[type] = new List<StackItem>();
            }

            result[StackItemType.Boolean].Add(StackItem.True);
            result[StackItemType.Boolean].Add(StackItem.False);

            result[StackItemType.Integer].Add(Benchmark_Opcode.MAX_INT);
            result[StackItemType.Integer].Add(Benchmark_Opcode.MIN_INT);

            result[StackItemType.ByteString].Add(new ByteString(new byte[ushort.MaxValue * 2]));
            result[StackItemType.ByteString].Add(new ByteString(new byte[ushort.MaxValue * 2]));

            // Create a 128-byte buffer
            var longBuffer = new byte[ushort.MaxValue * 2];
            for (int i = 0; i < 128; i++) longBuffer[i] = (byte)(i % 256);
            result[StackItemType.Buffer].Add(new Buffer(longBuffer));
            result[StackItemType.Buffer].Add(new Buffer(new byte[ushort.MaxValue * 2])); // Another 128-byte buffer, all zeros

            // Create an array with 10 items
            var longArray = new Array(referenceCounter);
            for (int i = 0; i < 10; i++) longArray.Add(new Integer(i));
            result[StackItemType.Array].Add(longArray);
            result[StackItemType.Array].Add(new Array(referenceCounter) { StackItem.True, new ByteString(new byte[] { 3, 4, 5 }) });

            // Create a struct with 10 items
            var longStruct = new Struct(referenceCounter);
            for (int i = 0; i < 10; i++) longStruct.Add(new Integer(i * 10));
            result[StackItemType.Struct].Add(longStruct);
            result[StackItemType.Struct].Add(new Struct(referenceCounter) { StackItem.False, new Buffer(new byte[] { 6, 7, 8 }) });

            // Create a map with 10 items
            var longMap = new Map(referenceCounter);
            for (int i = 0; i < 10; i++) longMap[new Integer(i)] = new ByteString(new byte[] { (byte)(i * 20) });
            result[StackItemType.Map].Add(longMap);
            result[StackItemType.Map].Add(new Map(referenceCounter) { [new ByteString(new byte[] { 9 })] = StackItem.True });

            result[StackItemType.InteropInterface].Add(new InteropInterface(new object()));
            result[StackItemType.InteropInterface].Add(new InteropInterface("test string"));

            return result;
        }
    }
}


// | Method         | fromType         | toType           | Mean        | Error       | StdDev      | Median      |
// |--------------- |----------------- |----------------- |------------:|------------:|------------:|------------:|
// | BenchConvertTo | Any              | Any              |    303.5 ns |    19.89 ns |    54.12 ns |    300.0 ns |
// | BenchConvertTo | Any              | Pointer          |    292.8 ns |    24.67 ns |    65.86 ns |    300.0 ns |
// | BenchConvertTo | Any              | Boolean          |    313.5 ns |    26.20 ns |    72.60 ns |    300.0 ns |
// | BenchConvertTo | Any              | Integer          |    310.2 ns |    19.88 ns |    54.75 ns |    300.0 ns |
// | BenchConvertTo | Any              | ByteString       |    295.1 ns |    18.65 ns |    49.45 ns |    300.0 ns |
// | BenchConvertTo | Any              | Buffer           |    279.7 ns |    15.56 ns |    40.45 ns |    300.0 ns |
// | BenchConvertTo | Any              | Array            |    278.8 ns |    15.20 ns |    41.10 ns |    300.0 ns |
// | BenchConvertTo | Any              | Struct           |    317.2 ns |    23.77 ns |    65.07 ns |    300.0 ns |
// | BenchConvertTo | Any              | Map              |    276.5 ns |    16.18 ns |    42.64 ns |    300.0 ns |
// | BenchConvertTo | Any              | InteropInterface |    281.3 ns |    15.52 ns |    39.23 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | Any              |    300.0 ns |     0.00 ns |     0.00 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | Pointer          |    284.1 ns |    13.86 ns |    36.75 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | Boolean          |    329.8 ns |    25.04 ns |    67.27 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | Integer          |    306.8 ns |    23.22 ns |    63.96 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | ByteString       |    318.7 ns |    19.83 ns |    55.60 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | Buffer           |    314.1 ns |    22.94 ns |    62.02 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | Array            |    275.6 ns |    21.82 ns |    57.87 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | Struct           |    312.9 ns |    18.75 ns |    50.68 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | Map              |    310.1 ns |    24.96 ns |    69.16 ns |    300.0 ns |
// | BenchConvertTo | Pointer          | InteropInterface |    279.2 ns |    15.93 ns |    40.84 ns |    300.0 ns |
// | BenchConvertTo | Boolean          | Any              | 14,742.9 ns |   490.04 ns | 1,316.47 ns | 14,500.0 ns |
// | BenchConvertTo | Boolean          | Pointer          | 15,643.8 ns |   909.26 ns | 2,519.56 ns | 14,900.0 ns |
// | BenchConvertTo | Boolean          | Boolean          |    429.4 ns |    29.00 ns |    78.41 ns |    400.0 ns |
// | BenchConvertTo | Boolean          | Integer          |  1,212.9 ns |   106.16 ns |   301.17 ns |  1,100.0 ns |
// | BenchConvertTo | Boolean          | ByteString       |    852.9 ns |    33.81 ns |    92.56 ns |    800.0 ns |
// | BenchConvertTo | Boolean          | Buffer           |  1,149.4 ns |    48.84 ns |   133.71 ns |  1,100.0 ns |
// | BenchConvertTo | Boolean          | Array            | 15,522.2 ns |   880.38 ns | 2,454.15 ns | 14,800.0 ns |
// | BenchConvertTo | Boolean          | Struct           | 17,402.0 ns | 1,281.64 ns | 3,758.83 ns | 15,600.0 ns |
// | BenchConvertTo | Boolean          | Map              | 16,755.1 ns | 1,201.55 ns | 3,504.96 ns | 15,300.0 ns |
// | BenchConvertTo | Boolean          | InteropInterface | 15,295.3 ns |   790.74 ns | 2,137.81 ns | 14,800.0 ns |
// | BenchConvertTo | Integer          | Any              | 15,310.3 ns |   674.15 ns | 1,845.49 ns | 14,900.0 ns |
// | BenchConvertTo | Integer          | Pointer          | 17,211.1 ns | 1,217.07 ns | 3,569.45 ns | 15,600.0 ns |
// | BenchConvertTo | Integer          | Boolean          |    687.7 ns |    33.62 ns |    88.58 ns |    700.0 ns |
// | BenchConvertTo | Integer          | Integer          |    402.4 ns |    18.72 ns |    49.63 ns |    400.0 ns |
// | BenchConvertTo | Integer          | ByteString       |  1,119.1 ns |    73.57 ns |   203.87 ns |  1,000.0 ns |
// | BenchConvertTo | Integer          | Buffer           |  1,335.7 ns |    45.00 ns |   120.88 ns |  1,300.0 ns |
// | BenchConvertTo | Integer          | Array            | 15,806.9 ns |   785.20 ns | 2,149.46 ns | 15,200.0 ns |
// | BenchConvertTo | Integer          | Struct           | 17,458.6 ns | 1,251.52 ns | 3,670.48 ns | 16,100.0 ns |
// | BenchConvertTo | Integer          | Map              | 14,805.8 ns |   614.89 ns | 1,672.85 ns | 14,500.0 ns |
// | BenchConvertTo | Integer          | InteropInterface | 16,758.2 ns | 1,122.58 ns | 3,274.61 ns | 15,700.0 ns |
// | BenchConvertTo | ByteString       | Any              | 19,682.0 ns | 1,548.08 ns | 4,564.56 ns | 19,750.0 ns |
// | BenchConvertTo | ByteString       | Pointer          | 16,710.3 ns | 1,194.82 ns | 3,466.40 ns | 15,500.0 ns |
// | BenchConvertTo | ByteString       | Boolean          |  1,112.0 ns |   129.91 ns |   383.04 ns |    900.0 ns |
// | BenchConvertTo | ByteString       | Integer          |  1,119.3 ns |    48.88 ns |   134.64 ns |  1,100.0 ns |
// | BenchConvertTo | ByteString       | ByteString       |    496.0 ns |    57.43 ns |   169.32 ns |    400.0 ns |
// | BenchConvertTo | ByteString       | Buffer           |  1,180.2 ns |    42.50 ns |   115.63 ns |  1,200.0 ns |
// | BenchConvertTo | ByteString       | Array            | 16,655.9 ns | 1,196.52 ns | 3,394.32 ns | 15,300.0 ns |
// | BenchConvertTo | ByteString       | Struct           | 15,635.6 ns |   902.43 ns | 2,470.38 ns | 15,100.0 ns |
// | BenchConvertTo | ByteString       | Map              | 16,024.7 ns | 1,126.73 ns | 3,196.35 ns | 14,900.0 ns |
// | BenchConvertTo | ByteString       | InteropInterface | 14,730.2 ns |   646.07 ns | 1,757.68 ns | 14,350.0 ns |
// | BenchConvertTo | Buffer           | Any              | 20,139.8 ns | 1,616.78 ns | 4,716.21 ns | 19,250.0 ns |
// | BenchConvertTo | Buffer           | Pointer          | 15,179.5 ns |   698.17 ns | 1,922.95 ns | 14,850.0 ns |
// | BenchConvertTo | Buffer           | Boolean          |    773.4 ns |    86.65 ns |   247.21 ns |    700.0 ns |
// | BenchConvertTo | Buffer           | Integer          | 14,946.5 ns | 1,159.94 ns | 3,401.90 ns | 13,600.0 ns |
// | BenchConvertTo | Buffer           | ByteString       |  1,095.4 ns |    66.60 ns |   182.30 ns |  1,000.0 ns |
// | BenchConvertTo | Buffer           | Buffer           |    424.7 ns |    21.28 ns |    57.54 ns |    400.0 ns |
// | BenchConvertTo | Buffer           | Array            | 15,062.8 ns |   544.47 ns | 1,481.26 ns | 14,650.0 ns |
// | BenchConvertTo | Buffer           | Struct           | 16,300.0 ns | 1,259.80 ns | 3,614.60 ns | 14,800.0 ns |
// | BenchConvertTo | Buffer           | Map              | 16,589.9 ns | 1,248.48 ns | 3,661.59 ns | 15,000.0 ns |
// | BenchConvertTo | Buffer           | InteropInterface | 14,602.4 ns |   495.92 ns | 1,323.70 ns | 14,400.0 ns |
// | BenchConvertTo | Array            | Any              | 17,636.0 ns | 1,334.95 ns | 3,936.14 ns | 16,200.0 ns |
// | BenchConvertTo | Array            | Pointer          | 15,381.7 ns |   853.59 ns | 2,421.50 ns | 14,600.0 ns |
// | BenchConvertTo | Array            | Boolean          |    870.0 ns |   111.97 ns |   330.14 ns |    700.0 ns |
// | BenchConvertTo | Array            | Integer          | 14,697.6 ns |   658.01 ns | 1,767.71 ns | 14,200.0 ns |
// | BenchConvertTo | Array            | ByteString       | 17,281.0 ns | 1,164.40 ns | 3,433.25 ns | 16,000.0 ns |
// | BenchConvertTo | Array            | Buffer           | 15,321.3 ns |   868.73 ns | 2,407.26 ns | 14,700.0 ns |
// | BenchConvertTo | Array            | Array            |    421.6 ns |    18.63 ns |    51.30 ns |    400.0 ns |
// | BenchConvertTo | Array            | Struct           |  2,410.0 ns |    51.87 ns |   116.01 ns |  2,400.0 ns |
// | BenchConvertTo | Array            | Map              | 16,611.2 ns | 1,195.65 ns | 3,487.77 ns | 15,200.0 ns |
// | BenchConvertTo | Array            | InteropInterface | 16,639.4 ns | 1,216.06 ns | 3,566.51 ns | 14,900.0 ns |
// | BenchConvertTo | Struct           | Any              | 18,582.8 ns | 1,270.54 ns | 3,726.27 ns | 16,900.0 ns |
// | BenchConvertTo | Struct           | Pointer          | 17,766.0 ns | 1,172.27 ns | 3,400.98 ns | 16,500.0 ns |
// | BenchConvertTo | Struct           | Boolean          |    675.6 ns |    42.45 ns |   118.33 ns |    600.0 ns |
// | BenchConvertTo | Struct           | Integer          | 16,930.7 ns |   942.98 ns | 2,597.23 ns | 16,100.0 ns |
// | BenchConvertTo | Struct           | ByteString       | 16,219.0 ns |   671.54 ns | 1,804.04 ns | 16,100.0 ns |
// | BenchConvertTo | Struct           | Buffer           | 18,178.8 ns | 1,341.14 ns | 3,933.33 ns | 16,600.0 ns |
// | BenchConvertTo | Struct           | Array            |  2,760.0 ns |    63.61 ns |   171.96 ns |  2,700.0 ns |
// | BenchConvertTo | Struct           | Struct           |    436.0 ns |    21.03 ns |    57.22 ns |    400.0 ns |
// | BenchConvertTo | Struct           | Map              | 16,154.5 ns |   728.80 ns | 2,007.34 ns | 15,600.0 ns |
// | BenchConvertTo | Struct           | InteropInterface | 17,226.9 ns |   907.21 ns | 2,573.62 ns | 16,700.0 ns |
// | BenchConvertTo | Map              | Any              | 16,645.0 ns | 1,423.56 ns | 4,197.41 ns | 15,100.0 ns |
// | BenchConvertTo | Map              | Pointer          | 13,489.4 ns |   493.59 ns | 1,334.44 ns | 13,200.0 ns |
// | BenchConvertTo | Map              | Boolean          |    757.3 ns |   104.49 ns |   301.49 ns |    600.0 ns |
// | BenchConvertTo | Map              | Integer          | 13,691.7 ns |   600.17 ns | 1,612.32 ns | 13,400.0 ns |
// | BenchConvertTo | Map              | ByteString       | 13,491.5 ns |   547.49 ns | 1,451.88 ns | 13,300.0 ns |
// | BenchConvertTo | Map              | Buffer           | 15,646.0 ns | 1,193.79 ns | 3,519.91 ns | 14,200.0 ns |
// | BenchConvertTo | Map              | Array            | 14,438.9 ns |   939.14 ns | 2,617.95 ns | 13,550.0 ns |
// | BenchConvertTo | Map              | Struct           | 16,564.0 ns | 1,531.10 ns | 4,514.48 ns | 14,600.0 ns |
// | BenchConvertTo | Map              | Map              |    412.2 ns |    22.06 ns |    61.49 ns |    400.0 ns |
// | BenchConvertTo | Map              | InteropInterface | 14,404.3 ns |   945.17 ns | 2,665.86 ns | 13,600.0 ns |
// | BenchConvertTo | InteropInterface | Any              | 16,915.0 ns | 1,494.93 ns | 4,407.83 ns | 14,750.0 ns |
// | BenchConvertTo | InteropInterface | Pointer          | 13,469.0 ns |   697.14 ns | 1,872.82 ns | 13,000.0 ns |
// | BenchConvertTo | InteropInterface | Boolean          |    680.5 ns |    56.77 ns |   155.40 ns |    600.0 ns |
// | BenchConvertTo | InteropInterface | Integer          | 14,928.9 ns | 1,125.45 ns | 3,265.12 ns | 13,600.0 ns |
// | BenchConvertTo | InteropInterface | ByteString       | 17,205.0 ns | 1,428.99 ns | 4,213.41 ns | 15,350.0 ns |
// | BenchConvertTo | InteropInterface | Buffer           | 13,597.7 ns |   565.77 ns | 1,539.21 ns | 13,300.0 ns |
// | BenchConvertTo | InteropInterface | Array            | 15,566.0 ns | 1,262.36 ns | 3,722.09 ns | 13,700.0 ns |
// | BenchConvertTo | InteropInterface | Struct           | 14,753.1 ns | 1,086.52 ns | 3,169.42 ns | 13,600.0 ns |
// | BenchConvertTo | InteropInterface | Map              | 14,618.3 ns | 1,010.64 ns | 2,867.01 ns | 13,600.0 ns |
// | BenchConvertTo | InteropInterface | InteropInterface |    381.3 ns |    15.52 ns |    39.23 ns |    400.0 ns |
