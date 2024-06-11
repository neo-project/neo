// Copyright (C) 2015-2024 The Neo Project.
//
// UnitTest1.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.


using Microsoft.Extensions.Configuration;
using Neo.SDK.Wallet;
using Xunit;

namespace Neo.SDK.Tests.Wallet
{
    public class UT_NEP6Wallet
    {
        [Fact]
        public void Test_Static_Load()
        {
            var root = new ConfigurationBuilder().AddJsonFile("wallet1.json", optional: false).Build();
            var wallet = NEP6Wallet.Load(root);

            Assert.NotNull(wallet);

            Assert.NotNull(wallet.Name);
            Assert.Equal("wallet1", wallet.Name);

            Assert.NotNull(wallet.Scrypt);
            Assert.Equal(ScryptParameters.Default, wallet.Scrypt);
        }
    }
}
