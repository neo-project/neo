// Copyright (C) 2015-2025 The Neo Project.
//
// CommandTokenizerTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neo.ConsoleService.Tests
{
    [TestClass]
    public class CommandTokenizerTest
    {
        [TestMethod]
        public void Test1()
        {
            var cmd = " ";
            var args = cmd.Tokenize();
            Assert.AreEqual(1, args.Count);
            Assert.AreEqual(" ", args[0].Value);
        }

        [TestMethod]
        public void Test2()
        {
            var cmd = "show  state";
            var args = cmd.Tokenize();
            Assert.AreEqual(3, args.Count);
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
            Assert.AreEqual(3, args.Count);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("hello world", args[2].Value);
        }

        [TestMethod]
        public void Test4()
        {
            var cmd = "show \"'\"";
            var args = cmd.Tokenize();
            Assert.AreEqual(3, args.Count);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("'", args[2].Value);
        }

        [TestMethod]
        public void Test5()
        {
            var cmd = "show \"123\\\"456\""; // Double quote because it is quoted twice in code and command.
            var args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(3, args.Count);
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
            Assert.AreEqual(3, args.Count);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("x1,x2,x3", args[2].Value);
            Assert.AreEqual("'x1,x2,x3'", args[2].RawValue);

            cmd = "show '\\n \\r \\t \\''"; // Double quote because it is quoted twice in code and command.
            args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(3, args.Count);
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
            Assert.AreEqual(7, args.Count);
            Assert.AreEqual("invoke", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5", args[2].Value);
            Assert.AreEqual(" ", args[3].Value);
            Assert.AreEqual("balanceOf", args[4].Value);
            Assert.AreEqual(" ", args[5].Value);
            Assert.AreEqual(args[6].Value, json);

            cmd = "show x'y'";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(3, args.Count);
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
            Assert.AreEqual(3, args.Count);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("x", args[2].Value);
            Assert.AreEqual("`x`", args[2].RawValue);

            cmd = "show `{\"a\": \"b\"}`";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(3, args.Count);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("{\"a\": \"b\"}", args[2].Value);
            Assert.AreEqual("`{\"a\": \"b\"}`", args[2].RawValue);

            cmd = "show `123\"456`"; // Donot quoted twice if the input uses backquote.
            args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(3, args.Count);
            Assert.AreEqual("show", args[0].Value);
            Assert.AreEqual(" ", args[1].Value);
            Assert.AreEqual("123\"456", args[2].Value);
            Assert.AreEqual("`123\"456`", args[2].RawValue);
        }
    }
}
