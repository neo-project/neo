// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ProtocolOptionsModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Models;
using Neo.Build.Core.Tests.Helpers;

namespace Neo.Build.Core.Tests.Models
{
    [TestClass]
    public class UT_ProtocolOptionsModel
    {
        [TestMethod]
        public void CheckPropertyValues()
        {
            var jsonTestString = "{\"network\":810960196,\"addressVersion\":53,\"millisecondsPerBlock\":1000,\"maxTransactionsPerBlock\":512,\"memoryPoolMaxTransactions\":50000,\"maxTraceableBlocks\":2102400,\"initialGasDistribution\":5200000000000000}";

            var actualProtocolOptionsModel = JsonModel.FromJson<ProtocolOptionsModel>(jsonTestString, TestDefaults.JsonDefaultSerializerOptions);

            Assert.IsNotNull(actualProtocolOptionsModel);

            Assert.AreEqual(810960196u, actualProtocolOptionsModel.Network);
            Assert.AreEqual(53, actualProtocolOptionsModel.AddressVersion);
            Assert.AreEqual(1_000u, actualProtocolOptionsModel.MillisecondsPerBlock);
            Assert.AreEqual(512u, actualProtocolOptionsModel.MaxTransactionsPerBlock);
            Assert.AreEqual(50_000, actualProtocolOptionsModel.MemoryPoolMaxTransactions);
            Assert.AreEqual(2_102_400u, actualProtocolOptionsModel.MaxTraceableBlocks);
            Assert.AreEqual(5_200_000_000_000_000uL, actualProtocolOptionsModel.InitialGasDistribution);
        }
    }
}
