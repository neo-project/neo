// Copyright (C) 2015-2024 The Neo Project.
//
// TestProtocolSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;

namespace Neo.UnitTests
{
    public static class TestProtocolSettings
    {
        public static readonly ProtocolSettings Default = new()
        {
            Network = 5195086u,
            AddressVersion = ProtocolSettings.Default.AddressVersion,
            StandbyCommittee =
            [
                //Validators
                ECPoint.Parse("0327708fd7a39d384cd1cc48803a084a43d3b5013ada9de299a12e7b7afccfa013", ECCurve.Secp256r1),
                ECPoint.Parse("02e691f222d1867098643aca188a3d290c2da98af9e1bcf94b35d67322c034988d", ECCurve.Secp256r1),
                ECPoint.Parse("03e556db7d519f65f43437251e8fd7df476f2d02a0e3ef3052c298791b7b1d812d", ECCurve.Secp256r1),
                ECPoint.Parse("030459831e43793a1f7a104692cd944eaa475b03a0c5298d9fbbca136c8268f978", ECCurve.Secp256r1),
                ECPoint.Parse("03f01c078f19887a525897ec6503f310170deab72c4ae672eef0ac45f46b082eee", ECCurve.Secp256r1),
                ECPoint.Parse("02226db3e9fe07d83ccedae8fd8f8cc12aff4ef541f06fedf9caa1828683829b9e", ECCurve.Secp256r1),
                ECPoint.Parse("03cce2da0e69b79d1aa5bec1378497be41567feac5f2dfb754b46fa421ec08c49e", ECCurve.Secp256r1),

                //Other Members
                ECPoint.Parse("023a36c72844610b4d34d1968662424011bf783ca9d984efa19a20babf5582f3fe", ECCurve.Secp256r1),
                ECPoint.Parse("03708b860c1de5d87f5b151a12c2a99feebd2e8b315ee8e7cf8aa19692a9e18379", ECCurve.Secp256r1),
                ECPoint.Parse("03c6aa6e12638b36e88adc1ccdceac4db9929575c3e03576c617c49cce7114a050", ECCurve.Secp256r1),
                ECPoint.Parse("03204223f8c86b8cd5c89ef12e4f0dbb314172e9241e30c9ef2293790793537cf0", ECCurve.Secp256r1),
                ECPoint.Parse("02a62c915cf19c7f19a50ec217e79fac2439bbaad658493de0c7d8ffa92ab0aa62", ECCurve.Secp256r1),
                ECPoint.Parse("03409f31f0d66bdc2f70a9730b66fe186658f84a8018204db01c106edc36553cd0", ECCurve.Secp256r1),
                ECPoint.Parse("0288342b141c30dc8ffcde0204929bb46aed5756b41ef4a56778d15ada8f0c6654", ECCurve.Secp256r1),
                ECPoint.Parse("020f2887f41474cfeb11fd262e982051c1541418137c02a0f4961af911045de639", ECCurve.Secp256r1),
                ECPoint.Parse("0222038884bbd1d8ff109ed3bdef3542e768eef76c1247aea8bc8171f532928c30", ECCurve.Secp256r1),
                ECPoint.Parse("03d281b42002647f0113f36c7b8efb30db66078dfaaa9ab3ff76d043a98d512fde", ECCurve.Secp256r1),
                ECPoint.Parse("02504acbc1f4b3bdad1d86d6e1a08603771db135a73e61c9d565ae06a1938cd2ad", ECCurve.Secp256r1),
                ECPoint.Parse("0226933336f1b75baa42d42b71d9091508b638046d19abd67f4e119bf64a7cfb4d", ECCurve.Secp256r1),
                ECPoint.Parse("03cdcea66032b82f5c30450e381e5295cae85c5e6943af716cc6b646352a6067dc", ECCurve.Secp256r1),
                ECPoint.Parse("02cd5a5547119e24feaa7c2a0f37b8c9366216bab7054de0065c9be42084003c8a", ECCurve.Secp256r1)
            ],
            ValidatorsCount = 7,
            SeedList =
            [
                "seed1.neo.org:10333",
                "seed2.neo.org:10333",
                "seed3.neo.org:10333",
                "seed4.neo.org:10333",
                "seed5.neo.org:10333"
            ],
            MillisecondsPerBlock = ProtocolSettings.Default.MillisecondsPerBlock,
            MaxTransactionsPerBlock = ProtocolSettings.Default.MaxTransactionsPerBlock,
            MemoryPoolMaxTransactions = ProtocolSettings.Default.MemoryPoolMaxTransactions,
            MaxTraceableBlocks = ProtocolSettings.Default.MaxTraceableBlocks,
            InitialGasDistribution = ProtocolSettings.Default.InitialGasDistribution,
            Hardforks = ProtocolSettings.Default.Hardforks
        };

        public static readonly ProtocolSettings SoleNode = new()
        {
            Network = 5195086u,
            AddressVersion = ProtocolSettings.Default.AddressVersion,
            StandbyCommittee =
            [
                //Validators
                ECPoint.Parse("0278ed78c917797b637a7ed6e7a9d94e8c408444c41ee4c0a0f310a256b9271eda", ECCurve.Secp256r1)
            ],
            ValidatorsCount = 1,
            SeedList =
            [
                "seed1.neo.org:10333",
                "seed2.neo.org:10333",
                "seed3.neo.org:10333",
                "seed4.neo.org:10333",
                "seed5.neo.org:10333"
            ],
            MillisecondsPerBlock = ProtocolSettings.Default.MillisecondsPerBlock,
            MaxTransactionsPerBlock = ProtocolSettings.Default.MaxTransactionsPerBlock,
            MemoryPoolMaxTransactions = ProtocolSettings.Default.MemoryPoolMaxTransactions,
            MaxTraceableBlocks = ProtocolSettings.Default.MaxTraceableBlocks,
            InitialGasDistribution = ProtocolSettings.Default.InitialGasDistribution,
            Hardforks = ProtocolSettings.Default.Hardforks
        };
    }
}
