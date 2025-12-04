// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Logs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;

namespace Neo.Tests
{
    [TestClass]
    public class UT_Logs
    {
        [TestMethod]
        public void TestGetLogger()
        {
            var logger = Logs.GetLogger("test");
            Assert.IsNotNull(logger);
            logger.Information("test");

            var logDir = Logs.LogDirectory;
            Assert.IsNull(logDir);

            Logs.LogDirectory = Path.Combine(Environment.CurrentDirectory, "Logs");
            logger = Logs.GetLogger("test");
            Assert.IsNotNull(logger);
            logger.Information("test");

            var fileName = $"log-{DateTime.Now:yyyyMMdd}.txt";
            Assert.IsTrue(File.Exists(Path.Combine(Logs.LogDirectory, "test", fileName)));
            Assert.ThrowsExactly<InvalidOperationException>(() => Logs.LogDirectory = "test");
        }

        [TestMethod]
        public void TestConsoleLogger()
        {
            var logger = Logs.ConsoleLogger;
            Assert.IsNotNull(logger);

            logger.Information("test");
        }
    }
}
