// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ITestOutputHelperExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Util;
using Neo;
using Neo.Hosting;
using Neo.Hosting.App;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Tests;
using Neo.Hosting.App.Tests.UTHelpers;
using Neo.Hosting.App.Tests.UTHelpers.Extensions;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.UTHelpers.Extensions
{
    internal static class UT_ITestOutputHelperExtensions
    {
        public static void LogDebug<T>(this ITestOutputHelper testOutputHelper, string className, string methodName, T actual, T expected)
        {
            testOutputHelper.WriteLine(nameof(Debug).PadCenter(17, '-'));
            testOutputHelper.WriteLine($"    Class: {className}");
            testOutputHelper.WriteLine($"   Method: {methodName}");

            testOutputHelper.WriteLine(nameof(Result).PadCenter(17, '-'));
            testOutputHelper.WriteLine($"   Actual: {actual}");
            testOutputHelper.WriteLine($" Expected: {expected}");
            testOutputHelper.WriteLine($"-----------------");
        }

        public static void LogDebug<T>(this ITestOutputHelper testOutputHelper, string className, string methodName, T[] actual, T[] expected)
        {
            testOutputHelper.WriteLine(nameof(Debug).PadCenter(17, '-'));
            testOutputHelper.WriteLine($"    Class: {className}");
            testOutputHelper.WriteLine($"   Method: {methodName}");

            testOutputHelper.WriteLine(nameof(Result).PadCenter(17, '-'));
            testOutputHelper.WriteLine($"   Actual: [{string.Join(", ", actual)}]");
            testOutputHelper.WriteLine($" Expected: [{string.Join(", ", expected)}]");
            testOutputHelper.WriteLine($"-----------------");
        }
    }
}
