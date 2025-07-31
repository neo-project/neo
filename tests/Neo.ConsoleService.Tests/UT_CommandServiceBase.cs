// Copyright (C) 2015-2025 The Neo Project.
//
// UT_CommandServiceBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Neo.ConsoleService.Tests
{
    [TestClass]
    public class UT_CommandServiceBase
    {
        private class TestConsoleService : ConsoleServiceBase
        {
            public override string ServiceName => "TestService";
            public bool _asyncTestCalled = false;

            // Test method with various parameter types
            [ConsoleCommand("test", Category = "Test Commands")]
            public void TestMethod(string strParam, uint intParam, bool boolParam, string optionalParam = "default") { }

            // Test method with enum parameter
            [ConsoleCommand("testenum", Category = "Test Commands")]
            public void TestEnumMethod(TestEnum enumParam) { }

            [ConsoleCommand("testversion", Category = "Test Commands")]
            public Version TestMethodVersion() { return new Version("1.0.0"); }

            [ConsoleCommand("testambiguous", Category = "Test Commands")]
            public void TestAmbiguousFirst() { }

            [ConsoleCommand("testambiguous", Category = "Test Commands")]
            public void TestAmbiguousSecond() { }

            [ConsoleCommand("testcrash", Category = "Test Commands")]
            public void TestCrashMethod(uint number) { }

            [ConsoleCommand("testasync", Category = "Test Commands")]
            public async Task TestAsyncCommand()
            {
                await Task.Delay(100);
                _asyncTestCalled = true;
            }

            public enum TestEnum { Value1, Value2, Value3 }
        }

        [TestMethod]
        public void TestParseIndicatorArguments()
        {
            var service = new TestConsoleService();
            var method = typeof(TestConsoleService).GetMethod("TestMethod");

            // Test case 1: Basic indicator arguments
            var args1 = "test --strParam hello --intParam 42 --boolParam".Tokenize();
            Assert.AreEqual(11, args1.Count);
            Assert.AreEqual("test", args1[0].Value);
            Assert.AreEqual("--strParam", args1[2].Value);
            Assert.AreEqual("hello", args1[4].Value);
            Assert.AreEqual("--intParam", args1[6].Value);
            Assert.AreEqual("42", args1[8].Value);
            Assert.AreEqual("--boolParam", args1[10].Value);

            var result1 = service.ParseIndicatorArguments(method, args1[1..]);
            Assert.AreEqual(4, result1.Length);
            Assert.AreEqual("hello", result1[0]);
            Assert.AreEqual(42u, result1[1]);
            Assert.AreEqual(true, result1[2]);
            Assert.AreEqual("default", result1[3]); // Default value

            // Test case 2: Boolean parameter without value
            var args2 = "test --boolParam".Tokenize();
            Assert.ThrowsExactly<ArgumentException>(() => service.ParseIndicatorArguments(method, args2[1..]));

            // Test case 3: Enum parameter
            var enumMethod = typeof(TestConsoleService).GetMethod("TestEnumMethod");
            var args3 = "testenum --enumParam Value2".Tokenize();
            var result3 = service.ParseIndicatorArguments(enumMethod, args3[1..]);
            Assert.AreEqual(1, result3.Length);
            Assert.AreEqual(TestConsoleService.TestEnum.Value2, result3[0]);

            // Test case 4: Unknown parameter should throw exception
            var args4 = "test --unknownParam value".Tokenize();
            Assert.ThrowsExactly<ArgumentException>(() => service.ParseIndicatorArguments(method, args4[1..]));

            // Test case 5: Missing value for non-boolean parameter should throw exception
            var args5 = "test --strParam".Tokenize();
            Assert.ThrowsExactly<ArgumentException>(() => service.ParseIndicatorArguments(method, args5[1..]));
        }

        [TestMethod]
        public void TestParseSequentialArguments()
        {
            var service = new TestConsoleService();
            var method = typeof(TestConsoleService).GetMethod("TestMethod");

            // Test case 1: All parameters provided
            var args1 = "test hello 42 true custom".Tokenize();
            var result1 = service.ParseSequentialArguments(method, args1[1..]);
            Assert.AreEqual(4, result1.Length);
            Assert.AreEqual("hello", result1[0]);
            Assert.AreEqual(42u, result1[1]);
            Assert.AreEqual(true, result1[2]);
            Assert.AreEqual("custom", result1[3]);

            // Test case 2: Some parameters with default values
            var args2 = "test hello 42 true".Tokenize();
            var result2 = service.ParseSequentialArguments(method, args2[1..]);
            Assert.AreEqual(4, result2.Length);
            Assert.AreEqual("hello", result2[0]);
            Assert.AreEqual(42u, result2[1]);
            Assert.AreEqual(true, result2[2]);
            Assert.AreEqual("default", result2[3]); // optionalParam default value

            // Test case 3: Enum parameter
            var enumMethod = typeof(TestConsoleService).GetMethod("TestEnumMethod");
            var args3 = "testenum Value1".Tokenize();
            var result3 = service.ParseSequentialArguments(enumMethod, args3[1..].Trim());
            Assert.AreEqual(1, result3.Length);
            Assert.AreEqual(TestConsoleService.TestEnum.Value1, result3[0]);

            // Test case 4: Missing required parameter should throw exception
            var args4 = "test hello".Tokenize();
            Assert.ThrowsExactly<ArgumentException>(() => service.ParseSequentialArguments(method, args4[1..].Trim()));

            // Test case 5: Empty arguments should use all default values
            var args5 = new List<CommandToken>();
            Assert.ThrowsExactly<ArgumentException>(() => service.ParseSequentialArguments(method, args5.Trim()));
        }

        [TestMethod]
        public void TestOnCommand()
        {
            var service = new TestConsoleService();
            service.RegisterCommand(service, "TestConsoleService");

            // Test case 1: Missing command
            var resultEmptyCommand = service.OnCommand("");
            Assert.IsTrue(resultEmptyCommand);

            // Test case 2: White space command
            var resultWhiteSpaceCommand = service.OnCommand(" ");
            Assert.IsTrue(resultWhiteSpaceCommand);

            // Test case 3: Not exist command
            var resultNotExistCommand = service.OnCommand("notexist");
            Assert.IsFalse(resultNotExistCommand);

            // Test case 4: Exists command test
            var resultTestCommand = service.OnCommand("testversion");
            Assert.IsTrue(resultTestCommand);

            // Test case 5: Exists command with quote
            var resultTestCommandWithQuote = service.OnCommand("testversion --noargs");
            Assert.IsTrue(resultTestCommandWithQuote);

            // Test case 6: Ambiguous command tst
            var ex = Assert.ThrowsExactly<ArgumentException>(() => service.OnCommand("testambiguous"));
            Assert.Contains("Ambiguous calls for", ex.Message);

            // Test case 7: Help test
            var resultTestHelp = service.OnCommand("testcrash notANumber");
            Assert.IsTrue(resultTestHelp);

            // Test case 8: Test Task
            var resultTestTaskAsync = service.OnCommand("testasync");
            Assert.IsTrue(resultTestTaskAsync);
            Assert.IsTrue(service._asyncTestCalled);
        }
    }
}
