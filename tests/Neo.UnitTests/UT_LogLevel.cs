// Copyright (C) 2015-2026 The Neo Project.
//
// UT_LogLevel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_LogLevel
    {
        [TestMethod]
        public void Values_MatchSerilogLevels()
        {
            Assert.AreEqual((byte)LogEventLevel.Debug, (byte)LogLevel.Debug);
            Assert.AreEqual((byte)LogEventLevel.Information, (byte)LogLevel.Info);
            Assert.AreEqual((byte)LogEventLevel.Warning, (byte)LogLevel.Warning);
            Assert.AreEqual((byte)LogEventLevel.Error, (byte)LogLevel.Error);
            Assert.AreEqual((byte)LogEventLevel.Fatal, (byte)LogLevel.Fatal);
        }

        [TestMethod]
        public void Ordering_IsIncreasing()
        {
            Assert.IsTrue((byte)LogLevel.Debug < (byte)LogLevel.Info);
            Assert.IsTrue((byte)LogLevel.Info < (byte)LogLevel.Warning);
            Assert.IsTrue((byte)LogLevel.Warning < (byte)LogLevel.Error);
            Assert.IsTrue((byte)LogLevel.Error < (byte)LogLevel.Fatal);
        }
    }
}
