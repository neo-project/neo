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
            Assert.AreEqual(args.Count, 1);
            Assert.AreEqual(args[0].Value, " ");
        }

        [TestMethod]
        public void Test2()
        {
            var cmd = "show  state";
            var args = cmd.Tokenize();
            Assert.AreEqual(args.Count, 3);
            Assert.AreEqual(args[0].Value, "show");
            Assert.AreEqual(args[1].Value, "  ");
            Assert.AreEqual(args[2].Value, "state");
            Assert.AreEqual(cmd, args.JoinRaw());
        }

        [TestMethod]
        public void Test3()
        {
            var cmd = "show \"hello world\"";
            var args = cmd.Tokenize();
            Assert.AreEqual(args.Count, 3);
            Assert.AreEqual(args[0].Value, "show");
            Assert.AreEqual(args[1].Value, " ");
            Assert.AreEqual(args[2].Value, "hello world");
        }

        [TestMethod]
        public void Test4()
        {
            var cmd = "show \"'\"";
            var args = cmd.Tokenize();
            Assert.AreEqual(args.Count, 3);
            Assert.AreEqual(args[0].Value, "show");
            Assert.AreEqual(args[1].Value, " ");
            Assert.AreEqual(args[2].Value, "'");
        }

        [TestMethod]
        public void Test5()
        {
            var cmd = "show \"123\\\"456\""; // Double quote because it is quoted twice in code and command.
            var args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(args.Count, 3);
            Assert.AreEqual(args[0].Value, "show");
            Assert.AreEqual(args[1].Value, " ");
            Assert.AreEqual(args[2].Value, "123\"456");
            Assert.AreEqual(args[2].RawValue, "\"123\"456\"");
        }

        [TestMethod]
        public void TestMore()
        {
            var cmd = "show 'x1,x2,x3'";
            var args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(args.Count, 3);
            Assert.AreEqual(args[0].Value, "show");
            Assert.AreEqual(args[1].Value, " ");
            Assert.AreEqual(args[2].Value, "x1,x2,x3");
            Assert.AreEqual(args[2].RawValue, "'x1,x2,x3'");

            cmd = "show '\\n \\r \\t \\''"; // Double quote because it is quoted twice in code and command.
            args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(args.Count, 3);
            Assert.AreEqual(args[0].Value, "show");
            Assert.AreEqual(args[1].Value, " ");
            Assert.AreEqual(args[2].Value, "\n \r \t \'");
            Assert.AreEqual(args[0].RawValue, "show");
            Assert.AreEqual(args[1].RawValue, " ");
            Assert.AreEqual(args[2].RawValue, "'\n \r \t \''");
            Assert.AreEqual("show '\n \r \t \''", args.JoinRaw());

            var json = "[{\"type\":\"Hash160\",\"value\":\"0x0010922195a6c7cab3233f923716ad8e2dd63f8a\"}]";
            cmd = "invoke 0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5 balanceOf " + json;
            args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(args.Count, 7);
            Assert.AreEqual(args[0].Value, "invoke");
            Assert.AreEqual(args[1].Value, " ");
            Assert.AreEqual(args[2].Value, "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5");
            Assert.AreEqual(args[3].Value, " ");
            Assert.AreEqual(args[4].Value, "balanceOf");
            Assert.AreEqual(args[5].Value, " ");
            Assert.AreEqual(args[6].Value, json);

            cmd = "show x'y'";
            args = CommandTokenizer.Tokenize(cmd);
            Assert.AreEqual(args.Count, 3);
            Assert.AreEqual(args[0].Value, "show");
            Assert.AreEqual(args[1].Value, " ");
            Assert.AreEqual(args[2].Value, "x'y'");
            Assert.AreEqual(args[2].RawValue, "x'y'");
        }
    }
}
