// Copyright (C) 2015-2026 The Neo Project.
//
// TestWalletAccount.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Moq;
using Neo.Factories;
using Neo.SmartContract;
using Neo.Wallets;

namespace Neo.UnitTests;

class TestWalletAccount : WalletAccount
{
    private static readonly KeyPair key;

    public override bool HasKey => true;
    public override KeyPair GetKey() => key;

    public TestWalletAccount(UInt160 hash)
        : base(hash, TestProtocolSettings.Default)
    {
        var mock = new Mock<Contract>(() => new Contract
        {
            Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
            ParameterList = new[] { ContractParameterType.Signature }
        });
        mock.SetupGet(p => p.ScriptHash).Returns(hash);
        Contract = mock.Object;
    }

    static TestWalletAccount()
    {
        byte[] prikey = RandomNumberFactory.NextBytes(32);
        key = new KeyPair(prikey);
    }
}
