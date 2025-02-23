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

using Neo.Build.Core.Models.Wallets;
using Neo.SmartContract;
using Neo.Wallets;
using System;

namespace Neo.Build.Core.Tests.Helpers
{
    internal static class TestObjectHelper
    {
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
            };
    }
}
