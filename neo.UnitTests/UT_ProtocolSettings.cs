using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ProtocolSettings
    {
        const uint mainNetMagic = 0x746E41u;

        // since ProtocolSettings.Default is designed to be writable only once, use reflection to 
        // reset the underlying _default field to null before and after running tests in this class.
        static void ResetProtocolSettings()
        {
            var defaultField = typeof(ProtocolSettings)
                .GetField("_default", BindingFlags.Static | BindingFlags.NonPublic);
            defaultField.SetValue(null, null);
        }

        [TestInitialize]
        public void Initialize()
        {
            ResetProtocolSettings();
        }

        [TestCleanup]
        public void Cleanup()
        {
            ResetProtocolSettings();
        }

        [TestMethod]
        public void Default_Magic_should_be_mainnet_Magic_value()
        {
            ProtocolSettings.Default.Magic.Should().Be(mainNetMagic);
        }

        [TestMethod]
        public void Can_initialize_ProtocolSettings()
        {
            var expectedMagic = 12345u;

            var dict = new Dictionary<string, string>()
            {
                { "ProtocolConfiguration:Magic", $"{expectedMagic}" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            ProtocolSettings.Initialize(config).Should().BeTrue();
            ProtocolSettings.Default.Magic.Should().Be(expectedMagic);
        }

        [TestMethod]
        public void Cant_initialize_ProtocolSettings_after_default_settings_used()
        {
            ProtocolSettings.Default.Magic.Should().Be(mainNetMagic);

            var updatedMagic = 54321u;
            var dict = new Dictionary<string, string>()
            {
                { "ProtocolConfiguration:Magic", $"{updatedMagic}" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            ProtocolSettings.Initialize(config).Should().BeFalse();
            ProtocolSettings.Default.Magic.Should().Be(mainNetMagic);
        }

        [TestMethod]
        public void Cant_initialize_ProtocolSettings_twice()
        {
            var expectedMagic = 12345u;
            var dict = new Dictionary<string, string>()
            {
                { "ProtocolConfiguration:Magic", $"{expectedMagic}" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            ProtocolSettings.Initialize(config).Should().BeTrue();

            var updatedMagic = 54321u;
            dict = new Dictionary<string, string>()
            {
                { "ProtocolConfiguration:Magic", $"{updatedMagic}" }
            };
            config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            ProtocolSettings.Initialize(config).Should().BeFalse();
            ProtocolSettings.Default.Magic.Should().Be(expectedMagic);
        }
    }
}
