// Copyright (C) 2015-2025 The Neo Project.
//
// UT_UPnP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network;
using Neo.UnitTests.Network;
using System;
using System.Linq;
using System.Net;

namespace Neo.Extensions.Tests.Net
{
    [TestClass]
    public class UT_UPnP
    {
        public TestContext TestContext { get; set; }

        private TestUPnPMockServer _server;
        private TestUPnPServerConfig _serverConfig;

        [TestInitialize]
        public void Setup()
        {
            _serverConfig = new();
            _server = new(_serverConfig);
            _server.Start();
            Utility.Logging += TestUpnp_Logging;
        }

        private void TestUpnp_Logging(string source, LogLevel level, object message)
        {
            TestContext.WriteLine("[{0}] {1}: {2}", level, source, message);
        }

        [TestCleanup]
        public void Clean()
        {
            _server.Dispose();
            Utility.Logging -= TestUpnp_Logging;
        }

        [TestMethod]
        public void Connect()
        {
            var expectedServiceControlUri = new Uri("http://127.0.0.1:5431/uuid:0000e068-20a0-00e0-20a0-48a802086048/WANIPConnection:1");
            var expectedDeviceUri = new Uri("http://127.0.0.1:5431/dyndev/uuid:0000e068-20a0-00e0-20a0-48a8000808e0");

            var actualDevices = UPnP.Search();
            var (actualDeviceUri, actualDevice) = actualDevices.FirstOrDefault();

            Assert.IsNotNull(actualDeviceUri);
            Assert.IsNotNull(actualDevice);
            Assert.AreEqual(expectedDeviceUri, actualDeviceUri);
            Assert.AreEqual(IPAddress.Loopback, actualDevice.HostEndPoint.Address);
            Assert.AreEqual(5431, actualDevice.HostEndPoint.Port);
            Assert.AreEqual(expectedServiceControlUri, actualDevice.ServiceControlUri);
            Assert.AreEqual("urn:schemas-upnp-org:service:WANIPConnection:1", actualDevice.ServiceType);
        }
    }
}
