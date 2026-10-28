// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractArgumentGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.BLS12_381;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using VMArray = Neo.VM.Types.Array;
using VMBuffer = Neo.VM.Types.Buffer;
using VMStruct = Neo.VM.Types.Struct;

#nullable enable

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Builds deterministic argument factories for native contract benchmark scenarios.
    /// </summary>
    public sealed class NativeContractArgumentGenerator
    {
        private readonly struct KnownFactory
        {
            public KnownFactory(
                Func<NativeContractBenchmarkContext, NativeContractInputProfile, object> valueFactory,
                Func<NativeContractInputProfile, string> summaryFactory)
            {
                ValueFactory = valueFactory;
                SummaryFactory = summaryFactory;
            }

            public Func<NativeContractBenchmarkContext, NativeContractInputProfile, object> ValueFactory { get; }
            public Func<NativeContractInputProfile, string> SummaryFactory { get; }
        }

        private readonly ImmutableDictionary<Type, KnownFactory> _knownFactories;
        private static readonly byte[] s_blsScalarOne = Scalar.One.ToArray();
        private static readonly byte[] s_ecdsaMessage = Encoding.ASCII.GetBytes("HelloWorld");
        private static readonly (byte[] PubKey, byte[] Signature) s_secp256k1Sha256Vector = CreateEcdsaVector(
            "0B5FB3A050385196B327BE7D86CBCE6E40A04C8832445AF83AD19C82103B3ED9",
            "04B6363B353C3EE1620C5AF58594458AA00ABF43A6D134D7C4CB2D901DC0F474FD74C94740BD7169AA0B1EF7BC657E824B1D7F4283C547E7EC18C8576ACF84418A",
            ECCurve.Secp256k1,
            HashAlgorithm.SHA256);
        private static readonly (byte[] PubKey, byte[] Signature) s_secp256r1Sha256Vector = CreateEcdsaVector(
            "6E63FDA41E9E3ABA9BB5696D58A75731F044A9BDC48FE546DA571543B2FA460E",
            "04CAE768E1CF58D50260CAB808DA8D6D83D5D3AB91EAC41CDCE577CE5862D736413643BDECD6D21C3B66F122AB080F9219204B10AA8BBCEB86C1896974768648F3",
            ECCurve.Secp256r1,
            HashAlgorithm.SHA256);

        public NativeContractArgumentGenerator()
        {
            _knownFactories = BuildKnownFactories();
        }

        public bool TryBuildArgumentFactory(
            NativeContract contract,
            MethodInfo handler,
            IReadOnlyList<InteropParameterDescriptor> parameters,
            NativeContractInputProfile profile,
            out Func<NativeContractBenchmarkContext, object[]> factory,
            out string summary,
            out string? failureReason)
        {
            if (parameters.Count == 0)
            {
                factory = _ => global::System.Array.Empty<object>();
                summary = "No parameters";
                failureReason = null;
                return true;
            }

            var generators = new List<Func<NativeContractBenchmarkContext, object>>(parameters.Count);
            var descriptorParts = new List<string>(parameters.Count);
            var overrides = new Func<NativeContractBenchmarkContext, object>?[parameters.Count];
            var overrideDescriptors = new string[parameters.Count];

            ApplyMethodOverrides(contract, handler, profile, overrides, overrideDescriptors);

            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (overrides[i] is null && parameter.Type == typeof(UInt160))
                {
                    var slot = i;
                    overrides[i] = ctx => ctx.GetAccount(profile.Size, slot);
                    overrideDescriptors[i] = $"UInt160(BenchmarkAccount[{slot}])";
                }

                if (overrides[i] is not null)
                {
                    generators.Add(overrides[i]!);
                    descriptorParts.Add($"{parameter.Name}:{overrideDescriptors[i]}");
                    continue;
                }

                if (!TryBuildGenerator(parameter.Type, profile, out var generator, out var descriptor, out failureReason))
                {
                    factory = null!;
                    summary = string.Empty;
                    return false;
                }

                generators.Add(generator);
                descriptorParts.Add($"{parameter.Name}:{descriptor}");
            }

            summary = string.Join("; ", descriptorParts);
            factory = context =>
            {
                var values = new object[generators.Count];
                for (int i = 0; i < generators.Count; i++)
                    values[i] = generators[i](context);
                return values;
            };
            failureReason = null;
            return true;
        }

        private void ApplyMethodOverrides(
            NativeContract contract,
            MethodInfo handler,
            NativeContractInputProfile profile,
            Func<NativeContractBenchmarkContext, object>?[] overrides,
            string[] overrideDescriptors)
        {
            if (contract.Name is nameof(CryptoLib))
            {
                switch (handler.Name)
                {
                    case nameof(CryptoLib.Bls12381Serialize):
                        overrides[0] = _ => new InteropInterface(G1Affine.Generator);
                        overrideDescriptors[0] = "Interop(G1Affine.Generator)";
                        break;

                    case nameof(CryptoLib.Bls12381Deserialize):
                        var bytes = CreateBlsSerializedBytes(profile);
                        overrides[0] = _ => bytes;
                        overrideDescriptors[0] = $"byte[{bytes.Length}]";
                        break;

                    case nameof(CryptoLib.Bls12381Equal):
                        overrides[0] = _ => new InteropInterface(G1Affine.Generator);
                        overrideDescriptors[0] = "Interop(G1Affine)";
                        overrides[1] = _ => new InteropInterface(G1Affine.Generator);
                        overrideDescriptors[1] = "Interop(G1Affine)";
                        break;

                    case nameof(CryptoLib.Bls12381Add):
                        overrides[0] = _ => new InteropInterface(new G1Projective(G1Affine.Generator));
                        overrideDescriptors[0] = "Interop(G1Projective)";
                        overrides[1] = _ => new InteropInterface(G1Affine.Identity);
                        overrideDescriptors[1] = "Interop(G1Affine.Identity)";
                        break;

                    case nameof(CryptoLib.Bls12381Mul):
                        overrides[0] = _ => new InteropInterface(Gt.Generator);
                        overrideDescriptors[0] = "Interop(Gt.Generator)";
                        overrides[1] = _ => s_blsScalarOne;
                        overrideDescriptors[1] = "Scalar(32 bytes)";
                        break;

                    case nameof(CryptoLib.Bls12381Pairing):
                        overrides[0] = _ => new InteropInterface(G1Affine.Generator);
                        overrideDescriptors[0] = "Interop(G1Affine)";
                        overrides[1] = _ => new InteropInterface(G2Affine.Generator);
                        overrideDescriptors[1] = "Interop(G2Affine)";
                        break;

                    case nameof(CryptoLib.VerifyWithECDsa):
                    case "VerifyWithECDsaV0":
                        var ecdsaVector = profile.Size switch
                        {
                            NativeContractInputSize.Tiny or NativeContractInputSize.Small => s_secp256k1Sha256Vector,
                            _ => s_secp256r1Sha256Vector
                        };
                        overrides[0] = _ => s_ecdsaMessage;
                        overrideDescriptors[0] = "\"HelloWorld\"";
                        overrides[1] = _ => ecdsaVector.PubKey;
                        overrideDescriptors[1] = "pubkey(uncompressed)";
                        overrides[2] = _ => ecdsaVector.Signature;
                        overrideDescriptors[2] = "signature";
                        break;
                }
            }

            if (contract.Name is nameof(StdLib))
            {
                switch (handler.Name)
                {
                    case nameof(StdLib.Atoi):
                        overrides[0] = _ => CreateDecimalString(profile);
                        overrideDescriptors[0] = "numeric string";
                        if (overrides.Length > 1)
                        {
                            overrides[1] = _ => 10;
                            overrideDescriptors[1] = "base10";
                        }
                        break;

                    case nameof(StdLib.Itoa):
                        if (overrides.Length > 1)
                        {
                            overrides[1] = _ => 10;
                            overrideDescriptors[1] = "base10";
                        }
                        break;

                    case nameof(StdLib.Base64Decode):
                        overrides[0] = _ => CreateBase64String(profile);
                        overrideDescriptors[0] = "base64";
                        break;

                    case nameof(StdLib.Base64UrlDecode):
                        overrides[0] = _ => CreateBase64UrlString(profile);
                        overrideDescriptors[0] = "base64url";
                        break;

                    case nameof(StdLib.Base58Decode):
                        overrides[0] = _ => CreateBase58String(profile);
                        overrideDescriptors[0] = "base58";
                        break;

                    case nameof(StdLib.Base58CheckDecode):
                        overrides[0] = _ => CreateBase58CheckString(profile);
                        overrideDescriptors[0] = "base58check";
                        break;

                    case "Deserialize":
                        overrides[0] = _ => CreateSerializedStackItemBytes(profile);
                        overrideDescriptors[0] = "serialized stack item bytes";
                        break;

                    case "JsonSerialize":
                        overrides[0] = _ => CreateJsonFriendlyStackItem(profile);
                        overrideDescriptors[0] = "StackItem(JSON-friendly)";
                        break;

                    case "JsonDeserialize":
                        overrides[0] = _ => CreateJsonBytes(profile);
                        overrideDescriptors[0] = "json bytes";
                        break;

                    case "MemorySearch":
                        overrides[0] = _ => Encoding.UTF8.GetBytes("abc");
                        overrideDescriptors[0] = "\"abc\" bytes";
                        overrides[1] = _ => Encoding.UTF8.GetBytes("c");
                        overrideDescriptors[1] = "\"c\" bytes";
                        if (overrides.Length > 2)
                        {
                            overrides[2] = _ => 0;
                            overrideDescriptors[2] = "start=0";
                        }
                        if (overrides.Length > 3)
                        {
                            overrides[3] = _ => false;
                            overrideDescriptors[3] = "backward=false";
                        }
                        break;
                }
            }

            if (contract.Name is nameof(RoleManagement))
            {
                switch (handler.Name)
                {
                    case "DesignateAsRole":
                        var role = ResolveRole(profile.Size);
                        overrides[0] = _ => role;
                        overrideDescriptors[0] = $"Role.{role}";
                        var nodeCount = DescribeRoleNodeCount(profile);
                        overrides[1] = ctx => CreateRoleNodes(ctx, profile);
                        overrideDescriptors[1] = $"ECPoint[{nodeCount}]";
                        break;

                    case "GetDesignatedByRole":
                        var queryRole = ResolveRole(profile.Size);
                        overrides[0] = _ => queryRole;
                        overrideDescriptors[0] = $"Role.{queryRole}";
                        overrides[1] = _ => 0u;
                        overrideDescriptors[1] = "index=0";
                        break;
                }
            }

            if (contract.Name is nameof(PolicyContract))
            {
                switch (handler.Name)
                {
                    case "SetMillisecondsPerBlock":
                        overrides[0] = ctx => CreatePolicyMilliseconds(ctx);
                        overrideDescriptors[0] = "milliseconds";
                        break;

                    case "SetAttributeFeeV0":
                        overrides[0] = _ => (byte)TransactionAttributeType.HighPriority;
                        overrideDescriptors[0] = "attribute=HighPriority";
                        overrides[1] = _ => CreatePolicyAttributeFee(profile);
                        overrideDescriptors[1] = "fee";
                        break;

                    case "SetAttributeFeeV1":
                        overrides[0] = _ => (byte)TransactionAttributeType.NotaryAssisted;
                        overrideDescriptors[0] = "attribute=NotaryAssisted";
                        overrides[1] = _ => CreatePolicyAttributeFee(profile);
                        overrideDescriptors[1] = "fee";
                        break;

                    case "GetAttributeFeeV0":
                        overrides[0] = _ => (byte)TransactionAttributeType.HighPriority;
                        overrideDescriptors[0] = "attribute=HighPriority";
                        break;

                    case "GetAttributeFeeV1":
                        overrides[0] = _ => (byte)TransactionAttributeType.NotaryAssisted;
                        overrideDescriptors[0] = "attribute=NotaryAssisted";
                        break;

                    case "SetFeePerByte":
                        overrides[0] = _ => CreatePolicyFeePerByte(profile);
                        overrideDescriptors[0] = "feePerByte";
                        break;

                    case "SetExecFeeFactor":
                        overrides[0] = _ => CreatePolicyExecFeeFactor(profile);
                        overrideDescriptors[0] = "execFeeFactor";
                        break;

                    case "SetStoragePrice":
                        overrides[0] = _ => CreatePolicyStoragePrice(profile);
                        overrideDescriptors[0] = "storagePrice";
                        break;

                    case "SetMaxValidUntilBlockIncrement":
                        overrides[0] = ctx => CreatePolicyMaxVub(ctx, profile);
                        overrideDescriptors[0] = "maxVUB";
                        break;

                    case "SetMaxTraceableBlocks":
                        overrides[0] = ctx => CreatePolicyMaxTraceableBlocks(ctx, profile);
                        overrideDescriptors[0] = "maxTraceable";
                        break;

                    case "BlockAccount":
                    case "UnblockAccount":
                        overrides[0] = ctx => ctx.GetAccount(profile.Size);
                        overrideDescriptors[0] = "UInt160(BenchmarkAccount)";
                        break;
                }
            }

            if (contract.Name is nameof(OracleContract))
            {
                switch (handler.Name)
                {
                    case "SetPrice":
                        overrides[0] = _ => CreateOraclePrice(profile);
                        overrideDescriptors[0] = "price";
                        break;
                    case "Request":
                        overrides[0] = _ => CreateOracleUrl(profile);
                        overrideDescriptors[0] = "url";
                        overrides[1] = _ => CreateOracleFilter(profile);
                        overrideDescriptors[1] = "filter";
                        overrides[2] = _ => CreateOracleCallback(profile);
                        overrideDescriptors[2] = "callback";
                        overrides[3] = _ => CreateOracleUserData(profile);
                        overrideDescriptors[3] = "userData";
                        overrides[4] = _ => CreateOracleGasBudget(profile);
                        overrideDescriptors[4] = "gasForResponse";
                        break;
                }
            }

            if (contract.Name is nameof(NeoToken))
            {
                switch (handler.Name)
                {
                    case "OnNEP17Payment":
                        overrides[0] = ctx => ctx.PrimaryBenchmarkAccount;
                        overrideDescriptors[0] = "from";
                        overrides[1] = ctx => ctx.NeoRegisterPrice;
                        overrideDescriptors[1] = "amount(register price)";
                        overrides[2] = ctx => CreateNeoCandidateData(ctx);
                        overrideDescriptors[2] = "candidate pubkey";
                        break;
                    case "RegisterCandidate":
                        overrides[0] = ctx =>
                        {
                            var committee = ctx.ProtocolSettings.StandbyCommittee;
                            return committee.Count > 0 ? committee[0] : ECCurve.Secp256r1.G;
                        };
                        overrideDescriptors[0] = "committee pubkey";
                        break;
                    case "UnclaimedGas":
                        overrides[0] = ctx => ctx.PrimaryBenchmarkAccount;
                        overrideDescriptors[0] = "benchmark account";
                        overrides[1] = ctx => ctx.SeededLedgerHeight + 1;
                        overrideDescriptors[1] = "end=seededHeight+1";
                        break;
                }
            }

            if (contract.Name is nameof(Notary))
            {
                switch (handler.Name)
                {
                    case "BalanceOf":
                    case "ExpirationOf":
                        overrides[0] = ctx => ctx.PrimaryBenchmarkAccount;
                        overrideDescriptors[0] = "benchmark account";
                        break;
                    case "LockDepositUntil":
                        overrides[0] = ctx => ctx.PrimaryBenchmarkAccount;
                        overrideDescriptors[0] = "benchmark account";
                        overrides[1] = ctx => CreateNotaryLockHeight(ctx);
                        overrideDescriptors[1] = "lockHeight";
                        break;
                    case "SetMaxNotValidBeforeDelta":
                        overrides[0] = ctx => CreateNotaryMaxDelta(ctx);
                        overrideDescriptors[0] = "maxDelta";
                        break;
                    case "OnNEP17Payment":
                        overrides[0] = ctx => ctx.PrimaryBenchmarkAccount;
                        overrideDescriptors[0] = "from=sender";
                        overrides[1] = _ => NativeContract.GAS.Factor * 20;
                        overrideDescriptors[1] = "amount(20 GAS)";
                        overrides[2] = ctx => CreateNotaryPaymentData(ctx, profile);
                        overrideDescriptors[2] = "data=[to,till]";
                        break;
                    case "Verify":
                        overrides[0] = _ => global::System.Array.Empty<byte>();
                        overrideDescriptors[0] = "signature";
                        break;
                    case "Withdraw":
                        overrides[0] = ctx => ctx.NotaryDepositAccount;
                        overrideDescriptors[0] = "deposit owner";
                        overrides[1] = ctx => ctx.GetAccount(profile.Size, 1);
                        overrideDescriptors[1] = "receiver";
                        break;
                }
            }

            if (contract.Name is nameof(LedgerContract))
            {
                switch (handler.Name)
                {
                    case nameof(LedgerContract.GetBlock):
                        overrides[0] = _ => BitConverter.GetBytes(0u);
                        overrideDescriptors[0] = "blockIndex(0)";
                        break;
                    case "GetTransactionFromBlock":
                        overrides[0] = _ => BitConverter.GetBytes(0u);
                        overrideDescriptors[0] = "blockIndex(0)";
                        overrides[1] = _ => 0;
                        overrideDescriptors[1] = "txIndex(0)";
                        break;
                }
            }

            if (contract.Name is nameof(NeoToken))
            {
                switch (handler.Name)
                {
                    case "SetGasPerBlock":
                        overrides[0] = _ => NativeContract.GAS.Factor * 5;
                        overrideDescriptors[0] = "gasPerBlock(5 GAS)";
                        break;
                }
            }

            if (contract.Name is nameof(ContractManagement))
            {
                var parameterInfos = handler.GetParameters();
                var reflectionOffset = Math.Max(0, parameterInfos.Length - overrideDescriptors.Length);

                switch (handler.Name)
                {
                    case "Deploy":
                    case "Update":
                        for (int i = 0; i < overrideDescriptors.Length; i++)
                        {
                            var infoIndex = i + reflectionOffset;
                            if (infoIndex < 0 || infoIndex >= parameterInfos.Length)
                                continue;

                            var parameterInfo = parameterInfos[infoIndex];
                            var parameterName = parameterInfo.Name;
                            if (string.Equals(parameterName, "nefFile", StringComparison.OrdinalIgnoreCase))
                            {
                                overrides[i] = _ => NativeContractBenchmarkArtifacts.CreateBenchmarkNefBytes(profile);
                                overrideDescriptors[i] = "nef bytes";
                            }
                            else if (string.Equals(parameterName, "manifest", StringComparison.OrdinalIgnoreCase))
                            {
                                overrides[i] = _ => NativeContractBenchmarkArtifacts.CreateBenchmarkManifestBytes(profile);
                                overrideDescriptors[i] = "manifest bytes";
                            }
                            else if (string.Equals(parameterName, "data", StringComparison.OrdinalIgnoreCase) &&
                                     parameterInfo.ParameterType == typeof(StackItem))
                            {
                                overrides[i] = _ => StackItem.Null;
                                overrideDescriptors[i] = "StackItem.Null";
                            }
                        }
                        break;
                }
            }
        }

        private bool TryBuildGenerator(
            Type type,
            NativeContractInputProfile profile,
            out Func<NativeContractBenchmarkContext, object> generator,
            out string descriptor,
            out string? failureReason,
            int depth = 0)
        {
            if (depth > 8)
            {
                generator = null!;
                descriptor = string.Empty;
                failureReason = $"Nested parameter depth {depth} exceeds supported limit for type {type.Name}.";
                return false;
            }

            if (_knownFactories.TryGetValue(type, out var knownFactory))
            {
                generator = ctx => knownFactory.ValueFactory(ctx, profile);
                descriptor = knownFactory.SummaryFactory(profile);
                failureReason = null;
                return true;
            }

            if (type.IsEnum)
            {
                var values = Enum.GetValues(type);
                var enumValue = values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(type);
                generator = _ => enumValue!;
                descriptor = $"{type.Name}.{enumValue}";
                failureReason = null;
                return true;
            }

            if (type.IsArray && type != typeof(byte[]))
            {
                var elementType = type.GetElementType()!;
                if (!TryBuildGenerator(elementType, profile, out var elementFactory, out var elementDescriptor, out failureReason, depth + 1))
                {
                    generator = null!;
                    descriptor = string.Empty;
                    return false;
                }

                var length = Math.Max(1, profile.ElementCount);
                generator = ctx =>
                {
                    var array = global::System.Array.CreateInstance(elementType, length);
                    for (int i = 0; i < length; i++)
                        array.SetValue(elementFactory(ctx), i);
                    return array;
                };
                descriptor = $"{elementDescriptor}[{length}]";
                return true;
            }

            if (typeof(IEnumerable<UInt160>).IsAssignableFrom(type))
            {
                var list = Enumerable.Repeat(CreateUInt160(0), Math.Max(1, profile.ElementCount)).ToList();
                generator = _ => list;
                descriptor = $"UInt160Enumerable({list.Count})";
                failureReason = null;
                return true;
            }

            if (typeof(IEnumerable<UInt256>).IsAssignableFrom(type))
            {
                var list = Enumerable.Repeat(CreateUInt256(1), Math.Max(1, profile.ElementCount)).ToList();
                generator = _ => list;
                descriptor = $"UInt256Enumerable({list.Count})";
                failureReason = null;
                return true;
            }

            if (type == typeof(object))
            {
                generator = _ => new ByteString(CreateBytes(Math.Max(1, profile.ByteLength)));
                descriptor = "ByteString(object)";
                failureReason = null;
                return true;
            }

            if (type == typeof(IReadOnlyStore))
            {
                generator = ctx => ctx.StoreView;
                descriptor = "StoreView";
                failureReason = null;
                return true;
            }

            if (typeof(DataCache).IsAssignableFrom(type))
            {
                generator = ctx => ctx.GetSnapshot();
                descriptor = "DataCache";
                failureReason = null;
                return true;
            }

            if (type == typeof(Transaction))
            {
                var tx = CreateBenchmarkTransaction(profile);
                generator = _ => tx;
                descriptor = "BenchmarkTransaction";
                failureReason = null;
                return true;
            }

            generator = null!;
            descriptor = string.Empty;
            failureReason = $"Unsupported parameter type {type.FullName}.";
            return false;
        }

        private ImmutableDictionary<Type, KnownFactory> BuildKnownFactories()
        {
            var builder = ImmutableDictionary.CreateBuilder<Type, KnownFactory>();

            builder[typeof(bool)] = new KnownFactory(
                (_, profile) => profile.Size != NativeContractInputSize.Tiny,
                profile => $"bool={profile.Size != NativeContractInputSize.Tiny}");

            builder[typeof(byte)] = new KnownFactory(
                (_, profile) => (byte)(profile.ByteLength % byte.MaxValue),
                profile => $"byte={profile.ByteLength % byte.MaxValue}");

            builder[typeof(sbyte)] = new KnownFactory(
                (_, profile) => (sbyte)(profile.ByteLength % sbyte.MaxValue),
                _ => "sbyte");

            builder[typeof(short)] = new KnownFactory(
                (_, profile) => (short)(profile.ByteLength % short.MaxValue),
                _ => "short");

            builder[typeof(ushort)] = new KnownFactory(
                (_, profile) => (ushort)(profile.ByteLength % ushort.MaxValue),
                _ => "ushort");

            builder[typeof(int)] = new KnownFactory(
                (_, profile) => ClampToInt(profile.IntegerMagnitude),
                profile => $"int={ClampToInt(profile.IntegerMagnitude)}");

            builder[typeof(uint)] = new KnownFactory(
                (_, profile) => ClampToUInt(profile.IntegerMagnitude),
                profile => $"uint={ClampToUInt(profile.IntegerMagnitude)}");

            builder[typeof(long)] = new KnownFactory(
                (_, profile) => ClampToLong(profile.IntegerMagnitude),
                profile => $"long={ClampToLong(profile.IntegerMagnitude)}");

            builder[typeof(ulong)] = new KnownFactory(
                (_, profile) => ClampToULong(profile.IntegerMagnitude),
                profile => $"ulong={ClampToULong(profile.IntegerMagnitude)}");

            builder[typeof(BigInteger)] = new KnownFactory(
                (_, profile) => profile.IntegerMagnitude,
                profile =>
                {
                    var bits = profile.IntegerMagnitude.IsZero ? 0 : (int)profile.IntegerMagnitude.GetBitLength();
                    return $"BigInt[{bits}-bits]";
                });

            builder[typeof(byte[])] = new KnownFactory(
                (_, profile) => CreateBytes(profile.ByteLength),
                profile => $"byte[{Math.Max(1, profile.ByteLength)}]");

            builder[typeof(ReadOnlyMemory<byte>)] = new KnownFactory(
                (_, profile) => new ReadOnlyMemory<byte>(CreateBytes(profile.ByteLength)),
                profile => $"ReadOnlyMemory[{Math.Max(1, profile.ByteLength)}]");

            builder[typeof(string)] = new KnownFactory(
                (_, profile) => new string('a', Math.Max(1, profile.ByteLength)),
                profile => $"string({Math.Max(1, profile.ByteLength)})");

            builder[typeof(UInt160)] = new KnownFactory(
                (_, profile) => CreateUInt160((int)profile.Size + 1),
                profile => $"UInt160({profile.Size})");

            builder[typeof(UInt256)] = new KnownFactory(
                (_, profile) => CreateUInt256((int)profile.Size + 1),
                profile => $"UInt256({profile.Size})");

            builder[typeof(ECPoint)] = new KnownFactory(
                (ctx, profile) =>
                {
                    var committee = ctx.ProtocolSettings.StandbyCommittee;
                    if (committee.Count > 0)
                    {
                        var index = Math.Min((int)profile.Size, committee.Count - 1);
                        return committee[index];
                    }

                    return profile.Size switch
                    {
                        NativeContractInputSize.Tiny or NativeContractInputSize.Medium => ECCurve.Secp256k1.G,
                        NativeContractInputSize.Small or NativeContractInputSize.Large => ECCurve.Secp256r1.G,
                        _ => ECCurve.Secp256r1.G
                    };
                },
                profile => $"ECPoint({profile.Size})");

            builder[typeof(ContractParameterType)] = new KnownFactory(
                (_, _) => ContractParameterType.Integer,
                _ => "ContractParameterType.Integer");

            builder[typeof(CallFlags)] = new KnownFactory(
                (_, _) => CallFlags.All,
                _ => "CallFlags.All");

            builder[typeof(NamedCurveHash)] = new KnownFactory(
                (_, profile) => profile.Size switch
                {
                    NativeContractInputSize.Tiny or NativeContractInputSize.Small => NamedCurveHash.secp256k1SHA256,
                    _ => NamedCurveHash.secp256r1SHA256
                },
                profile => profile.Size switch
                {
                    NativeContractInputSize.Tiny or NativeContractInputSize.Small => "NamedCurveHash.secp256k1SHA256",
                    _ => "NamedCurveHash.secp256r1SHA256"
                });

            builder[typeof(Role)] = new KnownFactory(
                (_, profile) => ResolveRole(profile.Size),
                profile => $"Role.{ResolveRole(profile.Size)}");

            builder[typeof(ByteString)] = new KnownFactory(
                (_, profile) => new ByteString(CreateBytes(profile.ByteLength)),
                profile => $"ByteString[{Math.Max(1, profile.ByteLength)}]");

            builder[typeof(VMBuffer)] = new KnownFactory(
                (_, profile) => new VMBuffer(CreateBytes(profile.ByteLength)),
                profile => $"Buffer[{Math.Max(1, profile.ByteLength)}]");

            builder[typeof(StackItem)] = new KnownFactory(
                (_, profile) => new ByteString(CreateBytes(Math.Max(1, profile.ByteLength / 2))),
                profile => $"StackItem(ByteString[{Math.Max(1, profile.ByteLength / 2)}])");

            builder[typeof(VMArray)] = new KnownFactory(
                (_, profile) =>
                {
                    var element = new ByteString(CreateBytes(Math.Max(1, profile.ByteLength / 4)));
                    return new VMArray(Enumerable.Repeat<StackItem>(element, Math.Max(1, profile.ElementCount / 2)));
                },
                profile => $"Array(ByteString x{Math.Max(1, profile.ElementCount / 2)})");

            builder[typeof(VMStruct)] = new KnownFactory(
                (_, profile) =>
                {
                    var element = new ByteString(CreateBytes(Math.Max(1, profile.ByteLength / 4)));
                    return new VMStruct(Enumerable.Repeat<StackItem>(element, Math.Max(1, profile.ElementCount / 2)));
                },
                profile => $"Struct(ByteString x{Math.Max(1, profile.ElementCount / 2)})");

            builder[typeof(HashAlgorithm)] = new KnownFactory(
                (_, _) => HashAlgorithm.SHA256,
                _ => "HashAlgorithm.SHA256");

            return builder.ToImmutable();
        }

        private static byte[] CreateBlsSerializedBytes(NativeContractInputProfile profile)
        {
            return profile.Size switch
            {
                NativeContractInputSize.Tiny => G1Affine.Generator.ToCompressed(),
                NativeContractInputSize.Small => G2Affine.Generator.ToCompressed(),
                NativeContractInputSize.Medium => Gt.Generator.ToArray(),
                NativeContractInputSize.Large => Gt.Generator.Double().ToArray(),
                _ => G1Affine.Generator.ToCompressed()
            };
        }

        private static int ClampToInt(BigInteger value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }

        private static uint ClampToUInt(BigInteger value)
        {
            if (value < uint.MinValue) return uint.MinValue;
            if (value > uint.MaxValue) return uint.MaxValue;
            return (uint)value;
        }

        private static long ClampToLong(BigInteger value)
        {
            if (value > long.MaxValue) return long.MaxValue;
            if (value < long.MinValue) return long.MinValue;
            return (long)value;
        }

        private static ulong ClampToULong(BigInteger value)
        {
            if (value < 0) return 0;
            if (value > ulong.MaxValue) return ulong.MaxValue;
            return (ulong)value;
        }

        private static byte[] CreateBytes(int length)
        {
            var size = Math.Max(1, length);
            var data = new byte[size];
            for (int i = 0; i < size; i++)
                data[i] = (byte)((i * 37 + 11) & 0xFF);
            return data;
        }

        private static UInt160 CreateUInt160(int seed)
        {
            Span<byte> buffer = stackalloc byte[UInt160.Length];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)(seed + i * 13);
            return new UInt160(buffer);
        }

        private static UInt256 CreateUInt256(int seed)
        {
            Span<byte> buffer = stackalloc byte[UInt256.Length];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)(seed + i * 7);
            return new UInt256(buffer);
        }

        private static string CreateDecimalString(NativeContractInputProfile profile)
        {
            int length = Math.Max(1, Math.Min(profile.ByteLength, 64));
            const string digits = "1234567890";
            StringBuilder sb = new(length);
            for (int i = 0; i < length; i++)
                sb.Append(digits[i % digits.Length]);
            return sb.ToString();
        }

        private static string CreateBase64String(NativeContractInputProfile profile)
        {
            var bytes = CreateBytes(Math.Max(4, profile.ByteLength));
            return Convert.ToBase64String(bytes);
        }

        private static string CreateBase64UrlString(NativeContractInputProfile profile)
        {
            var base64 = CreateBase64String(profile);
            return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string CreateBase58String(NativeContractInputProfile profile)
        {
            var bytes = CreateBytes(Math.Max(4, profile.ByteLength));
            return Base58.Encode(bytes);
        }

        private static string CreateBase58CheckString(NativeContractInputProfile profile)
        {
            var bytes = CreateBytes(Math.Max(4, profile.ByteLength));
            return Base58.Base58CheckEncode(bytes);
        }

        private static byte[] CreateJsonBytes(NativeContractInputProfile profile)
        {
            var value = Math.Abs(ClampToInt(profile.IntegerMagnitude));
            var json = $"{{\"value\":{value}}}";
            return Encoding.UTF8.GetBytes(json);
        }

        private static StackItem CreateJsonFriendlyStackItem(NativeContractInputProfile profile)
        {
            var count = Math.Clamp(profile.ElementCount, 1, 6);
            List<StackItem> items = new(count + 1);
            for (int i = 0; i < count; i++)
            {
                var token = CreateUtf8Token(profile, i);
                items.Add(new ByteString(Encoding.UTF8.GetBytes(token)));
            }

            if (profile.Size != NativeContractInputSize.Tiny)
            {
                var nested = new VMArray(new StackItem[]
                {
                    new ByteString(Encoding.UTF8.GetBytes("bucket")),
                    new ByteString(Encoding.UTF8.GetBytes(profile.Name))
                });
                items.Add(nested);
            }

            return new VMArray(items);
        }

        private static ECPoint[] CreateRoleNodes(NativeContractBenchmarkContext context, NativeContractInputProfile profile)
        {
            ArgumentNullException.ThrowIfNull(context);
            var committee = context.ProtocolSettings.StandbyCommittee;
            var desired = DescribeRoleNodeCount(profile);
            if (committee.Count == 0)
                return new[] { ECCurve.Secp256r1.G };
            var actual = Math.Min(desired, Math.Min(committee.Count, 32));
            return committee.Take(actual).ToArray();
        }

        private static int DescribeRoleNodeCount(NativeContractInputProfile profile)
        {
            return Math.Clamp(Math.Max(1, profile.ElementCount), 1, 32);
        }

        private static uint CreatePolicyMilliseconds(NativeContractBenchmarkContext context)
        {
            var value = context.ProtocolSettings.MillisecondsPerBlock;
            if (value == 0)
                value = 1;
            if (value > PolicyContract.MaxMillisecondsPerBlock)
                value = PolicyContract.MaxMillisecondsPerBlock;
            return value;
        }

        private static uint CreatePolicyAttributeFee(NativeContractInputProfile profile)
        {
            var baseline = (uint)(profile.ElementCount * 1_000 + 100);
            if (baseline == 0)
                baseline = 100;
            if (baseline > PolicyContract.MaxAttributeFee)
                baseline = PolicyContract.MaxAttributeFee;
            return baseline;
        }

        private static long CreatePolicyFeePerByte(NativeContractInputProfile profile)
        {
            var candidate = PolicyContract.DefaultFeePerByte + profile.ElementCount * 10;
            if (candidate > 100_00000000L)
                candidate = 100_00000000L;
            return candidate;
        }

        private static uint CreatePolicyExecFeeFactor(NativeContractInputProfile profile)
        {
            var candidate = PolicyContract.DefaultExecFeeFactor + (uint)Math.Max(1, profile.ElementCount);
            if (candidate < 1)
                candidate = 1;
            if (candidate > PolicyContract.MaxExecFeeFactor)
                candidate = PolicyContract.MaxExecFeeFactor;
            return candidate;
        }

        private static uint CreatePolicyStoragePrice(NativeContractInputProfile profile)
        {
            var candidate = PolicyContract.DefaultStoragePrice + (uint)Math.Max(1, profile.ByteLength);
            if (candidate > PolicyContract.MaxStoragePrice)
                candidate = PolicyContract.MaxStoragePrice;
            return candidate;
        }

        private static uint CreatePolicyMaxVub(NativeContractBenchmarkContext context, NativeContractInputProfile profile)
        {
            var baseValue = context.ProtocolSettings.MaxValidUntilBlockIncrement;
            if (baseValue == 0)
                baseValue = 1;
            var spread = Math.Min(baseValue - 1, (uint)Math.Max(1, profile.ElementCount));
            var candidate = baseValue > spread ? baseValue - spread : 1u;
            var traceable = context.ProtocolSettings.MaxTraceableBlocks;
            if (traceable > 0 && candidate >= traceable)
                candidate = traceable - 1;
            if (candidate == 0)
                candidate = 1;
            if (candidate > PolicyContract.MaxMaxValidUntilBlockIncrement)
                candidate = PolicyContract.MaxMaxValidUntilBlockIncrement;
            return candidate;
        }

        private static uint CreatePolicyMaxTraceableBlocks(NativeContractBenchmarkContext context, NativeContractInputProfile profile)
        {
            var current = context.ProtocolSettings.MaxTraceableBlocks;
            if (current == 0)
                current = PolicyContract.MaxMaxTraceableBlocks;
            var decrement = (uint)Math.Max(1, profile.ElementCount);
            var candidate = current > decrement ? current - decrement : current - 1;
            var maxValid = Math.Max(1u, context.ProtocolSettings.MaxValidUntilBlockIncrement);
            if (candidate <= maxValid)
                candidate = maxValid + 1;
            if (candidate > PolicyContract.MaxMaxTraceableBlocks)
                candidate = PolicyContract.MaxMaxTraceableBlocks;
            return candidate;
        }

        private static string CreateOracleUrl(NativeContractInputProfile profile)
        {
            var slug = profile.Name.ToLowerInvariant();
            var url = $"https://oracle.neo/{slug}/{profile.ElementCount}";
            return url.Length > 128 ? url[..128] : url;
        }

        private static string CreateOracleFilter(NativeContractInputProfile profile)
        {
            return profile.Size switch
            {
                NativeContractInputSize.Tiny => string.Empty,
                NativeContractInputSize.Small => "$.result",
                NativeContractInputSize.Medium => "$.data.items[0]",
                _ => "$.data.items[1]"
            };
        }

        private static string CreateOracleCallback(NativeContractInputProfile profile)
        {
            return profile.Size switch
            {
                NativeContractInputSize.Tiny => "onOracle",
                NativeContractInputSize.Small => "oracleHandler",
                NativeContractInputSize.Medium => "oracleResult",
                _ => "oracleLarge"
            };
        }

        private static StackItem CreateOracleUserData(NativeContractInputProfile profile)
        {
            var bytes = CreateBytes(Math.Min(profile.ByteLength, 64));
            return new ByteString(bytes);
        }

        private static long CreateOracleGasBudget(NativeContractInputProfile profile)
        {
            long baseValue = 0_20000000;
            long increment = (long)Math.Max(1, profile.ElementCount) * 1_0000000;
            return baseValue + increment;
        }

        private static long CreateOraclePrice(NativeContractInputProfile profile)
        {
            long baseValue = 50_000000;
            long bump = (long)Math.Max(1, profile.ElementCount) * 100_0000;
            return baseValue + bump;
        }

        private static uint CreateNotaryLockHeight(NativeContractBenchmarkContext context)
        {
            var current = NativeContract.Ledger.CurrentIndex(context.StoreView);
            var baseHeight = Math.Max(2u, current + 5);
            var depositTill = context.GetSeededNotaryTill();
            return Math.Max(baseHeight, depositTill + 1);
        }

        private static uint CreateNotaryMaxDelta(NativeContractBenchmarkContext context)
        {
            var min = (uint)ProtocolSettings.Default.ValidatorsCount;
            var half = Math.Max(min + 1, context.ProtocolSettings.MaxValidUntilBlockIncrement / 2);
            var candidate = min + 5;
            if (candidate >= half)
                candidate = half > min ? half - 1 : min;
            return Math.Max(min, candidate);
        }

        private static StackItem CreateNotaryPaymentData(NativeContractBenchmarkContext context, NativeContractInputProfile profile)
        {
            var receiver = context.GetAccount(profile.Size, 2);
            var targetHeight = Math.Max(context.SeededLedgerHeight + 5, context.GetSeededNotaryTill() + 2);
            return new VMArray(new StackItem[]
            {
                new ByteString(receiver.ToArray()),
                new Integer(targetHeight)
            });
        }

        private static StackItem CreateNeoCandidateData(NativeContractBenchmarkContext context)
        {
            var committee = context.ProtocolSettings.StandbyCommittee;
            var pubkey = committee.Count > 0 ? committee[0] : ECCurve.Secp256r1.G;
            return new ByteString(pubkey.EncodePoint(true));
        }

        private static byte[] CreateSerializedStackItemBytes(NativeContractInputProfile profile)
        {
            var stackItem = CreateJsonFriendlyStackItem(profile);
            return BinarySerializer.Serialize(stackItem, ExecutionEngineLimits.Default);
        }

        private static string CreateUtf8Token(NativeContractInputProfile profile, int index)
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
            var length = Math.Clamp(profile.ByteLength / 8, 6, 32);
            var builder = new StringBuilder(profile.Name.Length + length + 8);
            builder.Append(profile.Name);
            builder.Append('_');
            builder.Append(index);
            builder.Append('_');
            for (int i = 0; i < length; i++)
                builder.Append(alphabet[(index + i) % alphabet.Length]);
            return builder.ToString();
        }

        private static Role ResolveRole(NativeContractInputSize size) => size switch
        {
            NativeContractInputSize.Tiny => Role.StateValidator,
            NativeContractInputSize.Small => Role.Oracle,
            NativeContractInputSize.Medium => Role.NeoFSAlphabetNode,
            NativeContractInputSize.Large => Role.P2PNotary,
            _ => Role.StateValidator
        };

        private Transaction CreateBenchmarkTransaction(NativeContractInputProfile profile)
        {
            var script = profile.Size switch
            {
                NativeContractInputSize.Tiny => global::System.Array.Empty<byte>(),
                NativeContractInputSize.Small => new byte[] { (byte)OpCode.RET },
                NativeContractInputSize.Medium => Enumerable.Repeat((byte)OpCode.NOP, profile.ElementCount).ToArray(),
                NativeContractInputSize.Large => Enumerable.Repeat((byte)OpCode.NOP, profile.ElementCount * 2).ToArray(),
                _ => global::System.Array.Empty<byte>()
            };

            return new Transaction
            {
                Version = 0,
                Nonce = 1,
                Script = script,
                Signers = global::System.Array.Empty<Signer>(),
                Attributes = global::System.Array.Empty<TransactionAttribute>(),
                Witnesses = global::System.Array.Empty<Witness>(),
                ValidUntilBlock = 0,
                SystemFee = 0,
                NetworkFee = 0
            };
        }

        private static (byte[] PubKey, byte[] Signature) CreateEcdsaVector(string privateKeyHex, string publicKeyHex, ECCurve curve, HashAlgorithm hash)
        {
            var privateKey = Convert.FromHexString(privateKeyHex);
            var publicPoint = ECPoint.Parse(publicKeyHex, curve);
            var signature = Crypto.Sign(s_ecdsaMessage, privateKey, curve, hash);
            return (publicPoint.EncodePoint(false), signature);
        }

    }
}
