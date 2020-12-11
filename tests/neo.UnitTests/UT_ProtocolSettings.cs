using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ProtocolSettings
    {
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
        public void CheckFirstLetterOfAddresses()
        {
            UInt160 min = UInt160.Parse("0x0000000000000000000000000000000000000000");
            min.ToAddress()[0].Should().Be('N');
            UInt160 max = UInt160.Parse("0xffffffffffffffffffffffffffffffffffffffff");
            max.ToAddress()[0].Should().Be('N');
        }

        [TestMethod]
        public void Default_Magic_should_be_mainnet_Magic_value()
        {
            var mainNetMagic = 0x4F454Eu;
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
        public void Initialize_ProtocolSettings_NativeBlockIndex()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, @"
{
 ""ProtocolConfiguration"":
    {
    ""NativeActivations"":{ ""test"":123 }
    }
}
");
            var config = new ConfigurationBuilder().AddJsonFile(tempFile).Build();
            File.Delete(tempFile);

            ProtocolSettings.Initialize(config).Should().BeTrue();
            ProtocolSettings.Default.NativeActivations.Count.Should().Be(1);
            ProtocolSettings.Default.NativeActivations["test"].Should().Be(123);
        }

        [TestMethod]
        public void Cant_initialize_ProtocolSettings_after_default_settings_used()
        {
            var mainNetMagic = 0x4F454Eu;
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

        [TestMethod]
        public void TestGetMemoryPoolMaxTransactions()
        {
            ProtocolSettings.Default.MemoryPoolMaxTransactions.Should().Be(50000);
        }

        [TestMethod]
        public void TestGetMillisecondsPerBlock()
        {
            ProtocolSettings.Default.MillisecondsPerBlock.Should().Be(200);
        }

        [TestMethod]
        public void TestGetSeedList()
        {
            ProtocolSettings.Default.SeedList.Should().BeEquivalentTo(new string[] { "seed1.neo.org:10333", "seed2.neo.org:10333", "seed3.neo.org:10333", "seed4.neo.org:10333", "seed5.neo.org:10333", });
        }

        [TestMethod]
        public void TestNativeActivations()
        {
            ProtocolSettings.Default.NativeActivations.Count.Should().Be(0);
        }
    }
}
