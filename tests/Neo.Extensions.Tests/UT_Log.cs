// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Log.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Tests
{
    [TestClass]
    public class UT_Log
    {
        [TestMethod]
        public void TestGetLogger()
        {
            var logger = Log.GetLogger("test");
            Assert.IsNotNull(logger);
            logger.Information("test");

            var logDir = Log.LogDirectory;
            Assert.IsNotNull(logDir);
        }

        [TestMethod]
        public void TestConsoleLogger()
        {
            var logger = Log.ConsoleLogger;
            Assert.IsNotNull(logger);

            logger.Information("test");
        }
    }
}
