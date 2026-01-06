// Copyright (C) 2015-2026 The Neo Project.
//
// UT_DeployedContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Neo.SmartContract.Manifest;

namespace Neo.UnitTests.SmartContract;

[TestClass]
public class UT_DeployedContract
{
    [TestMethod]
    public void TestGetScriptHash()
    {
        var contract = new DeployedContract(new ContractState()
        {
            Manifest = new ContractManifest()
            {
                Name = "",
                Groups = [],
                SupportedStandards = [],
                Abi = new ContractAbi()
                {
                    Methods = new[]
                    {
                        new ContractMethodDescriptor()
                        {
                            Name = "verify",
                            Parameters = Array.Empty<ContractParameterDefinition>()
                        }
                    },
                    Events = []
                },
                Permissions = [],
                Trusts = WildcardContainer<ContractPermissionDescriptor>.CreateWildcard()
            },
            Nef = new NefFile
            {
                Compiler = "",
                Source = "",
                Tokens = [],
                Script = new byte[] { 1, 2, 3 }
            },
            Hash = new byte[] { 1, 2, 3 }.ToScriptHash()
        });

        Assert.AreEqual("0xb2e3fe334830b4741fa5d762f2ab36b90b86c49b", contract.ScriptHash.ToString());
    }

    [TestMethod]
    public void TestErrors()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => _ = new DeployedContract(new ContractState()
        {
            Hash = UInt160.Zero,
            Manifest = new ContractManifest()
            {
                Name = "",
                Groups = [],
                SupportedStandards = [],
                Abi = new ContractAbi()
                {
                    Methods = new[]
                    {
                        new ContractMethodDescriptor()
                        {
                            Name = "noverify",
                            Parameters = Array.Empty<ContractParameterDefinition>()
                        }
                    },
                    Events = []
                },
                Permissions = [],
                Trusts = WildcardContainer<ContractPermissionDescriptor>.CreateWildcard()
            },
            Nef = new NefFile
            {
                Compiler = "",
                Source = "",
                Tokens = [],
                Script = new byte[] { 1, 2, 3 }
            }
        }));
    }
}
