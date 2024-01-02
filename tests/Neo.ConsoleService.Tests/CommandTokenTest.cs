// Copyright (C) 2015-2024 The Neo Project.
//
// CommandTokenTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Neo.ConsoleService.Tests
{
    [TestClass]
    public class CommandTokenTest
    {
        [TestMethod]
        public void Test1()
        {
            var cmd = " ";
            var args = CommandToken.Parse(cmd).ToArray();

            AreEqual(args, new CommandSpaceToken(0, 1));
            Assert.AreEqual(cmd, CommandToken.ToString(args));
        }

        [TestMethod]
        public void Test2()
        {
            var cmd = "show  state";
            var args = CommandToken.Parse(cmd).ToArray();

            AreEqual(args, new CommandStringToken(0, "show"), new CommandSpaceToken(4, 2), new CommandStringToken(6, "state"));
            Assert.AreEqual(cmd, CommandToken.ToString(args));
        }

        [TestMethod]
        public void Test3()
        {
            var cmd = "show \"hello world\"";
            var args = CommandToken.Parse(cmd).ToArray();

            AreEqual(args,
                new CommandStringToken(0, "show"),
                new CommandSpaceToken(4, 1),
                new CommandQuoteToken(5, '"'),
                new CommandStringToken(6, "hello world"),
                new CommandQuoteToken(17, '"')
                );
            Assert.AreEqual(cmd, CommandToken.ToString(args));
        }

        [TestMethod]
        public void Test4()
        {
            var cmd = "show \"'\"";
            var args = CommandToken.Parse(cmd).ToArray();

            AreEqual(args,
                new CommandStringToken(0, "show"),
                new CommandSpaceToken(4, 1),
                new CommandQuoteToken(5, '"'),
                new CommandStringToken(6, "'"),
                new CommandQuoteToken(7, '"')
                );
            Assert.AreEqual(cmd, CommandToken.ToString(args));
        }

        [TestMethod]
        public void Test5()
        {
            var cmd = "show \"123\\\"456\"";
            var args = CommandToken.Parse(cmd).ToArray();

            AreEqual(args,
                new CommandStringToken(0, "show"),
                new CommandSpaceToken(4, 1),
                new CommandQuoteToken(5, '"'),
                new CommandStringToken(6, "123\\\"456"),
                new CommandQuoteToken(14, '"')
                );
            Assert.AreEqual(cmd, CommandToken.ToString(args));
        }

        private void AreEqual(CommandToken[] args, params CommandToken[] compare)
        {
            Assert.AreEqual(compare.Length, args.Length);

            for (int x = 0; x < args.Length; x++)
            {
                var a = args[x];
                var b = compare[x];

                Assert.AreEqual(a.Type, b.Type);
                Assert.AreEqual(a.Value, b.Value);
                Assert.AreEqual(a.Offset, b.Offset);
            }
        }
    }
}
