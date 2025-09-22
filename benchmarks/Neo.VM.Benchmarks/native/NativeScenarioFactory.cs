// Copyright (C) 2015-2025 The Neo Project.
//
// NativeScenarioFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Benchmark.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using VMArray = Neo.VM.Types.Array;
using VMStackItem = Neo.VM.Types.StackItem;

namespace Neo.VM.Benchmark.Native
{
    /// <summary>
    /// Builds simple read-only scenarios for native contract benchmarking.
    /// </summary>
    internal static class NativeScenarioFactory
    {
        private sealed record NativeMethod(
            string Id,
            UInt160 ContractHash,
            string MethodName,
            CallFlags Flags,
            Action<InstructionBuilder, ScenarioProfile>? EmitArguments = null,
            bool DropResult = true,
            Func<ScenarioProfile, BenchmarkApplicationEngine>? EngineFactory = null,
            Action<BenchmarkApplicationEngine, ScenarioProfile>? ConfigureEngine = null,
            Func<ScenarioProfile, ScenarioProfile>? ProfileFactory = null,
            Func<NativeMethod, ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet>? ScriptFactory = null);

        private static readonly IReadOnlyList<ECPoint> s_standbyValidators = BenchmarkProtocolSettings.StandbyValidators;
        private static readonly IReadOnlyList<ECPoint> s_standbyCommittee = BenchmarkProtocolSettings.StandbyCommittee;

        private static readonly UInt160 CommitteeAddress = Contract.GetBFTAddress(s_standbyValidators);

        private static readonly byte[] s_blsG1 = Convert.FromHexString("97F1D3A73197D7942695638C4FA9AC0FC3688C4F9774B905A14E3A3F171BAC586C55E83FF97A1AEFFB3AF00ADB22C6BB");
        private static readonly byte[] s_blsG2 = Convert.FromHexString("93E02B6052719F607DACD3A088274F65596BD0D09920B61AB5DA61BBDC7F5049334CF11213945D57E5AC7D055D042B7E024AA2B2F08F0A91260805272DC51051C6E47AD4FA403B02B4510B647AE3D1770BAC0326A805BBEFD48056C8C121BDB8");
        private static readonly byte[] s_blsGt = Convert.FromHexString("0F41E58663BF08CF068672CBD01A7EC73BACA4D72CA93544DEFF686BFD6DF543D48EAA24AFE47E1EFDE449383B67663104C581234D086A9902249B64728FFD21A189E87935A954051C7CDBA7B3872629A4FAFC05066245CB9108F0242D0FE3EF03350F55A7AEFCD3C31B4FCB6CE5771CC6A0E9786AB5973320C806AD360829107BA810C5A09FFDD9BE2291A0C25A99A211B8B424CD48BF38FCEF68083B0B0EC5C81A93B330EE1A677D0D15FF7B984E8978EF48881E32FAC91B93B47333E2BA5706FBA23EB7C5AF0D9F80940CA771B6FFD5857BAAF222EB95A7D2809D61BFE02E1BFD1B68FF02F0B8102AE1C2D5D5AB1A19F26337D205FB469CD6BD15C3D5A04DC88784FBB3D0B2DBDEA54D43B2B73F2CBB12D58386A8703E0F948226E47EE89D018107154F25A764BD3C79937A45B84546DA634B8F6BE14A8061E55CCEBA478B23F7DACAA35C8CA78BEAE9624045B4B601B2F522473D171391125BA84DC4007CFBF2F8DA752F7C74185203FCCA589AC719C34DFFBBAAD8431DAD1C1FB597AAA5193502B86EDB8857C273FA075A50512937E0794E1E65A7617C90D8BD66065B1FFFE51D7A579973B1315021EC3C19934F1368BB445C7C2D209703F239689CE34C0378A68E72A6B3B216DA0E22A5031B54DDFF57309396B38C881C4C849EC23E87089A1C5B46E5110B86750EC6A532348868A84045483C92B7AF5AF689452EAFABF1A8943E50439F1D59882A98EAA0170F1250EBD871FC0A92A7B2D83168D0D727272D441BEFA15C503DD8E90CE98DB3E7B6D194F60839C508A84305AACA1789B6");
        private static readonly byte[] s_blsScalar = CreateBlsScalar();
        private static readonly byte[] s_recoverMessageHash = Convert.FromHexString("5AE8317D34D1E595E3FA7247DB80C0AF4320CCE1116DE187F8F7E2E099C0D8D0");
        private static readonly byte[] s_recoverSignature = Convert.FromHexString("45C0B7F8C09A9E1F1CEA0C25785594427B6BF8F9F878A8AF0B1ABBB48E16D0920D8BECD0C220F67C51217EECFD7184EF0732481C843857E6BC7FC095C4F6B78801");
        private static readonly byte[] s_verifyMessage = Encoding.UTF8.GetBytes("中文");
        private static readonly byte[] s_verifySignature = Convert.FromHexString("B8CBA1FF42304D74D083E87706058F59CDD4F755B995926D2CD80A734C5A3C37E4583BFD4339AC762C1C91EEE3782660A6BAF62CD29E407ECCD3DA3E9DE55A02");
        private static readonly byte[] s_verifyPubKey = Convert.FromHexString("03661B86D54EB3A8E7EA2399E0DB36AB65753F95FFF661DA53AE0121278B881AD0");
        private static readonly byte[] s_ed25519Signature = Convert.FromHexString("E5564300C360AC729086E2CC806E828A84877F1EB8E5D974D873E065224901555FB8821590A33BACC61E39701CF9B46BD25BF5F0595BBE24655141438E7A100B");
        private static readonly byte[] s_ed25519PublicKey = Convert.FromHexString("D75A980182B10AB7D54BFED3C964073A0EE172F3DAA62325AF021A68F707511A");
        private static readonly ECPoint s_committeePublicKey = GetCommitteePublicKey();
        private const byte TokenAccountPrefix = 0x14;
        private const byte PolicyBlockedAccountPrefix = 15;
        private const byte ContractManagementStoragePrefixContract = 8;
        private const byte ContractManagementStoragePrefixContractHash = 12;
        private const byte NeoAccountPrefix = 20;
        private const byte NeoCandidatePrefix = 33;
        private const byte NeoRegisterPricePrefix = 13;
        private const byte NeoVotersCountPrefix = 1;
        private const byte NeoGasPerBlockPrefix = 29;
        private const byte NeoVoterRewardPrefix = 23;
        private const byte NotaryDepositPrefix = 1;
        private const byte NotaryMaxNotValidBeforeDeltaPrefix = 10;
        private const int NotaryDefaultDepositDeltaTill = 5760;
        private const int NotaryDefaultMaxNotValidBeforeDelta = 140;
        private const int OracleMaxUserDataLength = 512;
        private const byte OraclePricePrefix = 5;
        private const byte OracleRequestIdPrefix = 9;
        private const byte OracleRequestPrefix = 7;
        private const byte OracleIdListPrefix = 6;
        private static readonly byte[] s_committeePublicKeyCompressed = s_committeePublicKey.EncodePoint(true);
        private static readonly int s_validatorsCount = BenchmarkProtocolSettings.ValidatorsCount;
        private static readonly UInt160 s_gasTransferRecipient = UInt160.Parse("0x0102030405060708090a0b0c0d0e0f1011121314");
        private static readonly BigInteger s_gasTransferAmount = new BigInteger(1_0000_0000);
        private static readonly byte[] s_zeroHashBytes = new byte[UInt256.Length];
        private static readonly byte[] s_blockIndexBytes = [0x00];
        private static readonly long s_policyFeePerByteValue = PolicyContract.DefaultFeePerByte;
        private static readonly uint s_policyExecFeeFactorValue = PolicyContract.DefaultExecFeeFactor;
        private static readonly uint s_policyStoragePriceValue = PolicyContract.DefaultStoragePrice;
        private static readonly uint s_policyMillisecondsPerBlockValue = (uint)ProtocolSettings.Default.MillisecondsPerBlock;
        private static readonly uint s_policyMaxValidUntilValue = Math.Max(1u, Math.Min(PolicyContract.MaxMaxValidUntilBlockIncrement, ProtocolSettings.Default.MaxValidUntilBlockIncrement));
        private static readonly uint s_policyMaxTraceableValue = Math.Min(PolicyContract.MaxMaxTraceableBlocks, Math.Max(s_policyMaxValidUntilValue + 1, ProtocolSettings.Default.MaxTraceableBlocks));
        private static readonly uint s_policyAttributeFeeValue = 1_0000u;
        private static readonly byte s_policyAttributeType = (byte)TransactionAttributeType.HighPriority;
        private static readonly BigInteger s_defaultRegisterPrice = 1000 * NativeContract.GAS.Factor;

        private sealed record ContractArtifact(ContractState State, byte[] NefBytes, byte[] ManifestBytes);

        private static readonly Action<ExecutionContextState> s_setCallFlagsAll = state => state.CallFlags = CallFlags.All;
        private static readonly UInt160 s_contractDeployer = UInt160.Parse("0x0102030405060708090a0b0c0d0e0f1011121315");

        private const string UpdateContractName = "BenchmarkContract.Update";

        private static byte[] CreateBytePayload(ScenarioProfile profile, byte seed = 0x42, double scale = 1d, int minLength = 1)
        {
            var baseLength = profile.DataLength > 0 ? profile.DataLength : minLength;
            var length = Math.Max(minLength, (int)Math.Round(baseLength * scale, MidpointRounding.AwayFromZero));
            return BenchmarkDataFactory.CreateByteArray(length, seed);
        }

        private static string CreateStringPayload(ScenarioProfile profile, double scale = 1d, char seed = 'a', int minLength = 1)
        {
            var baseLength = profile.DataLength > 0 ? profile.DataLength : minLength;
            var length = Math.Max(minLength, (int)Math.Round(baseLength * scale, MidpointRounding.AwayFromZero));
            return BenchmarkDataFactory.CreateString(length, seed);
        }

        private static string CreateNumericPayload(ScenarioProfile profile, int minLength = 1)
        {
            var baseLength = profile.DataLength > 0 ? profile.DataLength : minLength;
            return BenchmarkDataFactory.CreateNumericString(Math.Max(minLength, baseLength));
        }

        private static string CreateDelimitedString(ScenarioProfile profile, char delimiter = '-')
        {
            var segments = Math.Max(1, profile.CollectionLength);
            var segmentLength = Math.Max(1, profile.DataLength / segments);
            var builder = new StringBuilder(segments * (segmentLength + 1));
            for (int i = 0; i < segments; i++)
            {
                if (i > 0)
                    builder.Append(delimiter);
                builder.Append(BenchmarkDataFactory.CreateString(segmentLength, (char)('a' + (i % 26))));
            }
            if (builder.Length == 0)
                return BenchmarkDataFactory.CreateString(Math.Max(1, profile.DataLength), 'a');
            return builder.ToString();
        }

        private static string CreateJsonPayload(ScenarioProfile profile)
        {
            var value = Math.Max(1, profile.DataLength);
            return $"{{\"value\":{value}}}";
        }

        private static readonly (NefFile Nef, ContractManifest Manifest, byte[] NefBytes, byte[] ManifestBytes) s_updateTargetDocument =
            CreateContractDocument(UpdateContractName, BuildSimpleScript(builder => builder.Push(2)));
        private static readonly byte[] s_updateTargetNefBytes = s_updateTargetDocument.NefBytes;
        private static readonly byte[] s_updateTargetManifestBytes = s_updateTargetDocument.ManifestBytes;

        private static readonly ContractArtifact s_deployContractArtifact =
            CreateContractArtifact("BenchmarkContract.Deploy", BuildSimpleScript(builder => builder.Push(true)), 0x5000);

        private static readonly ContractArtifact s_updateContractArtifact =
            CreateContractArtifact(UpdateContractName,
                BuildContractManagementInvocationScript("update", CallFlags.States | CallFlags.AllowNotify, EmitContractUpdateArguments),
                0x5001);

        private static readonly ContractArtifact s_destroyContractArtifact =
            CreateContractArtifact("BenchmarkContract.Destroy",
                BuildContractManagementInvocationScript("destroy", CallFlags.States | CallFlags.AllowNotify, null),
                0x5002);

        private static readonly ContractArtifact s_oracleCallbackContract =
            CreateContractArtifact("BenchmarkContract.OracleCallback", BuildSimpleScript(builder => { }), 0x5003);

        private static readonly UInt160 s_neoSenderAccount = UInt160.Parse("0xaabbccddeeff0011223344556677889900aabbcc");
        private static readonly UInt160 s_neoRecipientAccount = UInt160.Parse("0x11223344556677889900aabbccddeeff00112233");
        private static readonly ECPoint s_candidatePublicKey = s_standbyValidators.Count > 0
            ? s_standbyValidators[0]
            : s_standbyCommittee.Count > 0
                ? s_standbyCommittee[0]
                : ECCurve.Secp256r1.G;
        private static readonly UInt160 s_candidateAccount = Contract.CreateSignatureRedeemScript(s_candidatePublicKey).ToScriptHash();
        private static readonly UInt160 s_notaryAccount = UInt160.Parse("0x44556677889900aabbccddeeff00112233445566");
        private static readonly UInt160 s_notaryRecipientAccount = UInt160.Parse("0x556677889900aabbccddeeff0011223344556677");
        private static readonly uint s_notaryInitialTill = 100u;
        private static readonly uint s_notaryLockTargetTill = 200u;
        private static readonly byte[] s_notaryDummySignature = new byte[64];
        private static readonly uint s_roleDesignationIndex = 1u;
        private static readonly ECPoint[] s_roleDesignationNodes = s_standbyValidators.Take(1).ToArray();
        private const string OracleSampleUrl = "https://example.com/api";
        private const string OracleSampleFilter = "*";
        private const string OracleSampleCallback = "main";
        private static readonly byte[] s_oracleSampleResult = System.Array.Empty<byte>();
        private static readonly ulong s_oracleRequestId = 1uL;

        private static readonly PropertyInfo? s_nativeCallingScriptHashProperty = typeof(ExecutionContextState).GetProperty("NativeCallingScriptHash", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Type? s_candidateStateType = typeof(NeoToken).GetNestedType("CandidateState", BindingFlags.NonPublic);
        private static readonly FieldInfo? s_candidateRegisteredField = s_candidateStateType?.GetField("Registered", BindingFlags.Public | BindingFlags.Instance);
        private static readonly FieldInfo? s_candidateVotesField = s_candidateStateType?.GetField("Votes", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo? s_storageItemGetInteroperable = typeof(StorageItem).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m => m.IsGenericMethodDefinition && m.Name == "GetInteroperable" && m.GetParameters().Length == 0);
        private static readonly Type? s_roleNodeListType = typeof(RoleManagement).GetNestedType("NodeList", BindingFlags.NonPublic);
        private static readonly MethodInfo? s_nodeListAddRangeMethod = s_roleNodeListType?.GetMethod("AddRange", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo? s_nodeListSortMethod = s_roleNodeListType?.GetMethod("Sort", BindingFlags.Public | BindingFlags.Instance);
        private static readonly Type? s_oracleIdListType = typeof(OracleContract).GetNestedType("IdList", BindingFlags.NonPublic);
        private static readonly MethodInfo? s_idListAddRangeMethod = s_oracleIdListType?.GetMethod("AddRange", BindingFlags.Public | BindingFlags.Instance);

        private static NativeMethod CreateNativeMethod(
            NativeContract contract,
            string methodName,
            CallFlags flags,
            Action<InstructionBuilder, ScenarioProfile>? emitArguments = null,
            bool dropResult = true,
            Func<ScenarioProfile, BenchmarkApplicationEngine>? engineFactory = null,
            Action<BenchmarkApplicationEngine, ScenarioProfile>? configureEngine = null,
            Func<ScenarioProfile, ScenarioProfile>? profileFactory = null,
            Func<NativeMethod, ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet>? scriptFactory = null)
        {
            return new NativeMethod($"{contract.Name}:{methodName}", contract.Hash, methodName, flags, emitArguments, dropResult, engineFactory, configureEngine, profileFactory, scriptFactory);
        }
        private static readonly IReadOnlyList<NativeMethod> Methods = new List<NativeMethod>
        {
            CreateNativeMethod(NativeContract.Policy, "getExecFeeFactor", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.Policy, "getStoragePrice", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.Policy, "getFeePerByte", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.Policy, "getMillisecondsPerBlock", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.Policy, "getMaxValidUntilBlockIncrement", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.Policy, "getMaxTraceableBlocks", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.Policy, "getAttributeFee", CallFlags.ReadStates, EmitPolicyAttributeFeeArguments),
            CreateNativeMethod(NativeContract.Policy, "getBlockedAccounts", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.Policy, "isBlocked", CallFlags.ReadStates, EmitPolicyIsBlockedArguments),
            CreateNativeMethod(NativeContract.Ledger, "currentIndex", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.Ledger, "currentHash", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.Ledger, "getBlock", CallFlags.ReadStates, EmitLedgerGetBlockArguments, dropResult: true),
            CreateNativeMethod(NativeContract.Ledger, "getTransaction", CallFlags.ReadStates, EmitLedgerGetTransactionArguments, dropResult: true),
            CreateNativeMethod(NativeContract.Ledger, "getTransactionFromBlock", CallFlags.ReadStates, EmitLedgerGetTransactionFromBlockArguments, dropResult: true),
            CreateNativeMethod(NativeContract.Ledger, "getTransactionHeight", CallFlags.ReadStates, EmitLedgerGetTransactionHeightArguments, dropResult: true),
            CreateNativeMethod(NativeContract.Ledger, "getTransactionSigners", CallFlags.ReadStates, EmitLedgerGetTransactionSignersArguments, dropResult: true),
            CreateNativeMethod(NativeContract.Ledger, "getTransactionVMState", CallFlags.ReadStates, EmitLedgerGetTransactionVmStateArguments, dropResult: true),
            CreateNativeMethod(NativeContract.ContractManagement, "getContract", CallFlags.ReadStates, EmitContractGetArguments),
            CreateNativeMethod(NativeContract.ContractManagement, "getContractById", CallFlags.ReadStates, EmitContractGetByIdArguments),
            CreateNativeMethod(NativeContract.ContractManagement, "getContractHashes", CallFlags.ReadStates, dropResult: true),
            CreateNativeMethod(NativeContract.ContractManagement, "getMinimumDeploymentFee", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.ContractManagement, "hasMethod", CallFlags.ReadStates, EmitContractHasMethodArguments),
            CreateNativeMethod(NativeContract.ContractManagement, "isContract", CallFlags.ReadStates, EmitContractIsContractArguments),
            CreateNativeMethod(
                NativeContract.ContractManagement,
                "deploy",
                CallFlags.States | CallFlags.AllowNotify,
                EmitContractDeployArguments,
                dropResult: true,
                engineFactory: CreateContractDeploymentEngine,
                profileFactory: _ => new ScenarioProfile(1, s_deployContractArtifact.NefBytes.Length + s_deployContractArtifact.ManifestBytes.Length, 1),
                scriptFactory: (method, profile) => CreateSingleCallScriptSet(method, profile, s_setCallFlagsAll)),
            CreateNativeMethod(
                NativeContract.ContractManagement,
                "update",
                CallFlags.States | CallFlags.AllowNotify,
                EmitContractUpdateArguments,
                dropResult: false,
                configureEngine: ConfigureContractUpdateEngine,
                profileFactory: _ => new ScenarioProfile(1, s_updateTargetNefBytes.Length + s_updateTargetManifestBytes.Length, 1),
                scriptFactory: (method, profile) => CreateContractSelfCallScriptSet(method, profile, s_updateContractArtifact.State)),
            CreateNativeMethod(
                NativeContract.ContractManagement,
                "destroy",
                CallFlags.States | CallFlags.AllowNotify,
                emitArguments: null,
                dropResult: false,
                configureEngine: ConfigureContractDestroyEngine,
                profileFactory: _ => new ScenarioProfile(1, 0, 0),
                scriptFactory: (method, profile) => CreateContractSelfCallScriptSet(method, profile, s_destroyContractArtifact.State)),
            CreateNativeMethod(NativeContract.ContractManagement, "setMinimumDeploymentFee", CallFlags.States, EmitContractSetMinimumDeploymentFeeArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine),
            CreateNativeMethod(NativeContract.Policy, "setFeePerByte", CallFlags.All, EmitPolicySetFeePerByteArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine),
            CreateNativeMethod(NativeContract.Policy, "setExecFeeFactor", CallFlags.All, EmitPolicySetExecFeeFactorArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine),
            CreateNativeMethod(NativeContract.Policy, "setStoragePrice", CallFlags.All, EmitPolicySetStoragePriceArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine),
            CreateNativeMethod(NativeContract.Policy, "setMillisecondsPerBlock", CallFlags.All, EmitPolicySetMillisecondsPerBlockArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine),
            CreateNativeMethod(NativeContract.Policy, "setMaxValidUntilBlockIncrement", CallFlags.All, EmitPolicySetMaxValidUntilBlockIncrementArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine),
            CreateNativeMethod(NativeContract.Policy, "setMaxTraceableBlocks", CallFlags.All, EmitPolicySetMaxTraceableBlocksArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine),
            CreateNativeMethod(NativeContract.Policy, "setAttributeFee", CallFlags.All, EmitPolicySetAttributeFeeArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine),
            CreateNativeMethod(NativeContract.Policy, "blockAccount", CallFlags.States, EmitPolicyBlockAccountArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine),
            CreateNativeMethod(NativeContract.Policy, "unblockAccount", CallFlags.States, EmitPolicyBlockAccountArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine, configureEngine: ConfigurePolicyUnblockEngine),
            CreateNativeMethod(NativeContract.NEO, "totalSupply", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.NEO, "balanceOf", CallFlags.ReadStates, EmitCommitteeAccountArguments),
            CreateNativeMethod(NativeContract.NEO, "decimals", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.NEO, "symbol", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.NEO, "getAccountState", CallFlags.ReadStates, EmitNeoAccountStateArguments),
            CreateNativeMethod(NativeContract.NEO, "getCandidateVote", CallFlags.ReadStates, EmitNeoCandidateVoteArguments),
            CreateNativeMethod(NativeContract.NEO, "getCandidates", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.NEO, "getAllCandidates", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.NEO, "getCommittee", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.NEO, "getCommitteeAddress", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.NEO, "getGasPerBlock", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.NEO, "getRegisterPrice", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.NEO, "getNextBlockValidators", CallFlags.ReadStates, EmitNeoNextBlockValidatorsArguments),
            CreateNativeMethod(NativeContract.NEO, "unclaimedGas", CallFlags.ReadStates, EmitNeoUnclaimedGasArguments),
            CreateNativeMethod(
                NativeContract.NEO,
                "onNEP17Payment",
                CallFlags.States | CallFlags.AllowNotify,
                EmitNeoOnPaymentArguments,
                dropResult: false,
                engineFactory: CreateNeoOnPaymentEngine,
                configureEngine: ConfigureNeoOnPaymentEngine,
                profileFactory: _ => new ScenarioProfile(1, 96, 1),
                scriptFactory: (method, profile) => CreateNeoScriptSet(method, profile, state => SetNativeCallingScriptHash(state, NativeContract.GAS.Hash))),
            CreateNativeMethod(
                NativeContract.NEO,
                "registerCandidate",
                CallFlags.States | CallFlags.AllowNotify,
                EmitNeoRegisterCandidateArguments,
                dropResult: false,
                engineFactory: CreateNeoCandidateEngine,
                configureEngine: ConfigureNeoRegisterCandidateEngine,
                profileFactory: _ => new ScenarioProfile(1, 64, 1),
                scriptFactory: (method, profile) => CreateNeoScriptSet(method, profile)),
            CreateNativeMethod(
                NativeContract.NEO,
                "setGasPerBlock",
                CallFlags.States,
                EmitNeoSetGasPerBlockArguments,
                dropResult: true,
                engineFactory: CreateCommitteeWitnessEngine,
                configureEngine: ConfigureNeoSetGasPerBlockEngine,
                profileFactory: _ => new ScenarioProfile(1, 16, 1),
                scriptFactory: (method, profile) => CreateNeoScriptSet(method, profile)),
            CreateNativeMethod(
                NativeContract.NEO,
                "setRegisterPrice",
                CallFlags.States,
                EmitNeoSetRegisterPriceArguments,
                dropResult: true,
                engineFactory: CreateCommitteeWitnessEngine,
                configureEngine: ConfigureNeoSetRegisterPriceEngine,
                profileFactory: _ => new ScenarioProfile(1, 16, 1),
                scriptFactory: (method, profile) => CreateNeoScriptSet(method, profile)),
            CreateNativeMethod(
                NativeContract.NEO,
                "transfer",
                CallFlags.All,
                EmitNeoTransferArguments,
                dropResult: false,
                engineFactory: CreateNeoTransferEngine,
                configureEngine: ConfigureNeoTransferEngine,
                profileFactory: _ => new ScenarioProfile(1, 128, 4),
                scriptFactory: (method, profile) => CreateNeoScriptSet(method, profile)),
            CreateNativeMethod(
                NativeContract.NEO,
                "unregisterCandidate",
                CallFlags.States | CallFlags.AllowNotify,
                EmitNeoUnregisterCandidateArguments,
                dropResult: false,
                engineFactory: CreateNeoCandidateEngine,
                configureEngine: ConfigureNeoUnregisterCandidateEngine,
                profileFactory: _ => new ScenarioProfile(1, 64, 1),
                scriptFactory: (method, profile) => CreateNeoScriptSet(method, profile)),
            CreateNativeMethod(
                NativeContract.NEO,
                "vote",
                CallFlags.States | CallFlags.AllowNotify,
                EmitNeoVoteArguments,
                dropResult: false,
                engineFactory: CreateNeoVoteEngine,
                configureEngine: ConfigureNeoVoteEngine,
                profileFactory: _ => new ScenarioProfile(1, 96, 1),
                scriptFactory: (method, profile) => CreateNeoScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.GAS, "totalSupply", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.GAS, "balanceOf", CallFlags.ReadStates, EmitCommitteeAccountArguments),
            CreateNativeMethod(NativeContract.GAS, "decimals", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.GAS, "symbol", CallFlags.ReadStates),
            CreateNativeMethod(NativeContract.GAS, "transfer", CallFlags.All, EmitGasTransferArguments, dropResult: true, engineFactory: CreateGasTransferEngine),
            CreateNativeMethod(NativeContract.Notary, "balanceOf", CallFlags.ReadStates, EmitNotaryBalanceArguments, dropResult: true, engineFactory: _ => NativeBenchmarkStateFactory.CreateEngine(), configureEngine: ConfigureNotaryBalanceEngine, profileFactory: _ => new ScenarioProfile(1, 32, 1), scriptFactory: (method, profile) => CreateNotaryScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Notary, "expirationOf", CallFlags.ReadStates, EmitNotaryExpirationArguments, dropResult: true, engineFactory: _ => NativeBenchmarkStateFactory.CreateEngine(), configureEngine: ConfigureNotaryExpirationEngine, profileFactory: _ => new ScenarioProfile(1, 32, 1), scriptFactory: (method, profile) => CreateNotaryScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Notary, "getMaxNotValidBeforeDelta", CallFlags.ReadStates, dropResult: true, engineFactory: _ => NativeBenchmarkStateFactory.CreateEngine(), configureEngine: ConfigureNotarySetMaxDeltaEngine, profileFactory: _ => new ScenarioProfile(1, 0, 0), scriptFactory: (method, profile) => CreateNotaryScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Notary, "lockDepositUntil", CallFlags.States, EmitNotaryLockDepositUntilArguments, dropResult: true, engineFactory: CreateNotaryAccountEngine, configureEngine: ConfigureNotaryLockDepositEngine, profileFactory: _ => new ScenarioProfile(1, 48, 1), scriptFactory: (method, profile) => CreateNotaryScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Notary, "onNEP17Payment", CallFlags.States, EmitNotaryOnPaymentArguments, dropResult: true, engineFactory: CreateNotaryOnPaymentEngine, configureEngine: ConfigureNotaryOnPaymentEngine, profileFactory: _ => new ScenarioProfile(1, 96, 1), scriptFactory: (method, profile) => CreateNotaryScriptSet(method, profile, state => SetNativeCallingScriptHash(state, NativeContract.GAS.Hash))),
            CreateNativeMethod(NativeContract.Notary, "setMaxNotValidBeforeDelta", CallFlags.States, EmitNotarySetMaxDeltaArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine, configureEngine: ConfigureNotarySetMaxDeltaEngine, profileFactory: _ => new ScenarioProfile(1, 16, 1), scriptFactory: (method, profile) => CreateNotaryScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Notary, "verify", CallFlags.ReadStates, EmitNotaryVerifyArguments, dropResult: true, engineFactory: CreateNotaryVerifyEngine, configureEngine: ConfigureNotaryVerifyEngine, profileFactory: _ => new ScenarioProfile(1, 64, 1), scriptFactory: (method, profile) => CreateNotaryScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Notary, "withdraw", CallFlags.All, EmitNotaryWithdrawArguments, dropResult: true, engineFactory: CreateNotaryWithdrawEngine, configureEngine: ConfigureNotaryWithdrawEngine, profileFactory: _ => new ScenarioProfile(1, 64, 1), scriptFactory: (method, profile) => CreateNotaryScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Oracle, "getPrice", CallFlags.ReadStates, dropResult: true, engineFactory: _ => NativeBenchmarkStateFactory.CreateEngine(), configureEngine: ConfigureOracleGetPriceEngine, profileFactory: _ => new ScenarioProfile(1, 0, 0), scriptFactory: (method, profile) => CreateOracleScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Oracle, "setPrice", CallFlags.States, EmitOracleSetPriceArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine, configureEngine: ConfigureOracleSetPriceEngine, profileFactory: _ => new ScenarioProfile(1, 16, 1), scriptFactory: (method, profile) => CreateOracleScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Oracle, "request", CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify, EmitOracleRequestArguments, dropResult: true, engineFactory: CreateOracleRequestEngine, configureEngine: ConfigureOracleRequestEngine, profileFactory: _ => new ScenarioProfile(1, 256, 1), scriptFactory: (method, profile) => CreateOracleScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Oracle, "finish", CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify, dropResult: true, engineFactory: CreateOracleFinishEngine, configureEngine: ConfigureOracleFinishEngine, profileFactory: _ => new ScenarioProfile(1, 0, 0), scriptFactory: (method, profile) => CreateOracleScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.Oracle, "verify", CallFlags.ReadStates, dropResult: true, engineFactory: CreateOracleVerifyEngine, configureEngine: ConfigureOracleVerifyEngine, profileFactory: _ => new ScenarioProfile(1, 0, 0), scriptFactory: (method, profile) => CreateOracleScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.CryptoLib, "keccak256", CallFlags.All, EmitCryptoHashArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "murmur32", CallFlags.All, EmitCryptoMurmurArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "ripemd160", CallFlags.All, EmitCryptoHashArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "sha256", CallFlags.All, EmitCryptoHashArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "bls12381Deserialize", CallFlags.All, EmitCryptoBlsDeserializeArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "bls12381Serialize", CallFlags.All, EmitCryptoBlsSerializeArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "bls12381Add", CallFlags.All, EmitCryptoBlsAddArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "bls12381Equal", CallFlags.All, EmitCryptoBlsEqualArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "bls12381Mul", CallFlags.All, EmitCryptoBlsMulArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "bls12381Pairing", CallFlags.All, EmitCryptoBlsPairingArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "recoverSecp256K1", CallFlags.All, EmitCryptoRecoverSecpArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "verifyWithECDsa", CallFlags.All, EmitCryptoVerifyEcdsaArguments),
            CreateNativeMethod(NativeContract.CryptoLib, "verifyWithEd25519", CallFlags.All, EmitCryptoVerifyEd25519Arguments),
            CreateNativeMethod(NativeContract.StdLib, "base58CheckDecode", CallFlags.All, EmitStdLibBase58CheckDecodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "base58CheckEncode", CallFlags.All, EmitStdLibBase58CheckEncodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "base58Decode", CallFlags.All, EmitStdLibBase58DecodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "base58Encode", CallFlags.All, EmitStdLibBase58EncodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "base64Decode", CallFlags.All, EmitStdLibBase64DecodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "base64Encode", CallFlags.All, EmitStdLibBase64EncodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "base64UrlDecode", CallFlags.All, EmitStdLibBase64UrlDecodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "base64UrlEncode", CallFlags.All, EmitStdLibBase64UrlEncodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "hexDecode", CallFlags.All, EmitStdLibHexDecodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "hexEncode", CallFlags.All, EmitStdLibHexEncodeArguments),
            CreateNativeMethod(NativeContract.StdLib, "memoryCompare", CallFlags.All, EmitStdLibMemoryCompareArguments),
            CreateNativeMethod(NativeContract.StdLib, "memorySearch", CallFlags.All, EmitStdLibMemorySearchArguments),
            CreateNativeMethod(NativeContract.StdLib, "stringSplit", CallFlags.All, EmitStdLibStringSplitArguments),
            CreateNativeMethod(NativeContract.StdLib, "atoi", CallFlags.All, EmitStdLibAtoiArguments),
            CreateNativeMethod(NativeContract.StdLib, "itoa", CallFlags.All, EmitStdLibItoaArguments),
            CreateNativeMethod(NativeContract.StdLib, "serialize", CallFlags.All, EmitStdLibSerializeArguments),
            CreateNativeMethod(NativeContract.StdLib, "deserialize", CallFlags.All, EmitStdLibDeserializeArguments),
            CreateNativeMethod(NativeContract.StdLib, "jsonSerialize", CallFlags.All, EmitStdLibJsonSerializeArguments),
            CreateNativeMethod(NativeContract.StdLib, "jsonDeserialize", CallFlags.All, EmitStdLibJsonDeserializeArguments),
            CreateNativeMethod(NativeContract.StdLib, "strLen", CallFlags.All, EmitStdLibStrLenArguments),
            CreateNativeMethod(NativeContract.RoleManagement, "designateAsRole", CallFlags.States | CallFlags.AllowNotify, EmitRoleDesignateArguments, dropResult: true, engineFactory: CreateCommitteeWitnessEngine, configureEngine: ConfigureRoleDesignateEngine, profileFactory: _ => new ScenarioProfile(1, 96, 1), scriptFactory: (method, profile) => CreateRoleScriptSet(method, profile)),
            CreateNativeMethod(NativeContract.RoleManagement, "getDesignatedByRole", CallFlags.ReadStates, EmitRoleGetDesignatedArguments, dropResult: true, engineFactory: _ => NativeBenchmarkStateFactory.CreateEngine(), configureEngine: ConfigureRoleGetDesignatedEngine, profileFactory: _ => new ScenarioProfile(1, 32, 1), scriptFactory: (method, profile) => CreateRoleScriptSet(method, profile))
        };


        public static IEnumerable<VmBenchmarkCase> CreateCases()
        {
            foreach (ScenarioComplexity complexity in Enum.GetValues<ScenarioComplexity>())
            {
                foreach (var method in Methods)
                {
                    yield return BuildCase(method, complexity);
                }
            }
        }

        private static VmBenchmarkCase BuildCase(NativeMethod method, ScenarioComplexity complexity)
        {
            var scenario = new ApplicationEngineVmScenario(
                method.ScriptFactory is null
                    ? CreateScripts(method)
                    : profile => method.ScriptFactory(method, profile),
                method.ConfigureEngine,
                profile => method.EngineFactory?.Invoke(profile) ?? NativeBenchmarkStateFactory.CreateEngine());

            var baseProfile = ScenarioProfile.For(complexity);
            var effectiveProfile = method.ProfileFactory?.Invoke(baseProfile) ?? baseProfile;

            NativeCoverageTracker.Register(method.Id);
            return new VmBenchmarkCase(method.Id, BenchmarkComponent.NativeContract, complexity, scenario, effectiveProfile);
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateScripts(NativeMethod method)
        {
            return profile =>
            {
                var baselineProfile = profile.With(dataLength: 0, collectionLength: 0);
                var baselineScript = CreateScript(
                    LoopScriptFactory.BuildCountingLoop(profile, builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    }),
                    baselineProfile);

                var singleScript = CreateScript(BuildCallLoop(method, profile), profile);
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 4, profile.DataLength * 2, profile.CollectionLength * 2);
                var saturatedScript = CreateScript(BuildCallLoop(method, saturatedProfile), saturatedProfile);

                return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(baselineScript, singleScript, saturatedScript);
            };
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScriptSet CreateSingleCallScriptSet(
            NativeMethod method,
            ScenarioProfile profile,
            Action<ExecutionContextState>? configureState = null)
        {
            var iterations = Math.Max(1, profile.Iterations);
            var baseline = CreateScript(BuildNoOpScriptBytes(), profile.With(iterations: iterations, dataLength: 0, collectionLength: 0), configureState);
            var singleProfile = profile.With(iterations: iterations);
            var single = CreateScript(BuildSingleCallScriptBytes(method, singleProfile), singleProfile, configureState);
            return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(baseline, single, single);
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScriptSet CreateNeoScriptSet(
            NativeMethod method,
            ScenarioProfile profile,
            Action<ExecutionContextState>? additionalConfiguration = null)
        {
            return CreateSingleCallScriptSet(method, profile, state =>
            {
                additionalConfiguration?.Invoke(state);
                state.ScriptHash = NativeContract.NEO.Hash;
                state.Contract = CloneContractState(NativeContract.NEO.GetContractState(ProtocolSettings.Default, 0));
            });
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScriptSet CreateContractSelfCallScriptSet(
            NativeMethod method,
            ScenarioProfile profile,
            ContractState contract,
            Action<ExecutionContextState>? additionalConfiguration = null)
        {
            var configure = CombineConfigurations(additionalConfiguration, state =>
            {
                state.ScriptHash = contract.Hash;
                state.Contract = CloneContractState(contract);
                state.CallFlags = CallFlags.All;
            });

            return CreateSingleCallScriptSet(method, profile, configure);
        }

        private static Action<ExecutionContextState>? CombineConfigurations(params Action<ExecutionContextState>?[] configurations)
        {
            if (configurations.All(action => action is null))
                return null;

            return state =>
            {
                foreach (var action in configurations)
                    action?.Invoke(state);
            };
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScript CreateScript(
            byte[] script,
            ScenarioProfile profile,
            Action<ExecutionContextState>? configureState = null)
        {
            return new ApplicationEngineVmScenario.ApplicationEngineScript(script, profile, configureState);
        }

        private static void SetNativeCallingScriptHash(ExecutionContextState state, UInt160 hash)
        {
            s_nativeCallingScriptHashProperty?.SetValue(state, hash);
        }

        private static byte[] BuildSingleCallScriptBytes(NativeMethod method, ScenarioProfile profile)
        {
            var builder = new InstructionBuilder();
            if (method.EmitArguments is null)
            {
                builder.AddInstruction(VM.OpCode.NEWARRAY0);
            }
            else
            {
                method.EmitArguments(builder, profile);
            }

            builder.Push((int)method.Flags);
            builder.Push(method.MethodName);
            builder.Push(method.ContractHash.GetSpan().ToArray());
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.SYSCALL,
                _operand = BitConverter.GetBytes(ApplicationEngine.System_Contract_Call.Hash)
            });

            if (method.DropResult)
            {
                builder.AddInstruction(VM.OpCode.DROP);
            }

            builder.AddInstruction(VM.OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildNoOpScriptBytes()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(VM.OpCode.RET);
            return builder.ToArray();
        }

        private static BenchmarkApplicationEngine CreateNeoOnPaymentEngine(ScenarioProfile profile)
        {
            var transaction = new Transaction
            {
                Signers = new[]
                {
                    new Signer
                    {
                        Account = s_candidateAccount,
                        Scopes = WitnessScope.Global
                    }
                },
                Attributes = System.Array.Empty<TransactionAttribute>(),
                Witnesses = System.Array.Empty<Witness>(),
                Script = System.Array.Empty<byte>()
            };

            return NativeBenchmarkStateFactory.CreateEngine(transaction);
        }

        private static BenchmarkApplicationEngine CreateNeoCandidateEngine(ScenarioProfile profile)
        {
            return NativeBenchmarkStateFactory.CreateEngine(new ManualWitness(s_candidateAccount));
        }

        private static BenchmarkApplicationEngine CreateNeoTransferEngine(ScenarioProfile profile)
        {
            return NativeBenchmarkStateFactory.CreateEngine(new ManualWitness(s_neoSenderAccount));
        }

        private static BenchmarkApplicationEngine CreateNeoVoteEngine(ScenarioProfile profile)
        {
            return NativeBenchmarkStateFactory.CreateEngine(new ManualWitness(s_neoSenderAccount));
        }

        private static void ConfigureNeoOnPaymentEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.NEO);
            SeedNeoRegisterPrice(engine.SnapshotCache, s_defaultRegisterPrice);
            SeedNeoAccount(engine.SnapshotCache, s_candidateAccount, 0);
            SeedNeoCandidate(engine.SnapshotCache, s_candidatePublicKey, registered: false, votes: BigInteger.Zero);
            var depositAmount = new BigInteger(PolicyContract.DefaultNotaryAssistedAttributeFee) * 2;
            SeedGasAccount(engine.SnapshotCache, NativeContract.NEO.Hash, depositAmount);
        }

        private static void ConfigureNeoRegisterCandidateEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.NEO);
            SeedNeoRegisterPrice(engine.SnapshotCache, s_defaultRegisterPrice);
            SeedNeoCandidate(engine.SnapshotCache, s_candidatePublicKey, registered: false, votes: BigInteger.Zero);
            SeedNeoAccount(engine.SnapshotCache, s_candidateAccount, NativeContract.NEO.Factor);
        }

        private static void ConfigureNeoSetGasPerBlockEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.NEO);
            SeedNeoRegisterPrice(engine.SnapshotCache, s_defaultRegisterPrice);
        }

        private static void ConfigureNeoSetRegisterPriceEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.NEO);
            SeedNeoRegisterPrice(engine.SnapshotCache, s_defaultRegisterPrice);
        }

        private static void ConfigureNeoTransferEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.NEO);
            var balance = 10 * NativeContract.NEO.Factor;
            SeedNeoAccount(engine.SnapshotCache, s_neoSenderAccount, balance);
            SeedNeoAccount(engine.SnapshotCache, s_neoRecipientAccount, 0);
        }

        private static void ConfigureNeoUnregisterCandidateEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.NEO);
            SeedNeoRegisterPrice(engine.SnapshotCache, s_defaultRegisterPrice);
            SeedNeoCandidate(engine.SnapshotCache, s_candidatePublicKey, registered: true, votes: BigInteger.Zero);
        }

        private static void ConfigureNeoVoteEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.NEO);
            SeedNeoRegisterPrice(engine.SnapshotCache, s_defaultRegisterPrice);
            SeedNeoVotersCount(engine.SnapshotCache);
            SeedNeoCandidate(engine.SnapshotCache, s_candidatePublicKey, registered: true, votes: BigInteger.Zero);
            SeedNeoAccount(engine.SnapshotCache, s_neoSenderAccount, 5 * NativeContract.NEO.Factor, balanceHeight: 0, voteTo: null);
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScriptSet CreateNotaryScriptSet(
            NativeMethod method,
            ScenarioProfile profile,
            Action<ExecutionContextState>? additionalConfiguration = null)
        {
            return CreateSingleCallScriptSet(method, profile, state =>
            {
                additionalConfiguration?.Invoke(state);
                state.ScriptHash = NativeContract.Notary.Hash;
                state.Contract = CloneContractState(NativeContract.Notary.GetContractState(ProtocolSettings.Default, 0));
            });
        }

        private static BenchmarkApplicationEngine CreateNotaryOnPaymentEngine(ScenarioProfile profile)
        {
            var transaction = new Transaction
            {
                Signers = new[]
                {
                    new Signer
                    {
                        Account = s_notaryAccount,
                        Scopes = WitnessScope.Global
                    }
                },
                Attributes = System.Array.Empty<TransactionAttribute>(),
                Witnesses = System.Array.Empty<Witness>(),
                Script = System.Array.Empty<byte>()
            };

            return NativeBenchmarkStateFactory.CreateEngine(transaction);
        }

        private static BenchmarkApplicationEngine CreateNotaryAccountEngine(ScenarioProfile profile)
        {
            return NativeBenchmarkStateFactory.CreateEngine(new ManualWitness(s_notaryAccount));
        }

        private static BenchmarkApplicationEngine CreateNotaryVerifyEngine(ScenarioProfile profile)
        {
            var transaction = new Transaction
            {
                Signers = new[] { new Signer { Account = s_notaryAccount, Scopes = WitnessScope.Global } },
                Attributes = new TransactionAttribute[]
                {
                    new NotaryAssisted { NKeys = 0 }
                },
                Witnesses = System.Array.Empty<Witness>(),
                Script = System.Array.Empty<byte>()
            };

            return NativeBenchmarkStateFactory.CreateEngine(transaction);
        }

        private static BenchmarkApplicationEngine CreateNotaryWithdrawEngine(ScenarioProfile profile)
        {
            return NativeBenchmarkStateFactory.CreateEngine(new ManualWitness(s_notaryAccount));
        }

        private static void ConfigureNotaryBalanceEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Notary);
            SeedNotaryDeposit(engine.SnapshotCache, s_notaryAccount, new BigInteger(500_00000000), s_notaryInitialTill);
        }

        private static void ConfigureNotaryExpirationEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Notary);
            SeedNotaryDeposit(engine.SnapshotCache, s_notaryAccount, new BigInteger(500_00000000), s_notaryInitialTill);
        }

        private static void ConfigureNotarySetMaxDeltaEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Notary);
            SeedNotaryMaxDelta(engine.SnapshotCache, NotaryDefaultMaxNotValidBeforeDelta);
        }

        private static void ConfigureNotaryLockDepositEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Notary);
            SeedNotaryDeposit(engine.SnapshotCache, s_notaryAccount, new BigInteger(500_00000000), s_notaryInitialTill);
        }

        private static void ConfigureNotaryOnPaymentEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Notary);
            SeedNotaryMaxDelta(engine.SnapshotCache, NotaryDefaultMaxNotValidBeforeDelta);
            ClearNotaryDeposit(engine.SnapshotCache, s_notaryAccount);
        }

        private static void ConfigureNotaryVerifyEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Notary);
            SeedNotaryMaxDelta(engine.SnapshotCache, NotaryDefaultMaxNotValidBeforeDelta);
        }

        private static void ConfigureNotaryWithdrawEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Notary);
            SeedNotaryDeposit(engine.SnapshotCache, s_notaryAccount, new BigInteger(800_00000000), till: 0);
            SeedGasAccount(engine.SnapshotCache, NativeContract.Notary.Hash, new BigInteger(800_00000000));
        }

        private static void SeedNotaryDeposit(DataCache snapshot, UInt160 account, BigInteger amount, uint till)
        {
            var key = StorageKey.Create(NativeContract.Notary.Id, NotaryDepositPrefix, account);
            snapshot.Delete(key);
            var deposit = new Notary.Deposit
            {
                Amount = amount,
                Till = till
            };
            snapshot.Add(key, new StorageItem(deposit));
        }

        private static void SeedNotaryMaxDelta(DataCache snapshot, uint value)
        {
            var key = StorageKey.Create(NativeContract.Notary.Id, NotaryMaxNotValidBeforeDeltaPrefix);
            var item = snapshot.GetAndChange(key, () => new StorageItem(BigInteger.Zero));
            item.Set(value);
        }

        private static void ClearNotaryDeposit(DataCache snapshot, UInt160 account)
        {
            var key = StorageKey.Create(NativeContract.Notary.Id, NotaryDepositPrefix, account);
            snapshot.Delete(key);
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScriptSet CreateOracleScriptSet(
            NativeMethod method,
            ScenarioProfile profile,
            Action<ExecutionContextState>? additionalConfiguration = null)
        {
            return CreateSingleCallScriptSet(method, profile, state =>
            {
                additionalConfiguration?.Invoke(state);
                state.ScriptHash = NativeContract.Oracle.Hash;
                state.Contract = CloneContractState(NativeContract.Oracle.GetContractState(ProtocolSettings.Default, 0));
            });
        }

        private static BenchmarkApplicationEngine CreateOracleRequestEngine(ScenarioProfile profile)
        {
            var transaction = CreateOracleRequestTransaction(s_oracleCallbackContract.State.Hash);
            return NativeBenchmarkStateFactory.CreateEngine(transaction);
        }

        private static BenchmarkApplicationEngine CreateOracleFinishEngine(ScenarioProfile profile)
        {
            var response = new OracleResponse
            {
                Id = s_oracleRequestId,
                Code = OracleResponseCode.Success,
                Result = s_oracleSampleResult
            };
            var tx = CreateOracleResponseTransaction(response);
            return NativeBenchmarkStateFactory.CreateEngine(tx);
        }

        private static BenchmarkApplicationEngine CreateOracleVerifyEngine(ScenarioProfile profile)
        {
            var response = new OracleResponse
            {
                Id = s_oracleRequestId,
                Code = OracleResponseCode.Success,
                Result = s_oracleSampleResult
            };
            var tx = CreateOracleResponseTransaction(response);
            return NativeBenchmarkStateFactory.CreateEngine(tx);
        }

        private static void ConfigureOracleGetPriceEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Oracle);
            SeedOraclePrice(engine.SnapshotCache, 500_000000);
        }

        private static void ConfigureOracleSetPriceEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Oracle);
            SeedOraclePrice(engine.SnapshotCache, 500_000000);
        }

        private static void ConfigureOracleRequestEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Oracle);
            EnsureContract(engine, s_oracleCallbackContract.State);
            SeedOraclePrice(engine.SnapshotCache, 500_000000);
            SeedOracleRequestId(engine.SnapshotCache, s_oracleRequestId);
        }

        private static void ConfigureOracleFinishEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Oracle);
            EnsureContract(engine, s_oracleCallbackContract.State);
            SeedOraclePrice(engine.SnapshotCache, 500_000000);
            SeedOracleRequest(engine.SnapshotCache, s_oracleRequestId, CreateOracleRequestPayload());
            SeedOracleIdList(engine.SnapshotCache, s_oracleRequestId, OracleSampleUrl);
            SeedOracleRequestId(engine.SnapshotCache, s_oracleRequestId + 1);
        }

        private static void ConfigureOracleVerifyEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.Oracle);
            ConfigureOracleFinishEngine(engine, profile);
        }

        private static void SeedOraclePrice(DataCache snapshot, long price)
        {
            var key = StorageKey.Create(NativeContract.Oracle.Id, OraclePricePrefix);
            var item = snapshot.GetAndChange(key, () => new StorageItem(BigInteger.Zero));
            item.Set(price);
        }

        private static void SeedOracleRequestId(DataCache snapshot, ulong nextId)
        {
            var key = StorageKey.Create(NativeContract.Oracle.Id, OracleRequestIdPrefix);
            var item = snapshot.GetAndChange(key, () => new StorageItem(BigInteger.Zero));
            item.Set(new BigInteger(nextId));
        }

        private static void SeedOracleRequest(DataCache snapshot, ulong id, OracleRequest request)
        {
            var key = StorageKey.Create(NativeContract.Oracle.Id, OracleRequestPrefix, (long)id);
            snapshot.Delete(key);
            snapshot.Add(key, StorageItem.CreateSealed(request));
        }

        private static void SeedOracleIdList(DataCache snapshot, ulong id, string url)
        {
            var hash = Crypto.Hash160(url.ToStrictUtf8Bytes());
            var key = StorageKey.Create(NativeContract.Oracle.Id, OracleIdListPrefix, hash.AsSpan());
            snapshot.Delete(key);
            snapshot.Add(key, new StorageItem(CreateOracleIdList(ids: new[] { id })));
        }

        private static OracleRequest CreateOracleRequestPayload()
        {
            return new OracleRequest
            {
                OriginalTxid = UInt256.Zero,
                GasForResponse = 200_0000000,
                Url = OracleSampleUrl,
                Filter = OracleSampleFilter,
                CallbackContract = s_oracleCallbackContract.State.Hash,
                CallbackMethod = OracleSampleCallback,
                UserData = BinarySerializer.Serialize(VMStackItem.Null, OracleMaxUserDataLength, ExecutionEngineLimits.Default.MaxStackSize)
            };
        }

        private static Transaction CreateOracleRequestTransaction(UInt160 caller)
        {
            return new Transaction
            {
                Signers = new[] { new Signer { Account = caller, Scopes = WitnessScope.Global } },
                Attributes = System.Array.Empty<TransactionAttribute>(),
                Witnesses = System.Array.Empty<Witness>(),
                Script = System.Array.Empty<byte>()
            };
        }

        private static Transaction CreateOracleResponseTransaction(OracleResponse response)
        {
            return new Transaction
            {
                Signers = new[] { new Signer { Account = s_oracleCallbackContract.State.Hash, Scopes = WitnessScope.Global } },
                Attributes = new TransactionAttribute[] { response },
                Witnesses = System.Array.Empty<Witness>(),
                Script = OracleResponse.FixedScript,
                NetworkFee = 200_0000000L,
                SystemFee = 0
            };
        }

        private static IInteroperable CreateOracleIdList(IEnumerable<ulong> ids)
        {
            if (s_oracleIdListType is null)
                throw new InvalidOperationException("Oracle IdList type unavailable.");
            var instance = Activator.CreateInstance(s_oracleIdListType);
            if (instance is not IInteroperable interoperable)
                throw new InvalidOperationException("Oracle IdList does not implement IInteroperable.");
            s_idListAddRangeMethod?.Invoke(instance, new object[] { ids });
            return interoperable;
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScriptSet CreateRoleScriptSet(
            NativeMethod method,
            ScenarioProfile profile,
            Action<ExecutionContextState>? additionalConfiguration = null)
        {
            return CreateSingleCallScriptSet(method, profile, state =>
            {
                additionalConfiguration?.Invoke(state);
                state.ScriptHash = NativeContract.RoleManagement.Hash;
                state.Contract = CloneContractState(NativeContract.RoleManagement.GetContractState(ProtocolSettings.Default, 0));
            });
        }

        private static void ConfigureRoleDesignateEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.RoleManagement);
            ClearRoleDesignation(engine.SnapshotCache, Role.Oracle, engine.PersistingBlock.Index + 1);
        }

        private static void ConfigureRoleGetDesignatedEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureNativeContract(engine, NativeContract.RoleManagement);
            SeedRoleDesignation(engine.SnapshotCache, Role.Oracle, s_roleDesignationIndex, s_roleDesignationNodes);
        }

        private static void SeedRoleDesignation(DataCache snapshot, Role role, uint index, IEnumerable<ECPoint> nodes)
        {
            if (s_roleNodeListType is null)
                throw new InvalidOperationException("Role node list type unavailable.");
            var instance = Activator.CreateInstance(s_roleNodeListType);
            if (instance is not IInteroperable interoperable)
                throw new InvalidOperationException("NodeList does not implement IInteroperable.");
            s_nodeListAddRangeMethod?.Invoke(instance, new object[] { nodes });
            s_nodeListSortMethod?.Invoke(instance, System.Array.Empty<object>());

            var key = StorageKey.Create(NativeContract.RoleManagement.Id, (byte)role, index);
            snapshot.Delete(key);
            snapshot.Add(key, new StorageItem(interoperable));
        }

        private static void ClearRoleDesignation(DataCache snapshot, Role role, uint index)
        {
            var key = StorageKey.Create(NativeContract.RoleManagement.Id, (byte)role, index);
            snapshot.Delete(key);
        }

        private static void EnsureNativeContract(BenchmarkApplicationEngine engine, NativeContract contract)
        {
            var contractState = CloneContractState(contract.GetContractState(ProtocolSettings.Default, 0));
            EnsureContract(engine, contractState);
        }

        private static void SeedNeoRegisterPrice(DataCache snapshot, BigInteger price)
        {
            var key = StorageKey.Create(NativeContract.NEO.Id, NeoRegisterPricePrefix);
            var item = snapshot.GetAndChange(key, () => new StorageItem(BigInteger.Zero));
            item.Set(price);
        }

        private static void SeedNeoAccount(DataCache snapshot, UInt160 account, BigInteger balance, uint balanceHeight = 0, ECPoint? voteTo = null, BigInteger? lastGasPerVote = null)
        {
            var key = StorageKey.Create(NativeContract.NEO.Id, NeoAccountPrefix, account);
            var item = snapshot.GetAndChange(key, () => new StorageItem(new NeoToken.NeoAccountState()));
            var state = item.GetInteroperable<NeoToken.NeoAccountState>();
            state.Balance = balance;
            state.BalanceHeight = balanceHeight;
            state.VoteTo = voteTo;
            state.LastGasPerVote = lastGasPerVote ?? BigInteger.Zero;
        }

        private static void SeedNeoCandidate(DataCache snapshot, ECPoint pubKey, bool registered, BigInteger votes)
        {
            if (s_candidateStateType is null || s_candidateRegisteredField is null || s_candidateVotesField is null)
                return;

            var key = StorageKey.Create(NativeContract.NEO.Id, NeoCandidatePrefix, pubKey);
            var item = snapshot.GetAndChange(key, () => new StorageItem(CreateCandidateState(registered, votes)));
            if (s_storageItemGetInteroperable is null)
                return;

            var state = s_storageItemGetInteroperable.MakeGenericMethod(s_candidateStateType)
                .Invoke(item, System.Array.Empty<object>());
            if (state is null)
                return;
            s_candidateRegisteredField.SetValue(state, registered);
            s_candidateVotesField.SetValue(state, votes);
        }

        private static void SeedNeoVotersCount(DataCache snapshot)
        {
            var key = StorageKey.Create(NativeContract.NEO.Id, NeoVotersCountPrefix);
            if (!snapshot.Contains(key))
                snapshot.Add(key, new StorageItem(BigInteger.Zero));
        }

        private static IInteroperable CreateCandidateState(bool registered, BigInteger votes)
        {
            if (s_candidateStateType is null)
                throw new InvalidOperationException("Candidate state type is unavailable.");
            var instance = Activator.CreateInstance(s_candidateStateType);
            if (instance is not IInteroperable interoperable)
                throw new InvalidOperationException("Candidate state does not implement IInteroperable.");
            s_candidateRegisteredField?.SetValue(instance, registered);
            s_candidateVotesField?.SetValue(instance, votes);
            return interoperable;
        }


        private static byte[] BuildCallLoop(NativeMethod method, ScenarioProfile profile)
        {
            return LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                if (method.EmitArguments is null)
                {
                    builder.AddInstruction(VM.OpCode.NEWARRAY0);
                }
                else
                {
                    method.EmitArguments(builder, profile);
                }

                builder.Push((int)method.Flags);
                builder.Push(method.MethodName);
                builder.Push(method.ContractHash.GetSpan().ToArray());
                builder.AddInstruction(new Instruction
                {
                    _opCode = VM.OpCode.SYSCALL,
                    _operand = BitConverter.GetBytes(ApplicationEngine.System_Contract_Call.Hash)
                });

                if (method.DropResult)
                {
                    builder.AddInstruction(VM.OpCode.DROP);
                }
            });
        }

        private static byte[] BuildSimpleScript(Action<InstructionBuilder> emit)
        {
            var builder = new InstructionBuilder();
            emit(builder);
            builder.AddInstruction(VM.OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildContractManagementInvocationScript(
            string methodName,
            CallFlags flags,
            Action<InstructionBuilder, ScenarioProfile>? emitArguments)
        {
            var builder = new InstructionBuilder();

            if (emitArguments is null)
            {
                builder.AddInstruction(VM.OpCode.NEWARRAY0);
            }
            else
            {
                emitArguments(builder, ScenarioProfile.For(ScenarioComplexity.Standard));
            }

            builder.Push((int)flags);
            builder.Push(methodName);
            builder.Push(NativeContract.ContractManagement.Hash.GetSpan().ToArray());
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.SYSCALL,
                _operand = BitConverter.GetBytes(ApplicationEngine.System_Contract_Call.Hash)
            });
            builder.AddInstruction(VM.OpCode.RET);
            return builder.ToArray();
        }

        private static (NefFile Nef, ContractManifest Manifest, byte[] NefBytes, byte[] ManifestBytes) CreateContractDocument(string name, byte[] script)
        {
            var nef = new NefFile
            {
                Compiler = "benchmark",
                Source = string.Empty,
                Tokens = System.Array.Empty<MethodToken>(),
                Script = script
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);

            var methodDescriptor = new ContractMethodDescriptor
            {
                Name = "main",
                Parameters = System.Array.Empty<ContractParameterDefinition>(),
                ReturnType = ContractParameterType.Any,
                Offset = 0,
                Safe = true
            };

            var manifest = new ContractManifest
            {
                Name = name,
                Groups = System.Array.Empty<ContractGroup>(),
                SupportedStandards = System.Array.Empty<string>(),
                Abi = new ContractAbi
                {
                    Methods = new[] { methodDescriptor },
                    Events = System.Array.Empty<ContractEventDescriptor>()
                },
                Permissions = new[] { ContractPermission.DefaultPermission },
                Trusts = WildcardContainer<ContractPermissionDescriptor>.CreateWildcard(),
                Extra = new JObject()
            };

            var nefBytes = nef.ToArray();
            var manifestBytes = manifest.ToJson().ToString().ToStrictUtf8Bytes();

            return (nef, manifest, nefBytes, manifestBytes);
        }

        private static ContractArtifact CreateContractArtifact(string name, byte[] script, int id)
        {
            var document = CreateContractDocument(name, script);
            var contract = new ContractState
            {
                Id = id,
                UpdateCounter = 0,
                Hash = global::Neo.SmartContract.Helper.GetContractHash(s_contractDeployer, document.Nef.CheckSum, document.Manifest.Name),
                Nef = document.Nef,
                Manifest = document.Manifest
            };

            return new ContractArtifact(contract, document.NefBytes, document.ManifestBytes);
        }

        private static ContractState CloneContractState(ContractState contract)
        {
            return new ContractState
            {
                Id = contract.Id,
                UpdateCounter = contract.UpdateCounter,
                Hash = contract.Hash,
                Nef = NefFile.Parse(contract.Nef.ToArray()),
                Manifest = ContractManifest.Parse(contract.Manifest.ToJson().ToString())
            };
        }

        private static void EnsureContract(BenchmarkApplicationEngine engine, ContractState contract)
        {
            var contractKey = new KeyBuilder(NativeContract.ContractManagement.Id, ContractManagementStoragePrefixContract)
                .Add(contract.Hash);

            if (engine.SnapshotCache.Contains(contractKey))
                return;

            var clone = CloneContractState(contract);
            engine.SnapshotCache.Add(contractKey, StorageItem.CreateSealed(clone));

            var idKey = new KeyBuilder(NativeContract.ContractManagement.Id, ContractManagementStoragePrefixContractHash)
                .AddBigEndian(clone.Id);

            if (!engine.SnapshotCache.Contains(idKey))
                engine.SnapshotCache.Add(idKey, new StorageItem(contract.Hash.ToArray()));
        }

        private static BenchmarkApplicationEngine CreateContractDeploymentEngine(ScenarioProfile profile)
        {
            var transaction = new Transaction
            {
                Signers = new[] { new Signer { Account = s_contractDeployer, Scopes = WitnessScope.Global } },
                Attributes = System.Array.Empty<TransactionAttribute>(),
                Script = System.Array.Empty<byte>(),
                Witnesses = System.Array.Empty<Witness>()
            };

            return NativeBenchmarkStateFactory.CreateEngine(transaction);
        }

        private static void ConfigureContractUpdateEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureContract(engine, s_updateContractArtifact.State);
        }

        private static void ConfigureContractDestroyEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            EnsureContract(engine, s_destroyContractArtifact.State);
        }

        private static void EmitContractDeployArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_deployContractArtifact.NefBytes);
            builder.Push(s_deployContractArtifact.ManifestBytes);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitContractUpdateArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_updateTargetNefBytes);
            builder.Push(s_updateTargetManifestBytes);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitContractGetArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, NativeContract.NEO.Hash.GetSpan().ToArray());
        }

        private static void EmitContractGetByIdArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(NativeContract.NEO.Id);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitContractHasMethodArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(NativeContract.NEO.Hash.GetSpan().ToArray());
            builder.Push("transfer");
            builder.Push(4);
            builder.Push(3);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNeoOnPaymentArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_candidateAccount.ToArray());
            builder.Push(s_defaultRegisterPrice);
            builder.Push(s_candidatePublicKey.EncodePoint(true));
            builder.Push(3);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNeoRegisterCandidateArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_candidatePublicKey.EncodePoint(true));
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNeoSetGasPerBlockArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(6 * NativeContract.GAS.Factor);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNeoSetRegisterPriceArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(1500 * NativeContract.GAS.Factor);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNeoTransferArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_neoSenderAccount.ToArray());
            builder.Push(s_neoRecipientAccount.ToArray());
            builder.Push(NativeContract.NEO.Factor);
            builder.AddInstruction(VM.OpCode.PUSHNULL);
            builder.Push(4);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNeoUnregisterCandidateArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_candidatePublicKey.EncodePoint(true));
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNeoVoteArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_neoSenderAccount.ToArray());
            builder.Push(s_candidatePublicKey.EncodePoint(true));
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNotaryBalanceArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, s_notaryAccount.ToArray());
        }

        private static void EmitNotaryExpirationArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, s_notaryAccount.ToArray());
        }

        private static void EmitNotaryLockDepositUntilArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_notaryAccount.ToArray());
            builder.Push(s_notaryLockTargetTill);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNotaryOnPaymentArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_notaryAccount.ToArray());
            builder.Push(new BigInteger(PolicyContract.DefaultNotaryAssistedAttributeFee) * 2);
            builder.AddInstruction(VM.OpCode.PUSHNULL);
            builder.Push(s_notaryInitialTill);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.Push(3);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNotarySetMaxDeltaArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push((uint)NotaryDefaultMaxNotValidBeforeDelta / 2);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNotaryVerifyArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_notaryDummySignature);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNotaryWithdrawArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_notaryAccount.ToArray());
            builder.AddInstruction(VM.OpCode.PUSHNULL);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitOracleSetPriceArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            var basePrice = Math.Max(1, profile.DataLength) * 10_000L;
            builder.Push(basePrice);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitOracleRequestArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            var path = BenchmarkDataFactory.CreateString(Math.Max(4, profile.DataLength / 8), 'u');
            builder.Push($"{OracleSampleUrl}/{path}");
            builder.Push(OracleSampleFilter);
            builder.Push(OracleSampleCallback);
            builder.Push(BenchmarkDataFactory.CreateByteArray(Math.Max(1, profile.DataLength), 0x90));
            var gas = Math.Max(200_0000L, (long)profile.DataLength * 1_0000L);
            builder.Push(gas);
            builder.Push(Math.Max(1, profile.CollectionLength));
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitRoleDesignateArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push((int)Role.Oracle);
            foreach (var node in s_roleDesignationNodes)
            {
                builder.Push(node.EncodePoint(true));
            }
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitRoleGetDesignatedArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push((int)Role.Oracle);
            builder.Push(s_roleDesignationIndex);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitContractIsContractArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, NativeContract.NEO.Hash.GetSpan().ToArray());
        }

        private static void EmitContractSetMinimumDeploymentFeeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(10_00000000);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitPolicySetFeePerByteArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_policyFeePerByteValue);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitPolicySetExecFeeFactorArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_policyExecFeeFactorValue);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitPolicySetStoragePriceArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_policyStoragePriceValue);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitPolicySetMillisecondsPerBlockArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_policyMillisecondsPerBlockValue);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitPolicySetMaxValidUntilBlockIncrementArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_policyMaxValidUntilValue);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitPolicySetMaxTraceableBlocksArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_policyMaxTraceableValue);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitPolicySetAttributeFeeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_policyAttributeType);
            builder.Push(s_policyAttributeFeeValue);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitPolicyBlockAccountArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, s_gasTransferRecipient.GetSpan().ToArray());
        }

        private static void EmitLedgerGetBlockArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(0);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitLedgerGetTransactionArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, s_zeroHashBytes);
        }

        private static void EmitLedgerGetTransactionFromBlockArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_blockIndexBytes);
            builder.Push(0);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitLedgerGetTransactionHeightArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, s_zeroHashBytes);
        }

        private static void EmitLedgerGetTransactionSignersArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, s_zeroHashBytes);
        }

        private static void EmitLedgerGetTransactionVmStateArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, s_zeroHashBytes);
        }

        private static void EmitGasTransferArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.AddInstruction(VM.OpCode.PUSHNULL);
            builder.Push(s_gasTransferAmount);
            builder.Push(s_gasTransferRecipient.GetSpan().ToArray());
            builder.Push(CommitteeAddress.GetSpan().ToArray());
            builder.Push(4);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNeoAccountStateArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, CommitteeAddress.GetSpan().ToArray());
        }

        private static void EmitNeoCandidateVoteArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, s_committeePublicKeyCompressed);
        }

        private static void EmitNeoNextBlockValidatorsArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_validatorsCount);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitNeoUnclaimedGasArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(1);
            builder.Push(CommitteeAddress.GetSpan().ToArray());
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitCommitteeAccountArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, CommitteeAddress.GetSpan().ToArray());
        }

        private static void EmitPackedSingleArgument(InstructionBuilder builder, byte[] payload)
        {
            builder.Push(payload);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static BenchmarkApplicationEngine CreateCommitteeWitnessEngine(ScenarioProfile profile)
        {
            return NativeBenchmarkStateFactory.CreateEngine(new ManualWitness(CommitteeAddress));
        }

        private static BenchmarkApplicationEngine CreateGasTransferEngine(ScenarioProfile profile)
        {
            var engine = NativeBenchmarkStateFactory.CreateEngine(new ManualWitness(CommitteeAddress));
            var iterations = Math.Max(1, profile.Iterations * 8);
            var balance = s_gasTransferAmount * iterations;
            SeedGasAccount(engine.SnapshotCache, CommitteeAddress, balance);
            SeedGasAccount(engine.SnapshotCache, s_gasTransferRecipient, BigInteger.Zero);
            return engine;
        }

        private static void ConfigurePolicyUnblockEngine(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            var key = new KeyBuilder(NativeContract.Policy.Id, PolicyBlockedAccountPrefix).Add(s_gasTransferRecipient);
            if (!engine.SnapshotCache.Contains(key))
            {
                engine.SnapshotCache.Add(key, new StorageItem(System.Array.Empty<byte>()));
            }
        }

        private static void SeedGasAccount(DataCache snapshot, UInt160 account, BigInteger balance)
        {
            var key = new KeyBuilder(NativeContract.GAS.Id, TokenAccountPrefix).Add(account);
            var item = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            item.GetInteroperable<AccountState>().Balance = balance;
        }

        private static byte[] CreateBlsScalar()
        {
            var scalar = new byte[32];
            scalar[0] = 0x03;
            return scalar;
        }

        private static void EmitCryptoBlsDeserializeCall(InstructionBuilder builder, byte[] payload)
        {
            builder.Push(payload);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.Push((int)CallFlags.All);
            builder.Push("bls12381Deserialize");
            builder.Push(NativeContract.CryptoLib.Hash.GetSpan().ToArray());
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.SYSCALL,
                _operand = BitConverter.GetBytes(ApplicationEngine.System_Contract_Call.Hash)
            });
        }

        private static string ToBase64Url(string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            var base64 = Convert.ToBase64String(bytes);
            return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static void EmitPolicyAttributeFeeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push((byte)TransactionAttributeType.HighPriority);
        }

        private static void EmitPolicyIsBlockedArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CommitteeAddress.GetSpan().ToArray());
        }

        private static void EmitCryptoHashArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateBytePayload(profile, 0x50));
        }

        private static void EmitCryptoMurmurArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateBytePayload(profile, 0x51));
            builder.Push((uint)0x12345678);
        }

        private static void EmitCryptoBlsDeserializeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitPackedSingleArgument(builder, s_blsG1);
        }

        private static void EmitCryptoBlsSerializeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitCryptoBlsDeserializeCall(builder, s_blsG1);
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitCryptoBlsAddArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitCryptoBlsDeserializeCall(builder, s_blsGt);
            EmitCryptoBlsDeserializeCall(builder, s_blsGt);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitCryptoBlsEqualArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitCryptoBlsDeserializeCall(builder, s_blsG1);
            EmitCryptoBlsDeserializeCall(builder, s_blsG1);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitCryptoBlsMulArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(false);
            builder.Push(s_blsScalar);
            EmitCryptoBlsDeserializeCall(builder, s_blsGt);
            builder.Push(3);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitCryptoBlsPairingArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            EmitCryptoBlsDeserializeCall(builder, s_blsG2);
            EmitCryptoBlsDeserializeCall(builder, s_blsG1);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitCryptoRecoverSecpArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_recoverSignature);
            builder.Push(s_recoverMessageHash);
            builder.Push(2);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitCryptoVerifyEcdsaArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push((int)NamedCurveHash.secp256k1SHA256);
            builder.Push(s_verifySignature);
            builder.Push(s_verifyPubKey);
            builder.Push(s_verifyMessage);
            builder.Push(4);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitCryptoVerifyEd25519Arguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_ed25519Signature);
            builder.Push(s_ed25519PublicKey);
            builder.Push(System.Array.Empty<byte>());
            builder.Push(3);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitStdLibBase58EncodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateBytePayload(profile, 0x10));
        }

        private static void EmitStdLibBase58DecodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(Base58.Encode(CreateBytePayload(profile, 0x11)));
        }

        private static void EmitStdLibBase58CheckEncodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateBytePayload(profile, 0x12));
        }

        private static void EmitStdLibBase58CheckDecodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(Base58.Base58CheckEncode(CreateBytePayload(profile, 0x13)));
        }

        private static void EmitStdLibBase64EncodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateBytePayload(profile, 0x14));
        }

        private static void EmitStdLibBase64DecodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(Convert.ToBase64String(CreateBytePayload(profile, 0x15)));
        }

        private static void EmitStdLibBase64UrlEncodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateStringPayload(profile, seed: 'n'));
        }

        private static void EmitStdLibBase64UrlDecodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(ToBase64Url(CreateStringPayload(profile, seed: 'n')));
        }

        private static void EmitStdLibHexEncodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateBytePayload(profile, 0x16));
        }

        private static void EmitStdLibHexDecodeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(Convert.ToHexString(CreateBytePayload(profile, 0x17)).ToLowerInvariant());
        }

        private static void EmitStdLibMemoryCompareArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateBytePayload(profile, 0x18));
            builder.Push(CreateBytePayload(profile, 0x28));
        }

        private static void EmitStdLibMemorySearchArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            var haystack = CreateBytePayload(profile, 0x30, scale: 1.5, minLength: 4);
            var needleLength = Math.Clamp(haystack.Length / Math.Max(2, profile.CollectionLength == 0 ? 2 : profile.CollectionLength), 1, haystack.Length);
            var needle = haystack.Take(needleLength).ToArray();
            builder.Push(haystack);
            builder.Push(needle);
        }

        private static void EmitStdLibStringSplitArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateDelimitedString(profile));
            builder.Push("-");
        }

        private static void EmitStdLibSerializeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateBytePayload(profile, 0x40));
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitStdLibDeserializeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateBytePayload(profile, 0x41));
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.Push((int)CallFlags.All);
            builder.Push("serialize");
            builder.Push(NativeContract.StdLib.Hash.GetSpan().ToArray());
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.SYSCALL,
                _operand = BitConverter.GetBytes(ApplicationEngine.System_Contract_Call.Hash)
            });
            builder.AddInstruction(VM.OpCode.PUSH1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitStdLibJsonSerializeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateStringPayload(profile, seed: 'j'));
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitStdLibJsonDeserializeArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(Encoding.UTF8.GetBytes(CreateJsonPayload(profile)));
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitStdLibStrLenArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateStringPayload(profile));
            builder.Push(1);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitStdLibAtoiArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(CreateNumericPayload(profile));
        }

        private static void EmitStdLibItoaArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(Math.Max(1, profile.DataLength));
        }

        private static ECPoint GetCommitteePublicKey()
        {
            var committee = s_standbyCommittee;
            if (committee.Count > 0)
                return committee[0];
            var validators = s_standbyValidators;
            if (validators.Count > 0)
                return validators[0];
            return ECCurve.Secp256r1.G;
        }
    }
}
