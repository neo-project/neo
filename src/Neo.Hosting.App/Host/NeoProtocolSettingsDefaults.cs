// Copyright (C) 2015-2024 The Neo Project.
//
// NeoProtocolSettingsDefaults.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Neo.Hosting.App.Host
{
    internal class NeoProtocolSettingsDefaults
    {
        public static ProtocolSettings MainNet = new()
        {
            Network = 860833102u,
            AddressVersion = 53,
            MillisecondsPerBlock = 15_000u,
            MaxTransactionsPerBlock = 512u,
            MemoryPoolMaxTransactions = 50_000,
            MaxTraceableBlocks = 2_102_400u,
            InitialGasDistribution = 5_200_000_000_000_000ul,
            ValidatorsCount = 7,
            StandbyCommittee = [
                // Validators
                ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1),
                ECPoint.Parse("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", ECCurve.Secp256r1),
                ECPoint.Parse("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", ECCurve.Secp256r1),
                ECPoint.Parse("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", ECCurve.Secp256r1),
                ECPoint.Parse("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", ECCurve.Secp256r1),
                ECPoint.Parse("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", ECCurve.Secp256r1),
                ECPoint.Parse("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", ECCurve.Secp256r1),
                // Other Members
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
            SeedList = [
                "seed1.neo.org:10333",
                "seed2.neo.org:10333",
                "seed3.neo.org:10333",
                "seed4.neo.org:10333",
                "seed5.neo.org:10333"
            ],
            Hardforks = new Dictionary<Hardfork, uint>()
            {
                [Hardfork.HF_Aspidochelone] = 1_730_000u,
                [Hardfork.HF_Basilisk] = 4_120_000u,
                [Hardfork.HF_Cockatrice] = 5_450_000u,
            }.ToImmutableDictionary(),
        };

        public static ProtocolSettings TestNet = new()
        {
            Network = 894710606u,
            AddressVersion = 53,
            MillisecondsPerBlock = 15_000u,
            MaxTransactionsPerBlock = 5_000u,
            MemoryPoolMaxTransactions = 50_000,
            MaxTraceableBlocks = 2_102_400u,
            InitialGasDistribution = 5_200_000_000_000_000ul,
            ValidatorsCount = 7,
            StandbyCommittee = [
                // Validators
                ECPoint.Parse("023e9b32ea89b94d066e649b124fd50e396ee91369e8e2a6ae1b11c170d022256d", ECCurve.Secp256r1),
                ECPoint.Parse("03009b7540e10f2562e5fd8fac9eaec25166a58b26e412348ff5a86927bfac22a2", ECCurve.Secp256r1),
                ECPoint.Parse("02ba2c70f5996f357a43198705859fae2cfea13e1172962800772b3d588a9d4abd", ECCurve.Secp256r1),
                ECPoint.Parse("03408dcd416396f64783ac587ea1e1593c57d9fea880c8a6a1920e92a259477806", ECCurve.Secp256r1),
                ECPoint.Parse("02a7834be9b32e2981d157cb5bbd3acb42cfd11ea5c3b10224d7a44e98c5910f1b", ECCurve.Secp256r1),
                ECPoint.Parse("0214baf0ceea3a66f17e7e1e839ea25fd8bed6cd82e6bb6e68250189065f44ff01", ECCurve.Secp256r1),
                ECPoint.Parse("030205e9cefaea5a1dfc580af20c8d5aa2468bb0148f1a5e4605fc622c80e604ba", ECCurve.Secp256r1),
                // Other Members
                ECPoint.Parse("025831cee3708e87d78211bec0d1bfee9f4c85ae784762f042e7f31c0d40c329b8", ECCurve.Secp256r1),
                ECPoint.Parse("02cf9dc6e85d581480d91e88e8cbeaa0c153a046e89ded08b4cefd851e1d7325b5", ECCurve.Secp256r1),
                ECPoint.Parse("03840415b0a0fcf066bcc3dc92d8349ebd33a6ab1402ef649bae00e5d9f5840828", ECCurve.Secp256r1),
                ECPoint.Parse("026328aae34f149853430f526ecaa9cf9c8d78a4ea82d08bdf63dd03c4d0693be6", ECCurve.Secp256r1),
                ECPoint.Parse("02c69a8d084ee7319cfecf5161ff257aa2d1f53e79bf6c6f164cff5d94675c38b3", ECCurve.Secp256r1),
                ECPoint.Parse("0207da870cedb777fceff948641021714ec815110ca111ccc7a54c168e065bda70", ECCurve.Secp256r1),
                ECPoint.Parse("035056669864feea401d8c31e447fb82dd29f342a9476cfd449584ce2a6165e4d7", ECCurve.Secp256r1),
                ECPoint.Parse("0370c75c54445565df62cfe2e76fbec4ba00d1298867972213530cae6d418da636", ECCurve.Secp256r1),
                ECPoint.Parse("03957af9e77282ae3263544b7b2458903624adc3f5dee303957cb6570524a5f254", ECCurve.Secp256r1),
                ECPoint.Parse("03d84d22b8753cf225d263a3a782a4e16ca72ef323cfde04977c74f14873ab1e4c", ECCurve.Secp256r1),
                ECPoint.Parse("02147c1b1d5728e1954958daff2f88ee2fa50a06890a8a9db3fa9e972b66ae559f", ECCurve.Secp256r1),
                ECPoint.Parse("03c609bea5a4825908027e4ab217e7efc06e311f19ecad9d417089f14927a173d5", ECCurve.Secp256r1),
                ECPoint.Parse("0231edee3978d46c335e851c76059166eb8878516f459e085c0dd092f0f1d51c21", ECCurve.Secp256r1),
                ECPoint.Parse("03184b018d6b2bc093e535519732b3fd3f7551c8cffaf4621dd5a0b89482ca66c9", ECCurve.Secp256r1)
            ],
            SeedList = [
                "seed1t5.neo.org:20333",
                "seed2t5.neo.org:20333",
                "seed3t5.neo.org:20333",
                "seed4t5.neo.org:20333",
                "seed5t5.neo.org:20333"
            ],
            Hardforks = new Dictionary<Hardfork, uint>()
            {
                [Hardfork.HF_Aspidochelone] = 210_000u,
                [Hardfork.HF_Basilisk] = 2_680_000u,
                [Hardfork.HF_Cockatrice] = 3_967_000u,
            }.ToImmutableDictionary(),
        };
    }
}
