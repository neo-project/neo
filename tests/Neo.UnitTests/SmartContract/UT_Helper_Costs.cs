// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Helper_Costs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using static Neo.SmartContract.Helper;

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// Fee helpers not covered by contract-detection tests in UT_Helper.
    /// </summary>
    [TestClass]
    public class UT_Helper_Costs
    {
        [TestMethod]
        public void MaxVerificationGas_IsPositive()
        {
            Assert.IsTrue(MaxVerificationGas > 0);
        }

        [TestMethod]
        public void SignatureContractCost_IsPositive()
        {
            var cost = SignatureContractCost();
            Assert.IsTrue(cost > 0);
        }

        [TestMethod]
        public void MultiSignatureContractCost_ScalesWithMAndN()
        {
            var cost11 = MultiSignatureContractCost(1, 1);
            var cost12 = MultiSignatureContractCost(1, 2);
            var cost22 = MultiSignatureContractCost(2, 2);

            Assert.IsTrue(cost11 > 0);
            Assert.IsTrue(cost12 > cost11);
            Assert.IsTrue(cost22 > cost12);
        }
    }
}
