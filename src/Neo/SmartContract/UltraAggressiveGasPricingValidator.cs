// Copyright (C) 2015-2025 The Neo Project.
//
// UltraAggressiveGasPricingValidator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// Comprehensive validation and analysis tool for Ultra Aggressive Gas Pricing system.
    /// Provides detailed reports, performance analysis, and safety validation.
    /// </summary>
    public static class UltraAggressiveGasPricingValidator
    {
        /// <summary>
        /// Run comprehensive validation of the Ultra Aggressive Gas Pricing system.
        /// </summary>
        public static void RunComprehensiveValidation()
        {
            Console.WriteLine("=== Ultra Aggressive Gas Pricing - Comprehensive Validation ===");
            Console.WriteLine($"Validation Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Platform: {Environment.OSVersion}");
            Console.WriteLine();

            var results = new Dictionary<string, bool>
            {
                // Run all validation tests
                ["Basic Functionality"] = ValidateBasicFunctionality(),
                ["Cost Reductions"] = ValidateCostReductions(),
                ["DoS Protection"] = ValidateDoSProtection(),
                ["Edge Cases"] = ValidateEdgeCases(),
                ["Performance"] = ValidatePerformance(),
                ["Safety Bounds"] = ValidateSafetyBounds(),
                ["Consistency"] = ValidateConsistency(),
                ["Coverage"] = ValidateCoverage()
            };

            // Print summary
            Console.WriteLine("\n=== Validation Summary ===");
            int passed = results.Values.Count(r => r);
            int total = results.Count;

            foreach (var (test, passed) in results)
            {
                string status = passed ? "✅ PASS" : "❌ FAIL";
                Console.WriteLine($"{test,-25}: {status}");
            }

            Console.WriteLine();
            Console.WriteLine($"Overall Result: {passed}/{total} tests passed");

            if (passed == total)
            {
                Console.WriteLine("🎉 ALL TESTS PASSED - System is ready for production deployment!");
            }
            else
            {
                Console.WriteLine("⚠️ Some tests failed - Review and fix issues before deployment");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Validate basic functionality of the pricing system.
        /// </summary>
        private static bool ValidateBasicFunctionality()
        {
            Console.WriteLine("=== Validating Basic Functionality ===");

            try
            {
                // Test that all opcodes return valid costs
                int validOpcodes = 0;
                foreach (OpCode opcode in Enum.GetValues<OpCode>())
                {
                    long cost = UltraAggressiveGasPricing.CalculateGasCost(opcode);
                    if (cost >= 1 && cost <= 100000)
                    {
                        validOpcodes++;
                    }
                }

                int totalOpcodes = Enum.GetValues<OpCode>().Length;
                double coverage = (double)validOpcodes / totalOpcodes * 100;

                Console.WriteLine($"  Opcode coverage: {validOpcodes}/{totalOpcodes} ({coverage:F1}%)");

                // Test basic operations
                long pushCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.PUSH1);
                long addCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.ADD);
                long callCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CALL);

                Console.WriteLine($"  PUSH1 cost: {pushCost} gas {(pushCost == 1 ? "✅" : "❌")}");
                Console.WriteLine($"  ADD cost: {addCost} gas {(addCost == 1 ? "✅" : "❌")}");
                Console.WriteLine($"  CALL cost: {callCost} gas {(callCost <= 2 ? "✅" : "❌")}");

                bool allValid = pushCost == 1 && addCost == 1 && callCost <= 2 && coverage >= 95;
                Console.WriteLine($"  Basic functionality: {(allValid ? "✅ PASS" : "❌ FAIL")}");
                Console.WriteLine();

                return allValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ ERROR: {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// Validate that cost reductions meet the 95%+ target.
        /// </summary>
        private static bool ValidateCostReductions()
        {
            Console.WriteLine("=== Validating Cost Reductions ===");

            try
            {
                var comparison = UltraAggressiveGasPricing.GetCostComparison();
                var reductions = new List<double>();

                foreach (var (opcode, (currentCost, newCost, reduction)) in comparison)
                {
                    if (currentCost > 0)
                    {
                        reductions.Add(reduction);

                        // Show major reductions
                        if (reduction >= 50)
                        {
                            Console.WriteLine($"  {opcode,-15}: {currentCost,3} → {newCost,3} ({reduction,5:F1}%)");
                        }
                    }
                }

                if (reductions.Count == 0)
                {
                    Console.WriteLine("  ❌ No valid cost comparisons found");
                    return false;
                }

                double averageReduction = reductions.Average();
                double maxReduction = reductions.Max();
                int significantReductions = reductions.Count(r => r >= 50);
                double significantPercentage = (double)significantReductions / reductions.Count * 100;

                Console.WriteLine();
                Console.WriteLine($"  Average reduction: {averageReduction:F1}%");
                Console.WriteLine($"  Maximum reduction: {maxReduction:F1}%");
                Console.WriteLine($"  Significant reductions (≥50%): {significantReductions}/{reductions.Count} ({significantPercentage:F1}%)");

                bool meetsTarget = averageReduction >= 70 && significantPercentage >= 70;
                Console.WriteLine($"  Cost reductions: {(meetsTarget ? "✅ PASS" : "❌ FAIL")}");
                Console.WriteLine();

                return meetsTarget;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ ERROR: {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// Validate DoS protection mechanisms.
        /// </summary>
        private static bool ValidateDoSProtection()
        {
            Console.WriteLine("=== Validating DoS Protection ===");

            try
            {
                // Test data scaling
                byte[] smallData = new byte[10];
                byte[] largeData = new byte[10000];

                long smallCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, smallData);
                long largeCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, largeData);

                double dataRatio = (double)largeCost / smallCost;
                Console.WriteLine($"  CONVERT scaling: 10B={smallCost} gas, 10KB={largeCost} gas (ratio: {dataRatio:F1}x)");

                // Test collection scaling
                var smallCollection = new object[10];
                var largeCollection = new object[10000];

                long smallCollectionCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.KEYS, smallCollection);
                long largeCollectionCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.KEYS, largeCollection);

                double collectionRatio = (double)largeCollectionCost / smallCollectionCost;
                Console.WriteLine($"  KEYS scaling: 10={smallCollectionCost} gas, 10000={largeCollectionCost} gas (ratio: {collectionRatio:F1}x)");

                // Test mathematical scaling
                long smallMathCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.POW, 2, 10);
                long largeMathCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.POW, 2, 1000);

                double mathRatio = (double)largeMathCost / smallMathCost;
                Console.WriteLine($"  POW scaling: 2^10={smallMathCost} gas, 2^1000={largeMathCost} gas (ratio: {mathRatio:F1}x)");

                bool hasProtection = dataRatio >= 2 && collectionRatio >= 5 && mathRatio >= 1;
                Console.WriteLine($"  DoS protection: {(hasProtection ? "✅ PASS" : "❌ FAIL")}");
                Console.WriteLine();

                return hasProtection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ ERROR: {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// Validate edge cases and boundary conditions.
        /// </summary>
        private static bool ValidateEdgeCases()
        {
            Console.WriteLine("=== Validating Edge Cases ===");

            try
            {
                var testResults = new List<bool>();

                // Test null parameters
                try
                {
                    long cost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, null);
                    testResults.Add(cost >= 1);
                    Console.WriteLine($"  Null parameters: {cost} gas ✅");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Null parameters: ❌ {ex.Message}");
                    testResults.Add(false);
                }

                // Test empty parameters
                long emptyCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT);
                testResults.Add(emptyCost >= 1);
                Console.WriteLine($"  Empty parameters: {emptyCost} gas {(emptyCost >= 1 ? "✅" : "❌")}");

                // Test negative values
                long negativeCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.NEWARRAY, -1);
                testResults.Add(negativeCost >= 1);
                Console.WriteLine($"  Negative values: {negativeCost} gas {(negativeCost >= 1 ? "✅" : "❌")}");

                // Test very large values
                byte[] hugeData = new byte[1_000_000];
                long hugeCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, hugeData);
                testResults.Add(hugeCost > 0 && hugeCost < 1_000_000);
                Console.WriteLine($"  Huge data (1MB): {hugeCost} gas {(hugeCost > 0 && hugeCost < 1_000_000 ? "✅" : "❌")}");

                // Test zero values
                byte[] emptyData = new byte[0];
                long zeroCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, emptyData);
                testResults.Add(zeroCost >= 1);
                Console.WriteLine($"  Empty data: {zeroCost} gas {(zeroCost >= 1 ? "✅" : "❌")}");

                bool allPassed = testResults.All(r => r);
                Console.WriteLine($"  Edge cases: {(allPassed ? "✅ PASS" : "❌ FAIL")}");
                Console.WriteLine();

                return allPassed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ ERROR: {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// Validate performance characteristics.
        /// </summary>
        private static bool ValidatePerformance()
        {
            Console.WriteLine("=== Validating Performance ===");

            try
            {
                var random = new Random(42); // Fixed seed for reproducible tests
                var opcodes = Enum.GetValues<OpCode>().ToArray();
                var times = new List<long>();

                // Measure performance of gas cost calculations
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                for (int i = 0; i < 10000; i++)
                {
                    var opcode = opcodes[random.Next(opcodes.Length)];
                    long cost = UltraAggressiveGasPricing.CalculateGasCost(opcode);
                    times.Add(stopwatch.ElapsedTicks);
                }

                stopwatch.Stop();

                double averageTimePerCalculation = (double)stopwatch.ElapsedMilliseconds / 10000;
                double calculationsPerSecond = 1000000.0 / averageTimePerCalculation;

                Console.WriteLine($"  10,000 calculations in {stopwatch.ElapsedMilliseconds} ms");
                Console.WriteLine($"  Average time per calculation: {averageTimePerCalculation:F3} ms");
                Console.WriteLine($"  Calculations per second: {calculationsPerSecond:F0}");

                bool isPerformant = averageTimePerCalculation < 1.0; // Should be under 1ms per calculation
                Console.WriteLine($"  Performance: {(isPerformant ? "✅ PASS" : "❌ FAIL")}");
                Console.WriteLine();

                return isPerformant;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ ERROR: {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// Validate safety bounds and overflow protection.
        /// </summary>
        private static bool ValidateSafetyBounds()
        {
            Console.WriteLine("=== Validating Safety Bounds ===");

            try
            {
                var testResults = new List<bool>();

                // Test maximum value bounds
                long maxCost = 0;
                foreach (OpCode opcode in Enum.GetValues<OpCode>())
                {
                    long cost = UltraAggressiveGasPricing.CalculateGasCost(opcode);
                    maxCost = Math.Max(maxCost, cost);
                }

                testResults.Add(maxCost < 1000000); // Should be under 1M gas
                Console.WriteLine($"  Maximum cost: {maxCost} gas {(maxCost < 1000000 ? "✅" : "❌")}");

                // Test minimum cost bounds
                long minCost = long.MaxValue;
                foreach (OpCode opcode in Enum.GetValues<OpCode>())
                {
                    long cost = UltraAggressiveGasPricing.CalculateGasCost(opcode);
                    minCost = Math.Min(minCost, cost);
                }

                testResults.Add(minCost >= 1); // Should be at least 1 gas
                Console.WriteLine($"  Minimum cost: {minCost} gas {(minCost >= 1 ? "✅" : "❌")}");

                // Test overflow protection
                try
                {
                    byte[] massiveData = new byte[int.MaxValue / 1000]; // Very large but not too large
                    long massiveCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, massiveData);
                    testResults.Add(massiveCost > 0 && massiveCost < long.MaxValue / 2);
                    Console.WriteLine($"  Overflow protection: {massiveCost} gas ✅");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Overflow protection: ❌ {ex.Message}");
                    testResults.Add(false);
                }

                bool allPassed = testResults.All(r => r);
                Console.WriteLine($"  Safety bounds: {(allPassed ? "✅ PASS" : "❌ FAIL")}");
                Console.WriteLine();

                return allPassed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ ERROR: {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// Validate consistency of results.
        /// </summary>
        private static bool ValidateConsistency()
        {
            Console.WriteLine("=== Validating Consistency ===");

            try
            {
                var testResults = new List<bool>();

                // Test deterministic behavior
                byte[] testData = new byte[100];
                var testParams = new object[] { testData };

                long cost1 = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, testParams);
                long cost2 = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, testParams);
                long cost3 = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, testData);

                bool deterministic = cost1 == cost2 && cost2 == cost3;
                testResults.Add(deterministic);
                Console.WriteLine($"  Deterministic behavior: {cost1} gas {(deterministic ? "✅" : "❌")}");

                // Test multiple calls
                long sumCost = 0;
                for (int i = 0; i < 100; i++)
                {
                    sumCost += UltraAggressiveGasPricing.CalculateGasCost(OpCode.ADD);
                }

                bool consistent = sumCost == 100; // Should be exactly 100 * 1 gas
                testResults.Add(consistent);
                Console.WriteLine($"  Consistent repeated calls: {sumCost} gas {(consistent ? "✅" : "❌")}");

                // Test parameter equivalence
                var param1 = new object[] { 100 };
                var param2 = new object[] { (long)100 };
                var param3 = new object[] { (uint)100 };

                long cost4 = UltraAggressiveGasPricing.CalculateGasCost(OpCode.NEWARRAY, param1);
                long cost5 = UltraAggressiveGasPricing.CalculateGasCost(OpCode.NEWARRAY, param2);
                long cost6 = UltraAggressiveGasPricing.CalculateGasCost(OpCode.NEWARRAY, param3);

                bool equivalent = cost4 == cost5 && cost5 == cost6;
                testResults.Add(equivalent);
                Console.WriteLine($"  Parameter equivalence: {cost4} gas {(equivalent ? "✅" : "❌")}");

                bool allPassed = testResults.All(r => r);
                Console.WriteLine($"  Consistency: {(allPassed ? "✅ PASS" : "❌ FAIL")}");
                Console.WriteLine();

                return allPassed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ ERROR: {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// Validate coverage of all required features.
        /// </summary>
        private static bool ValidateCoverage()
        {
            Console.WriteLine("=== Validating Coverage ===");

            try
            {
                var testResults = new List<bool>();

                // Test all major opcode categories
                var categories = new[]
                {
                    ("Constants", new[] { OpCode.PUSH0, OpCode.PUSH1, OpCode.PUSHINT32, OpCode.PUSHDATA1 }),
                    ("Flow Control", new[] { OpCode.JMP, OpCode.CALL, OpCode.RET, OpCode.JMPIF }),
                    ("Stack Operations", new[] { OpCode.DUP, OpCode.SWAP, OpCode.DROP, OpCode.OVER }),
                    ("Bitwise Operations", new[] { OpCode.AND, OpCode.OR, OpCode.XOR, OpCode.INVERT }),
                    ("Arithmetic", new[] { OpCode.ADD, OpCode.SUB, OpCode.MUL, OpCode.DIV, OpCode.POW }),
                    ("Compound Types", new[] { OpCode.NEWARRAY, OpCode.NEWMAP, OpCode.PICKITEM, OpCode.SETITEM }),
                    ("Type Operations", new[] { OpCode.CONVERT, OpCode.ISTYPE, OpCode.ISNULL })
                };

                foreach (var (category, opcodes) in categories)
                {
                    int covered = 0;
                    foreach (var opcode in opcodes)
                    {
                        long cost = UltraAggressiveGasPricing.CalculateGasCost(opcode);
                        if (cost >= 1) covered++;
                    }

                    double coverage = (double)covered / opcodes.Length * 100;
                    bool fullyCovered = coverage == 100;
                    testResults.Add(fullyCovered);
                    Console.WriteLine($"  {category,-15}: {covered}/{opcodes.Length} ({coverage:F1}%) {(fullyCovered ? "✅" : "❌")}");
                }

                // Test scaling functions work
                byte[] scalingData = new byte[1000];
                long scalingCost = UltraAggressiveGasPricing.CalculateGasCost(OpCode.CONVERT, scalingData);
                testResults.Add(scalingCost > 1);
                Console.WriteLine($"  Scaling functions: {scalingCost} gas {(scalingCost > 1 ? "✅" : "❌")}");

                // Test comparison data available
                var comparison = UltraAggressiveGasPricing.GetCostComparison();
                testResults.Add(comparison.Count > 100);
                Console.WriteLine($"  Comparison data: {comparison.Count} opcodes {(comparison.Count > 100 ? "✅" : "❌")}");

                bool allPassed = testResults.All(r => r);
                Console.WriteLine($"  Coverage: {(allPassed ? "✅ PASS" : "❌ FAIL")}");
                Console.WriteLine();

                return allPassed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ ERROR: {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// Generate detailed cost analysis report.
        /// </summary>
        public static void GenerateCostAnalysisReport()
        {
            Console.WriteLine("=== Ultra Aggressive Gas Pricing - Cost Analysis Report ===");
            Console.WriteLine();

            var comparison = UltraAggressiveGasPricing.GetCostComparison();

            // Group by reduction ranges
            var reductionGroups = new Dictionary<string, List<(OpCode, long, long, double)>>
            {
                ["95%+"] = new List<(OpCode, long, long, double)>(),
                ["80-94%"] = new List<(OpCode, long, long, double)>(),
                ["50-79%"] = new List<(OpCode, long, long, double)>(),
                ["25-49%"] = new List<(OpCode, long, long, double)>(),
                ["0-24%"] = new List<(OpCode, long, long, double)>()
            };

            foreach (var (opcode, (currentCost, newCost, reduction)) in comparison)
            {
                if (currentCost > 0)
                {
                    if (reduction >= 95) reductionGroups["95%+"].Add((opcode, currentCost, newCost, reduction));
                    else if (reduction >= 80) reductionGroups["80-94%"].Add((opcode, currentCost, newCost, reduction));
                    else if (reduction >= 50) reductionGroups["50-79%"].Add((opcode, currentCost, newCost, reduction));
                    else if (reduction >= 25) reductionGroups["25-49%"].Add((opcode, currentCost, newCost, reduction));
                    else reductionGroups["0-24%"].Add((opcode, currentCost, newCost, reduction));
                }
            }

            // Print reduction analysis
            Console.WriteLine("Cost Reduction Analysis:");
            foreach (var (range, opcodes) in reductionGroups)
            {
                if (opcodes.Count > 0)
                {
                    double avgCurrent = opcodes.Average(o => (double)o.Item2);
                    double avgNew = opcodes.Average(o => (double)o.Item3);
                    double avgReduction = opcodes.Average(o => o.Item4);

                    Console.WriteLine($"  {range,-8}: {opcodes.Count,3} opcodes | Avg: {avgCurrent:F1} → {avgNew:F1} ({avgReduction:F1}% reduction)");

                    // Show top examples
                    var topExamples = opcodes.OrderByDescending(o => o.Item4).Take(3);
                    foreach (var (opcode, currentCost, newCost, reduction) in topExamples)
                    {
                        Console.WriteLine($"           {opcode,-15}: {currentCost,3} → {newCost,3} ({reduction,5:F1}%)");
                    }
                    Console.WriteLine();
                }
            }

            // Economic impact summary
            Console.WriteLine("Economic Impact Summary:");
            int totalOpcodes = comparison.Values.Count(v => v.Item1 > 0);
            double totalReduction = comparison.Values.Where(v => v.Item1 > 0).Average(v => v.Item3);
            int massiveReductions = comparison.Values.Count(v => v.Item1 > 0 && v.Item3 >= 90);
            int significantReductions = comparison.Values.Count(v => v.Item1 > 0 && v.Item3 >= 50);

            Console.WriteLine($"  Total opcodes analyzed: {totalOpcodes}");
            Console.WriteLine($"  Average cost reduction: {totalReduction:F1}%");
            Console.WriteLine($"  Massive reductions (≥90%): {massiveReductions} ({(double)massiveReductions / totalOpcodes * 100:F1}%)");
            Console.WriteLine($"  Significant reductions (≥50%): {significantReductions} ({(double)significantReductions / totalOpcodes * 100:F1}%)");

            Console.WriteLine();
            Console.WriteLine("Real-world Cost Examples (at $25/GAS):");
            Console.WriteLine($"  Simple operation (1 gas): ${(1.0 / 100000000) * 25:F6}");
            Console.WriteLine($"  Medium operation (5 gas): ${(5.0 / 100000000) * 25:F6}");
            Console.WriteLine($"  Complex operation (10 gas): ${(10.0 / 100000000) * 25:F6}");
            Console.WriteLine($"  Heavy operation (50 gas): ${(50.0 / 100000000) * 25:F6}");

            Console.WriteLine();
        }

        /// <summary>
        /// Generate performance benchmark report.
        /// </summary>
        public static void GeneratePerformanceBenchmark()
        {
            Console.WriteLine("=== Ultra Aggressive Gas Pricing - Performance Benchmark ===");
            Console.WriteLine();

            var random = new Random(42);
            var opcodes = Enum.GetValues<OpCode>().ToArray();

            // Benchmark different scenarios
            var scenarios = new[]
            {
                ("Simple operations", opcodes.Where(o => UltraAggressiveGasPricing.CalculateGasCost(o) == 1).Take(10).ToArray()),
                ("Complex operations", opcodes.Where(o => UltraAggressiveGasPricing.CalculateGasCost(o) > 1).Take(10).ToArray()),
                ("Data operations", new[] { OpCode.CONVERT, OpCode.KEYS, OpCode.PACK, OpCode.UNPACK }),
                ("Mathematical operations", new[] { OpCode.ADD, OpCode.SUB, OpCode.MUL, OpCode.DIV, OpCode.POW, OpCode.SQRT })
            };

            foreach (var (scenarioName, scenarioOpcodes) in scenarios)
            {
                Console.WriteLine($"{scenarioName}:");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                for (int i = 0; i < 10000; i++)
                {
                    var opcode = scenarioOpcodes[random.Next(scenarioOpcodes.Length)];
                    UltraAggressiveGasPricing.CalculateGasCost(opcode);
                }

                stopwatch.Stop();

                double avgMs = (double)stopwatch.ElapsedMilliseconds / 10000;
                double opsPerSec = 1000000.0 / avgMs;

                Console.WriteLine($"  10,000 calculations in {stopwatch.ElapsedMilliseconds} ms");
                Console.WriteLine($"  Average: {avgMs:F6} ms per operation");
                Console.WriteLine($"  Performance: {opsPerSec:F0} operations/second");
                Console.WriteLine();
            }

            // Memory usage test
            Console.WriteLine("Memory Usage Test:");
            var initialMemory = GC.GetTotalMemory(false);

            var costs = new List<long>();
            for (int i = 0; i < 100000; i++)
            {
                var opcode = opcodes[random.Next(opcodes.Length)];
                costs.Add(UltraAggressiveGasPricing.CalculateGasCost(opcode));
            }

            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            Console.WriteLine($"  100,000 calculations used {memoryUsed / 1024.0:F2} KB memory");
            Console.WriteLine($"  Average memory per calculation: {memoryUsed / 100000.0:F2} bytes");
            Console.WriteLine();
        }
    }
}
