// Copyright (C) 2015-2025 The Neo Project.
//
// RobustnessTestRunner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Framework;
using Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Scenarios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neo.Plugins.DBFTPlugin.Tests.ChaosTests
{
    /// <summary>
    /// Helper class to run comprehensive robustness validation suites
    /// </summary>
    public static class RobustnessTestRunner
    {
        /// <summary>
        /// Run basic robustness validation suite
        /// </summary>
        public static void RunBasicRobustnessTests()
        {
            var testResults = new List<(string testName, bool passed, string details)>();

            Console.WriteLine("=== DBFT BASIC ROBUSTNESS VALIDATION ===");
            Console.WriteLine($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();

            var testMethods = new[]
            {
                nameof(UT_DBFTRobustnessTests.Test_ChaosFrameworkInitialization),
                nameof(UT_DBFTRobustnessTests.Test_FaultTolerance_SingleNodeFailure),
                nameof(UT_DBFTRobustnessTests.Test_MessageLoss_ModerateLevel),
                nameof(UT_DBFTRobustnessTests.Test_ViewChange_UnresponsivePrimary)
            };

            foreach (var testMethod in testMethods)
            {
                var result = RunSingleTest(testMethod);
                testResults.Add(result);

                Console.WriteLine($"[{(result.passed ? "PASS" : "FAIL")}] {result.testName}");
                if (!result.passed)
                {
                    Console.WriteLine($"  Details: {result.details}");
                }
            }

            PrintSummary(testResults);
        }

        /// <summary>
        /// Run extended robustness validation with higher chaos levels
        /// </summary>
        public static void RunExtendedRobustnessTests()
        {
            var testResults = new List<(string testName, bool passed, string details)>();

            Console.WriteLine("=== DBFT EXTENDED ROBUSTNESS VALIDATION ===");
            Console.WriteLine($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();

            var testMethods = new[]
            {
                nameof(UT_DBFTRobustnessTests.Test_FaultTolerance_MaximumTolerableFailures),
                nameof(UT_DBFTRobustnessTests.Test_NetworkPartition_MajorityCanContinue),
                nameof(UT_DBFTRobustnessTests.Test_ByzantineNode_MaximumTolerable),
                nameof(UT_DBFTRobustnessTests.Test_MessageLoss_HighLevel),
                nameof(UT_DBFTRobustnessTests.Test_CombinedChaos_RealWorldScenario)
            };

            foreach (var testMethod in testMethods)
            {
                var result = RunSingleTest(testMethod);
                testResults.Add(result);

                Console.WriteLine($"[{(result.passed ? "PASS" : "FAIL")}] {result.testName}");
                if (!result.passed)
                {
                    Console.WriteLine($"  Details: {result.details}");
                }
            }

            PrintSummary(testResults);
        }

        /// <summary>
        /// Run Byzantine attack simulation tests
        /// </summary>
        public static void RunByzantineAttackTests()
        {
            var testResults = new List<(string testName, bool passed, string details)>();

            Console.WriteLine("=== BYZANTINE ATTACK SIMULATION ===");
            Console.WriteLine($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();

            var testMethods = new[]
            {
                nameof(UT_DBFTRobustnessTests.Test_ByzantineNode_SingleMalicious),
                nameof(UT_DBFTRobustnessTests.Test_ByzantineNode_MaximumTolerable),
                nameof(UT_DBFTRobustnessTests.Test_TimingAttack_DelayedMessages)
            };

            foreach (var testMethod in testMethods)
            {
                var result = RunSingleTest(testMethod);
                testResults.Add(result);

                Console.WriteLine($"[{(result.passed ? "PASS" : "FAIL")}] {result.testName}");
                if (!result.passed)
                {
                    Console.WriteLine($"  Details: {result.details}");
                }
            }

            PrintSummary(testResults);
        }

        /// <summary>
        /// Run network partition tests
        /// </summary>
        public static void RunNetworkPartitionTests()
        {
            var testResults = new List<(string testName, bool passed, string details)>();

            Console.WriteLine("=== NETWORK PARTITION RESILIENCE ===");
            Console.WriteLine($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();

            var testMethods = new[]
            {
                nameof(UT_DBFTRobustnessTests.Test_NetworkPartition_MajorityCanContinue),
                nameof(UT_DBFTRobustnessTests.Test_NetworkPartition_NoMajority_ShouldStall)
            };

            foreach (var testMethod in testMethods)
            {
                var result = RunSingleTest(testMethod);
                testResults.Add(result);

                Console.WriteLine($"[{(result.passed ? "PASS" : "FAIL")}] {result.testName}");
                if (!result.passed)
                {
                    Console.WriteLine($"  Details: {result.details}");
                }
            }

            PrintSummary(testResults);
        }

        private static (string testName, bool passed, string details) RunSingleTest(string testMethodName)
        {
            try
            {
                var testClass = new UT_DBFTRobustnessTests();

                // Initialize the test
                testClass.TestSetup();

                try
                {
                    // Get the test method and invoke it
                    var method = typeof(UT_DBFTRobustnessTests).GetMethod(testMethodName);
                    if (method == null)
                    {
                        return (testMethodName, false, "Test method not found");
                    }

                    method.Invoke(testClass, null);
                    return (testMethodName, true, "Test passed successfully");
                }
                finally
                {
                    // Cleanup the test
                    testClass.TestCleanup();
                }
            }
            catch (Exception ex)
            {
                var details = ex.InnerException?.Message ?? ex.Message;
                return (testMethodName, false, details);
            }
        }

        private static void PrintSummary(List<(string testName, bool passed, string details)> results)
        {
            Console.WriteLine();
            Console.WriteLine("=== TEST SUMMARY ===");

            var passed = results.Count(r => r.passed);
            var total = results.Count;
            var successRate = (double)passed / total;

            Console.WriteLine($"Tests Run: {total}");
            Console.WriteLine($"Passed: {passed}");
            Console.WriteLine($"Failed: {total - passed}");
            Console.WriteLine($"Success Rate: {successRate:P1}");

            if (successRate >= 0.8)
            {
                Console.WriteLine("✅ ROBUST - DBFT consensus shows strong resilience");
            }
            else if (successRate >= 0.6)
            {
                Console.WriteLine("⚠️  MODERATE - Some robustness concerns detected");
            }
            else
            {
                Console.WriteLine("❌ WEAK - Significant robustness issues found");
            }

            Console.WriteLine($"Completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();
        }

        /// <summary>
        /// Configuration for custom robustness testing
        /// </summary>
        public static void ConfigureCustomTest(
            double messageLossRate = 0.1,
            int maxLatencyMs = 1000,
            double nodeFailureRate = 0.05,
            double byzantineRate = 0.02)
        {
            Environment.SetEnvironmentVariable("CHAOS_MESSAGE_LOSS", messageLossRate.ToString());
            Environment.SetEnvironmentVariable("CHAOS_MAX_LATENCY", maxLatencyMs.ToString());
            Environment.SetEnvironmentVariable("CHAOS_NODE_FAILURE", nodeFailureRate.ToString());
            Environment.SetEnvironmentVariable("CHAOS_BYZANTINE", byzantineRate.ToString());

            Console.WriteLine("Custom chaos configuration applied:");
            Console.WriteLine($"  Message Loss Rate: {messageLossRate:P1}");
            Console.WriteLine($"  Max Latency: {maxLatencyMs}ms");
            Console.WriteLine($"  Node Failure Rate: {nodeFailureRate:P1}");
            Console.WriteLine($"  Byzantine Rate: {byzantineRate:P1}");
            Console.WriteLine();
        }
    }
}
