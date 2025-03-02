// Copyright (C) 2015-2025 The Neo Project.
//
// TestObjectHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Factories;
using Neo.Build.Core.Models;
using Neo.Build.Core.Models.Wallets;
using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;

namespace Neo.Build.Core.Tests.Helpers
{
    internal static class TestObjectHelper
    {
        public static TestWalletModel CreateTestWalletModelWithOutExtras() =>
            new()
            {
                Name = "Unit Test Wallet",
                Version = new(1, 0),
                Scrypt = SCryptModel.Default,
                Accounts = [
                    new()
                    {
                        Address = "0xce45fca32b8cd071bfbc20389c20cd7025f85ff0",
                        IsDefault = false,
                        Label = "Main Test Account",
                        Lock = false,
                        Key = new(Wallet.GetPrivateKeyFromWIF("Ky7cYncUA92kWnh7xymshpfgz7QiX46qPWCQBQPVUSv5vndE2VTR")),
                        Contract = new()
                        {
                            Deployed = false,
                            Script = Convert.FromBase64String("DCECjNhSCkN5\u002BL\u002BEc0/cgGPMgQkyrl8V2ddjYtevNcqDcahBVuezJw=="),
                            Parameters = [
                                new()
                                {
                                    Name = nameof(ContractParameterType.Signature),
                                    Type = ContractParameterType.Signature,
                                },
                            ],
                        },
                    },
                ],
            };

        public static TestWalletModel CreateTestWalletModel() =>
            new()
            {
                Name = "Unit Test Wallet",
                Version = new(1, 0),
                Scrypt = SCryptModel.Default,
                Accounts = [
                    new()
                    {
                        Address = "0xce45fca32b8cd071bfbc20389c20cd7025f85ff0",
                        IsDefault = false,
                        Label = "Main Test Account",
                        Lock = false,
                        Key = new(Wallet.GetPrivateKeyFromWIF("Ky7cYncUA92kWnh7xymshpfgz7QiX46qPWCQBQPVUSv5vndE2VTR")),
                        Contract = new()
                        {
                            Deployed = false,
                            Script = Convert.FromBase64String("DCECjNhSCkN5\u002BL\u002BEc0/cgGPMgQkyrl8V2ddjYtevNcqDcahBVuezJw=="),
                            Parameters = [
                                new()
                                {
                                    Name = nameof(ContractParameterType.Signature),
                                    Type = ContractParameterType.Signature,
                                },
                            ],
                        },
                    },
                ],
                Extra = new
                {
                    ProtocolConfiguration = new ProtocolSettingsModel()
                    {
                        Network = FunctionFactory.GetDevNetwork(0),
                        AddressVersion = 53,
                        MillisecondsPerBlock = 1_000u,
                        MaxTransactionsPerBlock = 512u,
                        MemoryPoolMaxTransactions = 50_000,
                        MaxTraceableBlocks = 2_102_400u,
                        InitialGasDistribution = 5_200_000_000_000_000u,
                        Hardforks = new Dictionary<Hardfork, uint>(),
                        ValidatorsCount = 1,
                        StandbyCommittee = [
                            ECPoint.Parse("028cd8520a4379f8bf84734fdc8063cc810932ae5f15d9d76362d7af35ca8371a8", ECCurve.Secp256r1),
                        ],
                        SeedList = [
                            "127.0.0.1:20037"
                        ],
                    }
                }
            };
    }
}
