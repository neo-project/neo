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
using Neo.Cryptography.ECC;

namespace Neo.Build.Core.Tests.Models
{
    [TestClass]
    public class UT_ProtocolOptionsModel
    {
        [TestMethod]
        public void CheckPropertyValues()
        {
            var jsonTestString = "{\"network\":810960196,\"addressVersion\":53,\"millisecondsPerBlock\":1000,\"maxTransactionsPerBlock\":512,\"memoryPoolMaxTransactions\":50000,\"maxTraceableBlocks\":2102400,\"hardforks\":{\"HF_Aspidochelone\":0},\"initialGasDistribution\":5200000000000000,\"validatorsCount\":1,\"standbyCommittee\":[\"03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c\"],\"seedList\":[\"seed1.neo.org:10333\"]}";
            var actualProtocolOptionsModel = JsonModel.FromJson<ProtocolOptionsModel>(jsonTestString, TestDefaults.JsonDefaultSerializerOptions);

            Assert.IsNotNull(actualProtocolOptionsModel);

            Assert.AreEqual(810960196u, actualProtocolOptionsModel.Network);
            Assert.AreEqual(53, actualProtocolOptionsModel.AddressVersion);
            Assert.AreEqual(1_000u, actualProtocolOptionsModel.MillisecondsPerBlock);
            Assert.AreEqual(512u, actualProtocolOptionsModel.MaxTransactionsPerBlock);
            Assert.AreEqual(50_000, actualProtocolOptionsModel.MemoryPoolMaxTransactions);
            Assert.AreEqual(2_102_400u, actualProtocolOptionsModel.MaxTraceableBlocks);
            Assert.AreEqual(5_200_000_000_000_000uL, actualProtocolOptionsModel.InitialGasDistribution);
            Assert.AreEqual(1, actualProtocolOptionsModel.ValidatorsCount);

            Assert.IsNotNull(actualProtocolOptionsModel.Hardforks);
            Assert.AreEqual(0u, actualProtocolOptionsModel.Hardforks[Hardfork.HF_Aspidochelone]);

            Assert.IsNotNull(actualProtocolOptionsModel.StandbyCommittee);
            Assert.AreEqual(ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1), actualProtocolOptionsModel.StandbyCommittee[0]);

            Assert.IsNotNull(actualProtocolOptionsModel.SeedList);
            Assert.AreEqual("seed1.neo.org:10333", actualProtocolOptionsModel.SeedList[0]);
        }
    }
}
