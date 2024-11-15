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
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.VM.Benchmark
{
    public class Benchmarks_Convert
    {
        private Dictionary<StackItemType, List<StackItem>>? testItemsByType;

        [GlobalSetup]
        public void Setup()
        {
            testItemsByType = CreateTestItemsByType();
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetTypeConversionPairs))]
        public void BenchConvertTo(StackItemType fromType, StackItemType toType)
        {
            if (testItemsByType is null)
                throw new InvalidOperationException($"{nameof(testItemsByType)} not initialized");

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
                    yield return new object[] { fromType, toType };
                }
            }
        }

        private Dictionary<StackItemType, List<StackItem>> CreateTestItemsByType()
        {
            var referenceCounter = new ReferenceCounterV2();
            var result = new Dictionary<StackItemType, List<StackItem>>();

            foreach (StackItemType type in Enum.GetValues(typeof(StackItemType)))
            {
                result[type] = new List<StackItem>();
            }

            result[StackItemType.Boolean].Add(StackItem.True);
            result[StackItemType.Boolean].Add(StackItem.False);

            result[StackItemType.Integer].Add(new Integer(42));
            result[StackItemType.Integer].Add(new Integer(-1));

            result[StackItemType.ByteString].Add(new ByteString(new byte[] { 1, 2, 3 }));
            result[StackItemType.ByteString].Add(new ByteString(new byte[] { 255, 0, 128 }));

            // Create a 128-byte buffer
            var longBuffer = new byte[128];
            for (int i = 0; i < 128; i++) longBuffer[i] = (byte)(i % 256);
            result[StackItemType.Buffer].Add(new Buffer(longBuffer));
            result[StackItemType.Buffer].Add(new Buffer(new byte[128])); // Another 128-byte buffer, all zeros

            // Create an array with 10 items
            var longArray = new Array();
            for (int i = 0; i < 10; i++) longArray.Add(new Integer(i));
            result[StackItemType.Array].Add(longArray);
            result[StackItemType.Array].Add(new Array() { StackItem.True, new ByteString(new byte[] { 3, 4, 5 }) });

            // Create a struct with 10 items
            var longStruct = new Struct();
            for (int i = 0; i < 10; i++) longStruct.Add(new Integer(i * 10));
            result[StackItemType.Struct].Add(longStruct);
            result[StackItemType.Struct].Add(new Struct() { StackItem.False, new Buffer(new byte[] { 6, 7, 8 }) });

            // Create a map with 10 items
            var longMap = new Map();
            for (int i = 0; i < 10; i++) longMap[new Integer(i)] = new ByteString(new byte[] { (byte)(i * 20) });
            result[StackItemType.Map].Add(longMap);
            result[StackItemType.Map].Add(new Map() { [new ByteString(new byte[] { 9 })] = StackItem.True });

            result[StackItemType.InteropInterface].Add(new InteropInterface(new object()));
            result[StackItemType.InteropInterface].Add(new InteropInterface("test string"));

            return result;
        }
    }
}

// BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4249/23H2/2023Update/SunValley3)
// Intel Core i9-14900HX, 1 CPU, 32 logical and 24 physical cores
// .NET SDK 8.0.205
//   [Host]     : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
//   DefaultJob : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
//
//
// | Method         | fromType         | toType           | Mean         | Error       | StdDev      |
// |--------------- |----------------- |----------------- |-------------:|------------:|------------:|
// | BenchConvertTo | Any              | Any              |     1.762 ns |   0.0195 ns |   0.0182 ns |
// | BenchConvertTo | Any              | Pointer          |     1.791 ns |   0.0196 ns |   0.0183 ns |
// | BenchConvertTo | Any              | Boolean          |     1.774 ns |   0.0245 ns |   0.0229 ns |
// | BenchConvertTo | Any              | Integer          |     1.781 ns |   0.0236 ns |   0.0220 ns |
// | BenchConvertTo | Any              | ByteString       |     1.767 ns |   0.0255 ns |   0.0226 ns |
// | BenchConvertTo | Any              | Buffer           |     1.774 ns |   0.0217 ns |   0.0203 ns |
// | BenchConvertTo | Any              | Array            |     1.770 ns |   0.0412 ns |   0.0385 ns |
// | BenchConvertTo | Any              | Struct           |     1.787 ns |   0.0227 ns |   0.0212 ns |
// | BenchConvertTo | Any              | Map              |     1.796 ns |   0.0292 ns |   0.0273 ns |
// | BenchConvertTo | Any              | InteropInterface |     1.820 ns |   0.0549 ns |   0.0675 ns |
// | BenchConvertTo | Pointer          | Any              |     2.312 ns |   0.0210 ns |   0.0175 ns |
// | BenchConvertTo | Pointer          | Pointer          |     2.337 ns |   0.0157 ns |   0.0146 ns |
// | BenchConvertTo | Pointer          | Boolean          |     2.352 ns |   0.0190 ns |   0.0169 ns |
// | BenchConvertTo | Pointer          | Integer          |     2.334 ns |   0.0231 ns |   0.0216 ns |
// | BenchConvertTo | Pointer          | ByteString       |     2.317 ns |   0.0298 ns |   0.0279 ns |
// | BenchConvertTo | Pointer          | Buffer           |     2.329 ns |   0.0274 ns |   0.0256 ns |
// | BenchConvertTo | Pointer          | Array            |     2.338 ns |   0.0257 ns |   0.0241 ns |
// | BenchConvertTo | Pointer          | Struct           |     2.336 ns |   0.0318 ns |   0.0298 ns |
// | BenchConvertTo | Pointer          | Map              |     2.351 ns |   0.0676 ns |   0.0903 ns |
// | BenchConvertTo | Pointer          | InteropInterface |     2.281 ns |   0.0133 ns |   0.0125 ns |
// | BenchConvertTo | Boolean          | Any              | 5,926.451 ns | 118.1195 ns | 136.0266 ns |
// | BenchConvertTo | Boolean          | Pointer          | 6,001.282 ns |  15.3048 ns |  12.7802 ns |
// | BenchConvertTo | Boolean          | Boolean          |     4.459 ns |   0.0151 ns |   0.0133 ns |
// | BenchConvertTo | Boolean          | Integer          |    14.104 ns |   0.1526 ns |   0.1428 ns |
// | BenchConvertTo | Boolean          | ByteString       |    11.650 ns |   0.0539 ns |   0.0450 ns |
// | BenchConvertTo | Boolean          | Buffer           |    26.106 ns |   0.1549 ns |   0.1449 ns |
// | BenchConvertTo | Boolean          | Array            | 5,813.116 ns |  28.1911 ns |  26.3700 ns |
// | BenchConvertTo | Boolean          | Struct           | 5,809.844 ns |  19.1249 ns |  15.9702 ns |
// | BenchConvertTo | Boolean          | Map              | 6,061.558 ns |  29.3991 ns |  27.4999 ns |
// | BenchConvertTo | Boolean          | InteropInterface | 5,924.682 ns |  80.5533 ns |  75.3496 ns |
// | BenchConvertTo | Integer          | Any              | 5,240.903 ns |  41.0628 ns |  38.4102 ns |
// | BenchConvertTo | Integer          | Pointer          | 5,479.116 ns |  75.8232 ns |  70.9251 ns |
// | BenchConvertTo | Integer          | Boolean          |     5.981 ns |   0.0445 ns |   0.0416 ns |
// | BenchConvertTo | Integer          | Integer          |     4.277 ns |   0.0177 ns |   0.0166 ns |
// | BenchConvertTo | Integer          | ByteString       |    19.053 ns |   0.2125 ns |   0.1883 ns |
// | BenchConvertTo | Integer          | Buffer           |    32.782 ns |   0.1653 ns |   0.1380 ns |
// | BenchConvertTo | Integer          | Array            | 4,693.207 ns |  14.2446 ns |  12.6275 ns |
// | BenchConvertTo | Integer          | Struct           | 4,737.341 ns |  60.1813 ns |  56.2936 ns |
// | BenchConvertTo | Integer          | Map              | 4,808.431 ns |  23.5380 ns |  22.0174 ns |
// | BenchConvertTo | Integer          | InteropInterface | 4,684.409 ns |  24.7033 ns |  21.8989 ns |
// | BenchConvertTo | ByteString       | Any              | 5,833.857 ns |  20.1553 ns |  18.8533 ns |
// | BenchConvertTo | ByteString       | Pointer          | 5,807.973 ns |  11.7754 ns |  10.4386 ns |
// | BenchConvertTo | ByteString       | Boolean          |    33.007 ns |   0.1574 ns |   0.1472 ns |
// | BenchConvertTo | ByteString       | Integer          |    23.622 ns |   0.0755 ns |   0.0669 ns |
// | BenchConvertTo | ByteString       | ByteString       |     4.288 ns |   0.0152 ns |   0.0142 ns |
// | BenchConvertTo | ByteString       | Buffer           |    24.881 ns |   0.0889 ns |   0.0788 ns |
// | BenchConvertTo | ByteString       | Array            | 6,030.813 ns |  19.9562 ns |  18.6670 ns |
// | BenchConvertTo | ByteString       | Struct           | 5,811.185 ns |  24.0781 ns |  22.5226 ns |
// | BenchConvertTo | ByteString       | Map              | 5,866.820 ns |  17.0315 ns |  15.0980 ns |
// | BenchConvertTo | ByteString       | InteropInterface | 5,757.124 ns |  16.3184 ns |  14.4658 ns |
// | BenchConvertTo | Buffer           | Any              | 4,886.279 ns |  17.1370 ns |  14.3102 ns |
// | BenchConvertTo | Buffer           | Pointer          | 4,698.364 ns |  14.5491 ns |  12.1492 ns |
// | BenchConvertTo | Buffer           | Boolean          |     6.130 ns |   0.0323 ns |   0.0302 ns |
// | BenchConvertTo | Buffer           | Integer          | 4,645.764 ns |  15.8146 ns |  14.7930 ns |
// | BenchConvertTo | Buffer           | ByteString       |    29.874 ns |   0.1518 ns |   0.1268 ns |
// | BenchConvertTo | Buffer           | Buffer           |     4.939 ns |   0.0190 ns |   0.0178 ns |
// | BenchConvertTo | Buffer           | Array            | 4,683.427 ns |  21.3813 ns |  20.0001 ns |
// | BenchConvertTo | Buffer           | Struct           | 4,680.762 ns |  15.7220 ns |  13.9371 ns |
// | BenchConvertTo | Buffer           | Map              | 4,706.510 ns |  14.2061 ns |  12.5934 ns |
// | BenchConvertTo | Buffer           | InteropInterface | 4,703.050 ns |  15.8002 ns |  14.0064 ns |
// | BenchConvertTo | Array            | Any              | 4,652.710 ns |  23.2061 ns |  20.5716 ns |
// | BenchConvertTo | Array            | Pointer          | 4,625.049 ns |  12.4455 ns |  11.6415 ns |
// | BenchConvertTo | Array            | Boolean          |     5.568 ns |   0.0181 ns |   0.0169 ns |
// | BenchConvertTo | Array            | Integer          | 4,659.897 ns |  19.8036 ns |  18.5243 ns |
// | BenchConvertTo | Array            | ByteString       | 4,663.020 ns |  12.4988 ns |  11.6914 ns |
// | BenchConvertTo | Array            | Buffer           | 4,680.281 ns |  14.9748 ns |  13.2748 ns |
// | BenchConvertTo | Array            | Array            |     4.246 ns |   0.0124 ns |   0.0110 ns |
// | BenchConvertTo | Array            | Struct           | 1,193.106 ns |  98.5374 ns | 285.8748 ns |
// | BenchConvertTo | Array            | Map              | 4,742.631 ns |  35.5855 ns |  33.2867 ns |
// | BenchConvertTo | Array            | InteropInterface | 4,670.743 ns |   9.3547 ns |   7.8116 ns |
// | BenchConvertTo | Struct           | Any              | 4,643.558 ns |  31.0451 ns |  29.0396 ns |
// | BenchConvertTo | Struct           | Pointer          | 4,867.925 ns |  22.2347 ns |  19.7105 ns |
// | BenchConvertTo | Struct           | Boolean          |     5.581 ns |   0.0251 ns |   0.0235 ns |
// | BenchConvertTo | Struct           | Integer          | 4,653.442 ns |  17.7417 ns |  16.5956 ns |
// | BenchConvertTo | Struct           | ByteString       | 4,646.242 ns |  13.7830 ns |  12.8926 ns |
// | BenchConvertTo | Struct           | Buffer           | 4,776.205 ns |  14.1918 ns |  13.2751 ns |
// | BenchConvertTo | Struct           | Array            | 1,622.573 ns | 144.8116 ns | 398.8532 ns |
// | BenchConvertTo | Struct           | Struct           |     4.195 ns |   0.0327 ns |   0.0290 ns |
// | BenchConvertTo | Struct           | Map              | 4,672.579 ns |  17.6257 ns |  16.4871 ns |
// | BenchConvertTo | Struct           | InteropInterface | 4,653.476 ns |   8.2047 ns |   7.6747 ns |
// | BenchConvertTo | Map              | Any              | 4,676.540 ns |  15.2010 ns |  13.4753 ns |
// | BenchConvertTo | Map              | Pointer          | 4,663.489 ns |  13.7871 ns |  12.2219 ns |
// | BenchConvertTo | Map              | Boolean          |     5.535 ns |   0.0205 ns |   0.0192 ns |
// | BenchConvertTo | Map              | Integer          | 4,661.275 ns |  12.4402 ns |  11.6366 ns |
// | BenchConvertTo | Map              | ByteString       | 4,662.482 ns |  25.7111 ns |  24.0502 ns |
// | BenchConvertTo | Map              | Buffer           | 4,859.809 ns |  18.2981 ns |  16.2208 ns |
// | BenchConvertTo | Map              | Array            | 4,627.149 ns |  10.7487 ns |   9.5285 ns |
// | BenchConvertTo | Map              | Struct           | 4,646.504 ns |  22.4190 ns |  20.9707 ns |
// | BenchConvertTo | Map              | Map              |     4.160 ns |   0.0180 ns |   0.0169 ns |
// | BenchConvertTo | Map              | InteropInterface | 4,667.024 ns |  14.1790 ns |  13.2630 ns |
// | BenchConvertTo | InteropInterface | Any              | 4,700.511 ns |  17.4725 ns |  15.4889 ns |
// | BenchConvertTo | InteropInterface | Pointer          | 4,705.819 ns |  25.2035 ns |  23.5754 ns |
// | BenchConvertTo | InteropInterface | Boolean          |     5.557 ns |   0.0244 ns |   0.0228 ns |
// | BenchConvertTo | InteropInterface | Integer          | 4,695.410 ns |  21.8674 ns |  20.4547 ns |
// | BenchConvertTo | InteropInterface | ByteString       | 4,674.552 ns |  18.8705 ns |  17.6515 ns |
// | BenchConvertTo | InteropInterface | Buffer           | 4,649.237 ns |  23.9084 ns |  22.3639 ns |
// | BenchConvertTo | InteropInterface | Array            | 4,827.652 ns |  29.7153 ns |  27.7957 ns |
// | BenchConvertTo | InteropInterface | Struct           | 4,624.202 ns |  10.3563 ns |   8.0855 ns |
// | BenchConvertTo | InteropInterface | Map              | 4,695.310 ns |  23.1192 ns |  21.6257 ns |
// | BenchConvertTo | InteropInterface | InteropInterface |     4.137 ns |   0.0156 ns |   0.0138 ns |
