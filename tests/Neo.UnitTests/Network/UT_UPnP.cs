// Copyright (C) 2015-2024 The Neo Project.
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
using System;

namespace Neo.UnitTests.Network
{
    [TestClass]
    public class UT_UPnP
    {
        [TestMethod]
        public void GetTimeOut()
        {
            Assert.AreEqual(3, UPnP.TimeOut.TotalSeconds);
        }

        [TestMethod]
        public void NoService()
        {
            Assert.ThrowsException<Exception>(() => UPnP.ForwardPort(1, System.Net.Sockets.ProtocolType.Tcp, ""));
            Assert.ThrowsException<Exception>(() => UPnP.DeleteForwardingRule(1, System.Net.Sockets.ProtocolType.Tcp));
            Assert.ThrowsException<Exception>(() => UPnP.GetExternalIP());
        }
    }
}
