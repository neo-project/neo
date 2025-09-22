// Copyright (C) 2015-2025 The Neo Project.
//
// ManualSuiteRunner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using System.Reflection;

namespace Neo.VM.Benchmark.Infrastructure
{
    internal static class ManualSuiteRunner
    {
        public static void RunAll()
        {
            RunSuite(new Syscalls.SyscallSuite());
            RunSuite(new Native.NativeSuite());
            RunSuite(new OpCode.OpcodeSuite());
        }

        private static void RunSuite<TSuite>(TSuite suite) where TSuite : VmBenchmarkSuite
        {
            var type = suite.GetType();
            InvokeLifecycle(type, suite, typeof(GlobalSetupAttribute));

            foreach (var vmCase in suite.Cases())
            {
                suite.Case = vmCase;
                InvokeLifecycle(type, suite, typeof(IterationSetupAttribute));
                suite.Baseline();
                InvokeLifecycle(type, suite, typeof(IterationSetupAttribute));
                suite.Single();
                InvokeLifecycle(type, suite, typeof(IterationSetupAttribute));
                suite.Saturated();
            }

            InvokeLifecycle(type, suite, typeof(GlobalCleanupAttribute));
        }

        private static void InvokeLifecycle(Type suiteType, object instance, Type attributeType)
        {
            var methods = suiteType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(attributeType, inherit: false).Any());
            foreach (var method in methods)
            {
                method.Invoke(instance, null);
            }
        }
    }
}
