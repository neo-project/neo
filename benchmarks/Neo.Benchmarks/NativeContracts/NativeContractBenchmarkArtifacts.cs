// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractBenchmarkArtifacts.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System;
using System.IO;
using System.Text;

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Produces shared NEF and manifest artifacts for benchmark scenarios.
    /// </summary>
    internal static class NativeContractBenchmarkArtifacts
    {
        public static NefFile CreateBenchmarkNef()
        {
            var nef = new NefFile
            {
                Compiler = "benchmark",
                Source = "benchmark",
                Tokens = Array.Empty<MethodToken>(),
                Script = new byte[] { (byte)OpCode.RET }
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            return nef;
        }

        public static byte[] CreateBenchmarkNefBytes(NativeContractInputProfile profile)
        {
            return CreateBenchmarkNefBytes();
        }

        public static byte[] CreateBenchmarkNefBytes()
        {
            var nef = CreateBenchmarkNef();
            using MemoryStream ms = new();
            using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                nef.Serialize(writer);
            }
            return ms.ToArray();
        }

        public static ContractManifest CreateBenchmarkManifestDefinition(NativeContractInputProfile profile)
        {
            return CreateBenchmarkManifestDefinition(profile.Name);
        }

        public static ContractManifest CreateBenchmarkManifestDefinition(string profileName)
        {
            return new ContractManifest
            {
                Name = $"Benchmark.{profileName}",
                Groups = Array.Empty<ContractGroup>(),
                SupportedStandards = Array.Empty<string>(),
                Abi = new ContractAbi
                {
                    Events = Array.Empty<ContractEventDescriptor>(),
                    Methods = new[]
                    {
                        new ContractMethodDescriptor
                        {
                            Name = ContractBasicMethod.Deploy,
                            Parameters = new[]
                            {
                                new ContractParameterDefinition { Name = "data", Type = ContractParameterType.Any },
                                new ContractParameterDefinition { Name = "update", Type = ContractParameterType.Boolean }
                            },
                            ReturnType = ContractParameterType.Void,
                            Offset = 0,
                            Safe = false
                        }
                    }
                },
                Permissions = new[] { ContractPermission.DefaultPermission },
                Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(),
                Extra = new JObject()
            };
        }

        public static byte[] CreateBenchmarkManifestBytes(NativeContractInputProfile profile)
        {
            return CreateBenchmarkManifestDefinition(profile).ToJson().ToByteArray(indented: false);
        }
    }
}
