// Copyright (C) 2015-2025 The Neo Project.
//
// UT_CommandTokenizer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.ConsoleService.Tests
{
    [TestClass]
    public class UT_CommandTokenizer
    {
        [TestMethod]
        public void Test1()
        {
            var cmd = " ";
            var args = cmd.Tokenize();
            Assert.HasCount(1, args);
            Assert.AreEqual(" ", args[0].Value);
        }

        [TestMethod]
        public void Test2()
        {
            var cmd = "show  state";
            var args = cmd.Tokenize();
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual("  ", args[1].Value);
            Assert.AreEqual("state", args[2].Value);
            Assert.AreEqual(cmd, args.JoinRaw());
        }

        [TestMethod]
        public void Test3()
        {
            var cmd = "show \"hello world\"";
            var args = cmd.Tokenize();
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("hello world", args[2].Value);
        }

        [TestMethod]
        public void Test4()
        {
            var cmd = "show \"'\"";
            var args = cmd.Tokenize();
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("'", args[2].Value);
        }

        [TestMethod]
        public void Test5()
        {
            var cmd = "show \"123\\\"456\""; // Double quote because it is quoted twice in code and command.
            var args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("123\"456", args[2].Value);
            Assert.AreEqual("\"123\"456\"", args[2].RawValue);
        }

        [TestMethod]
        public void TestMore()
        {
            var cmd = "show 'x1,x2,x3'";
            var args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("x1,x2,x3", args[2].Value);
            Assert.AreEqual("'x1,x2,x3'", args[2].RawValue);

            cmd = "show '\\n \\r \\t \\''"; // Double quote because it is quoted twice in code and command.
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("\n \r \t \'", args[2].Value);
            Assert.AreEqual("show", args[0].RawValue);
            Assert.AreEqual(" ", args[1].RawValue);
            Assert.AreEqual("'\n \r \t \''", args[2].RawValue);
            Assert.AreEqual("show '\n \r \t \''", args.JoinRaw());

            var json = "[{\"type\":\"Hash160\",\"value\":\"0x0010922195a6c7cab3233f923716ad8e2dd63f8a\"}]";
            cmd = "invoke 0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5 balanceOf " + json;
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(7, args);
            Assert.AreEqual("invoke", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5", args[2].Value);
            Assert.AreEqual(" ", args[3].Value);
            Assert.AreEqual("balanceOf", args[4].Value);
            Assert.AreEqual(" ", args[5].Value);
            Assert.AreEqual(args[6].Value, json);

            cmd = "show x'y'";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("x'y'", args[2].Value);
            Assert.AreEqual("x'y'", args[2].RawValue);
        }

        [TestMethod]
        public void TestBackQuote()
        {
            var cmd = "show `x`";
            var args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("x", args[2].Value);
            Assert.AreEqual("`x`", args[2].RawValue);

            cmd = "show `{\"a\": \"b\"}`";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("{\"a\": \"b\"}", args[2].Value);
            Assert.AreEqual("`{\"a\": \"b\"}`", args[2].RawValue);

            cmd = "show `123\"456`"; // Donot quoted twice if the input uses backquote.
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("123\"456", args[2].Value);
            Assert.AreEqual("`123\"456`", args[2].RawValue);
        }

        [TestMethod]
        public void TestUnicodeEscape()
        {
            // Test basic Unicode escape sequence
            var cmd = "show \"\\u0041\""; // Should decode to 'A'
            var args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("A", args[2].Value);

            // Test Unicode escape sequence for emoji
            cmd = "show \"\\uD83D\\uDE00\""; // Should decode to 😀
            args = CommandTokenizer.Tokenize(cmd); // surrogate pairs
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("😀", args[2].Value);

            // Test Unicode escape sequence in single quotes
            cmd = "show '\\u0048\\u0065\\u006C\\u006C\\u006F'"; // Should decode to "Hello"
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("Hello", args[2].Value);

            cmd = "show '\\x48\\x65\\x6C\\x6C\\x6F'"; // Should decode to "Hello"
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("Hello", args[2].Value);
        }

        [TestMethod]
        public void TestUnicodeEscapeErrors()
        {
            // Test incomplete Unicode escape sequence
            Assert.ThrowsExactly<ArgumentException>(() => CommandTokenizer.Tokenize("show \"\\u123\""));

            // Test invalid hex digits
            Assert.ThrowsExactly<ArgumentException>(() => CommandTokenizer.Tokenize("show \"\\u12XY\""));

            // Test Unicode escape at end of string
            Assert.ThrowsExactly<ArgumentException>(() => CommandTokenizer.Tokenize("show \"\\u"));
        }

        [TestMethod]
        public void TestUnicodeEdgeCases()
        {
            // Test surrogate pairs - high surrogate
            var cmd = "show \"\\uD83D\"";
            var args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("\uD83D", args[2].Value); // High surrogate

            // Test surrogate pairs - low surrogate
            cmd = "show \"\\uDE00\"";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("\uDE00", args[2].Value); // Low surrogate

            // Test null character
            cmd = "show \"\\u0000\"";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("\u0000", args[2].Value); // Null character

            // Test maximum Unicode value
            cmd = "show \"\\uFFFF\"";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("\uFFFF", args[2].Value); // Maximum Unicode value

            // Test multiple Unicode escapes in sequence
            cmd = "show \"\\u0048\\u0065\\u006C\\u006C\\u006F\\u0020\\u0057\\u006F\\u0072\\u006C\\u0064\"";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("Hello World", args[2].Value);

            // Test Unicode escape mixed with regular characters
            cmd = "show \"Hello\\u0020World\"";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.HasCount(3, args);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("Hello World", args[2].Value);
        }
    }
}
