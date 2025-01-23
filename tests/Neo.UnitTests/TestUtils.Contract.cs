// Copyright (C) 2015-2025 The Neo Project.
//
// TestUtils.Contract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using System;
using System.Linq;

namespace Neo.UnitTests
{
    partial class TestUtils
    {
        public static ContractManifest CreateDefaultManifest()
        {
            return new ContractManifest
            {
                Name = "testManifest",
                Groups = [],
                SupportedStandards = [],
                Abi = new ContractAbi
                {
                    Events = [],
                    Methods =
                    [
                        new ContractMethodDescriptor
                        {
                            Name = "testMethod",
                            Parameters = [],
                            ReturnType = ContractParameterType.Void,
                            Offset = 0,
                            Safe = true
                        }
                    ]
                },
                Permissions = [ContractPermission.DefaultPermission],
                Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(),
                Extra = null
            };
        }

        public static ContractManifest CreateManifest(string method, ContractParameterType returnType, params ContractParameterType[] parameterTypes)
        {
            var manifest = CreateDefaultManifest();
            manifest.Abi.Methods =
            [
                new ContractMethodDescriptor()
                {
                    Name = method,
                    Parameters = parameterTypes.Select((p, i) => new ContractParameterDefinition
                    {
                        Name = $"p{i}",
                        Type = p
                    }).ToArray(),
                    ReturnType = returnType
                }
            ];
            return manifest;
        }

        public static ContractState GetContract(string method = "test", int parametersCount = 0)
        {
            NefFile nef = new()
            {
                Compiler = "",
                Source = "",
                Tokens = [],
                Script = new byte[] { 0x01, 0x01, 0x01, 0x01 }
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            return new ContractState
            {
                Id = 0x43000000,
                Nef = nef,
                Hash = nef.Script.Span.ToScriptHash(),
                Manifest = CreateManifest(method, ContractParameterType.Any, Enumerable.Repeat(ContractParameterType.Any, parametersCount).ToArray())
            };
        }

        internal static ContractState GetContract(byte[] script, ContractManifest manifest = null)
        {
            NefFile nef = new()
            {
                Compiler = "",
                Source = "",
                Tokens = [],
                Script = script
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            return new ContractState
            {
                Id = 1,
                Hash = script.ToScriptHash(),
                Nef = nef,
                Manifest = manifest ?? CreateDefaultManifest()
            };
        }
    }
}
