// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcError.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neo.Plugins.RpcServer.Tests
{
    [TestClass]
    public class UT_RpcError
    {
        [TestMethod]
        public void AllDifferent()
        {
            HashSet<string> codes = new();

            foreach (RpcError error in typeof(RpcError)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(u => u.DeclaringType == typeof(RpcError))
                .Select(u => u.GetValue(null))
                .Cast<RpcError>())
            {
                Assert.IsTrue(codes.Add(error.ToString()));

                if (error.Code == RpcError.WalletFeeLimit.Code)
                    Assert.IsNotNull(error.Data);
                else
                    Assert.IsNull(error.Data);
            }
        }

        [TestMethod]
        public void TestJson()
        {
            Assert.AreEqual("{\"code\":-600,\"message\":\"Access denied\"}", RpcError.AccessDenied.ToJson().ToString(false));
        }
    }
}
