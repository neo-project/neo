// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TransferOutput.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets;
using System.Numerics;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_TransferOutput
    {
        [TestMethod]
        public void Properties_RoundTrip()
        {
            var asset = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var to = UInt160.Parse("0x0000000000000000000000000000000000000002");
            var data = new object();

            var output = new TransferOutput
            {
                AssetId = asset,
                Value = new BigDecimal(BigInteger.One, 0),
                ScriptHash = to,
                Data = data
            };

            Assert.AreEqual(asset, output.AssetId);
            Assert.AreEqual(to, output.ScriptHash);
            Assert.AreEqual(new BigDecimal(BigInteger.One, 0), output.Value);
            Assert.AreSame(data, output.Data);
        }

        [TestMethod]
        public void Data_CanBeNull()
        {
            var output = new TransferOutput
            {
                AssetId = UInt160.Zero,
                Value = new BigDecimal(BigInteger.Zero, 8),
                ScriptHash = UInt160.Zero,
                Data = null
            };
            Assert.IsNull(output.Data);
        }
    }
}
