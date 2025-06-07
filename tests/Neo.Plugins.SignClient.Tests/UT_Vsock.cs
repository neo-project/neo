// Copyright (C) 2015-2025 The Neo Project.
// 
// UT_Vsock.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neo.Plugins.SignClient.Tests
{
    [TestClass]
    public class UT_Vsock
    {
        [TestMethod]
        public void TestGetVsockAddress()
        {
            var address = new VsockAddress(1, 9991);
            var section = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PluginConfiguration:EndpointType"] = Settings.EndpointVsock,
                    ["PluginConfiguration:Endpoint"] = $"http://{address.ContextId}:{address.Port}"
                })
                .Build()
                .GetSection("PluginConfiguration");

            var settings = new Settings(section);
            Assert.AreEqual(address, settings.GetVsockAddress());

            section = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PluginConfiguration:Endpoint"] = "http://127.0.0.1:9991",
                    ["PluginConfiguration:EndpointType"] = Settings.EndpointTcp
                })
                .Build()
                .GetSection("PluginConfiguration");
            Assert.IsNull(new Settings(section).GetVsockAddress());
        }

        [TestMethod]
        public void TestInvalidEndpoint()
        {
            var section = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PluginConfiguration:EndpointType"] = Settings.EndpointVsock,
                    ["PluginConfiguration:Endpoint"] = "http://127.0.0.1:9991"
                })
                .Build()
                .GetSection("PluginConfiguration");
            Assert.ThrowsExactly<FormatException>(() => _ = new Settings(section).GetVsockAddress());

            section = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PluginConfiguration:EndpointType"] = "xyz",
                    ["PluginConfiguration:Endpoint"] = "http://127.0.0.1:9991"
                })
                .Build()
                .GetSection("PluginConfiguration");
            Assert.ThrowsExactly<FormatException>(() => _ = new Settings(section).GetVsockAddress());
        }
    }
}
