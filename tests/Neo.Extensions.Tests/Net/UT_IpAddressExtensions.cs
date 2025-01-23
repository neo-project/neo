// Copyright (C) 2015-2025 The Neo Project.
//
// UT_IpAddressExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Neo.Extensions;
using System.Net;

namespace Neo.Extensions.Tests.Net
{
    [TestClass]
    public class UT_IpAddressExtensions
    {
        [TestMethod]
        public void TestUnmapForIPAddress()
        {
            var addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
            addr.UnMap().Should().Be(addr);

            var addr2 = addr.MapToIPv6();
            addr2.UnMap().Should().Be(addr);
        }

        [TestMethod]
        public void TestUnmapForIPEndPoin()
        {
            var addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
            var endPoint = new IPEndPoint(addr, 8888);
            endPoint.UnMap().Should().Be(endPoint);

            var addr2 = addr.MapToIPv6();
            var endPoint2 = new IPEndPoint(addr2, 8888);
            endPoint2.UnMap().Should().Be(endPoint);
        }
    }
}
