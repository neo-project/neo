using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
//using Settings = Neo.Plugins.Settings;
using ProtocolSettings = Neo.ProtocolSettings;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ProtocolSettings
    {
        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestMethod]
        public void Test_2_Seconds()
        {
            ProtocolSettings.Default.SecondsPerBlock.Should().Be(2);
        }
    }
}
