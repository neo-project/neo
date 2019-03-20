using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
//using Settings = Neo.Plugins.Settings;
using ProtocolSettings = Neo.ProtocolSettings;

namespace Neo.UnitTests_fast
{
    [TestClass]
    public class UT_ProtocolSettings
    {
        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestMethod]
        public void Test_15_Seconds()
        {
            ProtocolSettings.Default.SecondsPerBlock.Should().Be(15);
        }
    }
}
