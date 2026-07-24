// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Utility_Log.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Utility_Log
    {
        [TestMethod]
        public void StrictUTF8_IsUtf8WithExceptionFallback()
        {
            Assert.AreEqual(Encoding.UTF8.CodePage, Utility.StrictUTF8.CodePage);
            Assert.IsInstanceOfType(Utility.StrictUTF8.DecoderFallback, typeof(DecoderFallback));
            Assert.IsInstanceOfType(Utility.StrictUTF8.EncoderFallback, typeof(EncoderFallback));
        }

        [TestMethod]
        public void Log_RespectsMinimumLevel_And_RaisesEvent()
        {
            var previous = Utility.LogLevel;
            string receivedSource = null;
            LogLevel? receivedLevel = null;
            object receivedMessage = null;

            void Handler(string source, LogLevel level, object message)
            {
                receivedSource = source;
                receivedLevel = level;
                receivedMessage = message;
            }

            try
            {
                Utility.LogLevel = LogLevel.Warning;
                Utility.Logging += Handler;

#pragma warning disable CS0618 // Testing obsolete Utility.Log coverage
                Utility.Log("src", LogLevel.Info, "skipped");
                Assert.IsNull(receivedSource);

                Utility.Log("src", LogLevel.Error, "boom");
#pragma warning restore CS0618
                Assert.AreEqual("src", receivedSource);
                Assert.AreEqual(LogLevel.Error, receivedLevel);
                Assert.AreEqual("boom", receivedMessage);
            }
            finally
            {
                Utility.Logging -= Handler;
                Utility.LogLevel = previous;
            }
        }
    }
}
