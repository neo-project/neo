// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Logs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace Neo.Tests
{
    [TestClass]
    public class UT_Logs
    {
        private static readonly string LogPath = Path.Combine(Environment.CurrentDirectory, "Logs");

        private static ILogger TestLoggerFactory(string source)
        {
            return new LoggerConfiguration()
                .WriteTo.File(
                    path: Path.Combine(LogPath, source, "log-.txt"),
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100 MiB
                    rollOnFileSizeLimit: true,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30 // about 1 month
                ).CreateLogger();
        }

        [TestMethod]
        public void TestGetLogger()
        {
            var logger = Logs.GetLogger("test");
            Assert.IsNotNull(logger);
            logger.Information("test");


            Logs.LoggerFactory = TestLoggerFactory;
            logger = Logs.GetLogger("test");
            Assert.IsNotNull(logger);
            logger.Information("test");

            var fileName = $"log-{DateTime.Now:yyyyMMdd}.txt";
            Assert.IsTrue(File.Exists(Path.Combine(LogPath, "test", fileName)));

            (logger as IDisposable)?.Dispose();
            Logs.LoggerFactory = null;
            Directory.Delete(LogPath, true);
        }

        [TestMethod]
        public void TestToLogEventLevel()
        {
            Assert.AreEqual(Logs.LogActor.ToLogEventLevel(Akka.Event.LogLevel.DebugLevel), LogEventLevel.Debug);
            Assert.AreEqual(Logs.LogActor.ToLogEventLevel(Akka.Event.LogLevel.InfoLevel), LogEventLevel.Information);
            Assert.AreEqual(Logs.LogActor.ToLogEventLevel(Akka.Event.LogLevel.WarningLevel), LogEventLevel.Warning);
            Assert.AreEqual(Logs.LogActor.ToLogEventLevel(Akka.Event.LogLevel.ErrorLevel), LogEventLevel.Error);
            Assert.AreEqual(Logs.LogActor.ToLogEventLevel(Akka.Event.LogLevel.ErrorLevel + 1), LogEventLevel.Information);
        }
    }
}
