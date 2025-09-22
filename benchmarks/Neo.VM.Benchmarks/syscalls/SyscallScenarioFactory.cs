// Copyright (C) 2015-2025 The Neo Project.
//
// SyscallScenarioFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Benchmark;
using Neo.VM.Benchmark.Infrastructure;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Benchmark.Syscalls
{
    internal static class SyscallScenarioFactory
    {
        private const byte ContractManagementPrefixContract = 8;
        private const byte ContractManagementPrefixContractHash = 12;
        private const int CallInvokerContractId = 0x4343;
        private const int CallCalleeContractId = 0x4344;

        private static readonly byte[] s_calleeScript = BuildCalleeScript();
        private static readonly ContractState s_calleeContract = CreateScriptContract(s_calleeScript, "Callee", CallCalleeContractId, "run");
        private static readonly UInt160 s_calleeScriptHash = s_calleeScript.ToScriptHash();

        private static readonly IReadOnlyList<ECPoint> s_standbyValidators = BenchmarkProtocolSettings.StandbyValidators;
        private static readonly byte[] s_checkSigPublicKey = s_standbyValidators[0].EncodePoint(true);
        private static readonly byte[] s_checkSigSignature = new byte[64];

        private static readonly byte[][] s_multisigSignatures = { new byte[64] };
        private static readonly byte[][] s_multisigPubKeys = s_standbyValidators
            .Take(2)
            .Select(validators => validators.EncodePoint(true))
            .ToArray();

        private static readonly ContractState s_nativePolicyContract = NativeContract.Policy.GetContractState(ProtocolSettings.Default, 0);
        private const int StorageContractId = 0x4242;

        private static readonly byte[] s_storageKey = { 0xB0, 0x01 };
        private static readonly byte[] s_iteratorPrefix = { 0xC1 };

        private sealed record ScenarioBuilder(
            Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> ScriptFactory,
            Action<BenchmarkApplicationEngine, ScenarioProfile>? Configure = null,
            Func<ScenarioProfile, BenchmarkApplicationEngine>? EngineFactory = null)
        {
            public VmBenchmarkCase BuildCase(InteropDescriptor descriptor, ScenarioComplexity complexity)
            {
                var profile = ScenarioProfile.For(complexity);
                var scenario = new ApplicationEngineVmScenario(ScriptFactory, Configure, EngineFactory);
                return new VmBenchmarkCase(descriptor.Name, BenchmarkComponent.Syscall, complexity, scenario);
            }
        }

        private static readonly UInt160[] s_witnessAccounts =
        {
            UInt160.Parse("0x0102030405060708090a0b0c0d0e0f1011121314"),
            UInt160.Parse("0x1112131415161718191a1b1c1d1e1f2021222324")
        };

        public static IEnumerable<VmBenchmarkCase> CreateCases()
        {
            var builders = CreateBuilders();
            foreach (ScenarioComplexity complexity in Enum.GetValues<ScenarioComplexity>())
            {
                foreach (var (descriptor, builder) in builders)
                {
                    SyscallCoverageTracker.Register(descriptor.Name);
                    yield return builder.BuildCase(descriptor, complexity);
                }
            }
        }

        private static IReadOnlyDictionary<InteropDescriptor, ScenarioBuilder> CreateBuilders()
        {
            var map = new Dictionary<InteropDescriptor, ScenarioBuilder>
            {
                [ApplicationEngine.System_Runtime_Platform] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_Platform)),
                [ApplicationEngine.System_Runtime_GetNetwork] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetNetwork)),
                [ApplicationEngine.System_Runtime_GetAddressVersion] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetAddressVersion)),
                [ApplicationEngine.System_Runtime_GetTrigger] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetTrigger)),
                [ApplicationEngine.System_Runtime_GetTime] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetTime)),
                [ApplicationEngine.System_Runtime_GetInvocationCounter] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetInvocationCounter)),
                [ApplicationEngine.System_Runtime_GasLeft] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GasLeft)),
                [ApplicationEngine.System_Runtime_GetRandom] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetRandom)),
                [ApplicationEngine.System_Runtime_GetScriptContainer] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetScriptContainer), EngineFactory: CreateTransactionBackedEngine),
                [ApplicationEngine.System_Runtime_GetExecutingScriptHash] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetExecutingScriptHash)),
                [ApplicationEngine.System_Runtime_GetCallingScriptHash] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetCallingScriptHash)),
                [ApplicationEngine.System_Runtime_GetEntryScriptHash] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_GetEntryScriptHash)),
                [ApplicationEngine.System_Runtime_Log] = new ScenarioBuilder(CreateSyscallScripts(ApplicationEngine.System_Runtime_Log, EmitLogArguments, dropResult: false)),
                [ApplicationEngine.System_Runtime_Notify] = new ScenarioBuilder(CreateSyscallScripts(ApplicationEngine.System_Runtime_Notify, EmitNotifyArguments, dropResult: false)),
                [ApplicationEngine.System_Runtime_GetNotifications] = new ScenarioBuilder(CreateSyscallScripts(ApplicationEngine.System_Runtime_GetNotifications, EmitGetNotificationsArguments, dropResult: true)),
                [ApplicationEngine.System_Runtime_BurnGas] = new ScenarioBuilder(CreateSyscallScripts(ApplicationEngine.System_Runtime_BurnGas, EmitBurnGasArguments, dropResult: false)),
                [ApplicationEngine.System_Runtime_CurrentSigners] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Runtime_CurrentSigners), EngineFactory: CreateTransactionBackedEngine),
                [ApplicationEngine.System_Runtime_CheckWitness] = new ScenarioBuilder(CreateSyscallScripts(ApplicationEngine.System_Runtime_CheckWitness, EmitCheckWitnessArguments, dropResult: true), EngineFactory: CreateTransactionBackedEngine),
                [ApplicationEngine.System_Runtime_LoadScript] = new ScenarioBuilder(CreateLoadScriptScripts()),
                [ApplicationEngine.System_Contract_GetCallFlags] = new ScenarioBuilder(CreateZeroArgumentScripts(ApplicationEngine.System_Contract_GetCallFlags)),
                [ApplicationEngine.System_Contract_CreateStandardAccount] = new ScenarioBuilder(CreateSyscallScripts(ApplicationEngine.System_Contract_CreateStandardAccount, EmitCreateStandardAccountArguments, dropResult: true)),
                [ApplicationEngine.System_Contract_CreateMultisigAccount] = new ScenarioBuilder(CreateSyscallScripts(ApplicationEngine.System_Contract_CreateMultisigAccount, EmitCreateMultisigAccountArguments, dropResult: true)),
                [ApplicationEngine.System_Contract_Call] = new ScenarioBuilder(CreateContractCallScripts()),
                [ApplicationEngine.System_Contract_CallNative] = new ScenarioBuilder(CreateContractCallNativeScripts()),
                [ApplicationEngine.System_Contract_NativeOnPersist] = new ScenarioBuilder(CreateNativeOnPersistScripts(), ConfigureNativePersist, _ => BenchmarkApplicationEngine.Create(trigger: TriggerType.OnPersist)),
                [ApplicationEngine.System_Contract_NativePostPersist] = new ScenarioBuilder(CreateNativePostPersistScripts(), ConfigureNativePersist, _ => BenchmarkApplicationEngine.Create(trigger: TriggerType.PostPersist)),
                [ApplicationEngine.System_Crypto_CheckSig] = new ScenarioBuilder(CreateCheckSigScripts(), EngineFactory: CreateTransactionBackedEngine),
                [ApplicationEngine.System_Crypto_CheckMultisig] = new ScenarioBuilder(CreateCheckMultisigScripts(), EngineFactory: CreateTransactionBackedEngine),
                [ApplicationEngine.System_Storage_GetContext] = new ScenarioBuilder(CreateStorageGetContextScripts()),
                [ApplicationEngine.System_Storage_GetReadOnlyContext] = new ScenarioBuilder(CreateStorageGetReadOnlyContextScripts()),
                [ApplicationEngine.System_Storage_AsReadOnly] = new ScenarioBuilder(CreateStorageAsReadOnlyScripts()),
                [ApplicationEngine.System_Storage_Get] = new ScenarioBuilder(CreateStorageGetScripts()),
                [ApplicationEngine.System_Storage_Put] = new ScenarioBuilder(CreateStoragePutScripts()),
                [ApplicationEngine.System_Storage_Delete] = new ScenarioBuilder(CreateStorageDeleteScripts()),
                [ApplicationEngine.System_Storage_Find] = new ScenarioBuilder(CreateStorageFindScripts()),
                [ApplicationEngine.System_Storage_Local_Get] = new ScenarioBuilder(CreateStorageLocalGetScripts()),
                [ApplicationEngine.System_Storage_Local_Put] = new ScenarioBuilder(CreateStorageLocalPutScripts()),
                [ApplicationEngine.System_Storage_Local_Delete] = new ScenarioBuilder(CreateStorageLocalDeleteScripts()),
                [ApplicationEngine.System_Storage_Local_Find] = new ScenarioBuilder(CreateStorageLocalFindScripts()),
                [ApplicationEngine.System_Iterator_Next] = new ScenarioBuilder(CreateIteratorNextScripts()),
                [ApplicationEngine.System_Iterator_Value] = new ScenarioBuilder(CreateIteratorValueScripts())
            };

            return map;
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateZeroArgumentScripts(InteropDescriptor descriptor)
        {
            return CreateSyscallScripts(descriptor, emitArguments: null, dropResult: true);
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateSyscallScripts(
            InteropDescriptor descriptor,
            Action<InstructionBuilder, ScenarioProfile>? emitArguments,
            bool dropResult,
            Func<ScenarioProfile, ScenarioProfile>? saturatedProfileSelector = null)
        {
            return profile =>
            {
                var baseline = CreateScript(BuildNoOpLoop(profile), profile);
                var single = CreateScript(BuildSyscallLoop(descriptor, profile, emitArguments, dropResult), profile);
                var saturatedProfile = saturatedProfileSelector?.Invoke(profile)
                                      ?? new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = CreateScript(BuildSyscallLoop(descriptor, saturatedProfile, emitArguments, dropResult), saturatedProfile);
                return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(baseline, single, saturated);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateLoadScriptScripts()
        {
            return profile =>
            {
                var single = CreateScript(BuildLoadScriptLoop(profile), profile);
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 4, Math.Max(profile.DataLength, 1) * 2, profile.CollectionLength);
                var saturated = CreateScript(BuildLoadScriptLoop(saturatedProfile), saturatedProfile);
                var baseline = CreateScript(BuildNoOpLoop(profile), profile);
                return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(baseline, single, saturated);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageGetContextScripts()
        {
            return profile =>
            {
                var baseline = BuildNoOpLoop(profile);
                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, seed: null);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageGetReadOnlyContextScripts()
        {
            return profile =>
            {
                var baseline = BuildNoOpLoop(profile);
                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetReadOnlyContext);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetReadOnlyContext);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, seed: null);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageAsReadOnlyScripts()
        {
            return profile =>
            {
                var valueLength = Math.Max(1, profile.DataLength);
                var storageValue = BenchmarkDataFactory.CreateByteArray(valueLength, 0x70);
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_AsReadOnly);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_AsReadOnly);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, seed: null);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageGetScripts()
        {
            return profile =>
            {
                var valueLength = Math.Max(1, profile.DataLength);
                var storageValue = BenchmarkDataFactory.CreateByteArray(valueLength, 0x70);
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push(s_storageKey);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Get);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push(s_storageKey);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Get);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedSimpleStorage);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStoragePutScripts()
        {
            return profile =>
            {
                var valueLength = Math.Max(1, profile.DataLength);
                var storageValue = BenchmarkDataFactory.CreateByteArray(valueLength, 0x70);
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push(storageValue);
                        builder.Push(s_storageKey);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Put);
                    },
                    localCount: 2);

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength * 2, profile.CollectionLength);
                var saturatedValue = BenchmarkDataFactory.CreateByteArray(Math.Max(1, saturatedProfile.DataLength), 0x72);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push(saturatedValue);
                        builder.Push(s_storageKey);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Put);
                    },
                    localCount: 2);

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedSimpleStorage);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageDeleteScripts()
        {
            return profile =>
            {
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push(s_storageKey);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Delete);
                    },
                    localCount: 2);

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push(s_storageKey);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Delete);
                    },
                    localCount: 2);

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedSimpleStorage);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageLocalGetScripts()
        {
            return profile =>
            {
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.Push(s_storageKey);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Local_Get);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        builder.Push(s_storageKey);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Local_Get);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedSimpleStorage);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageLocalPutScripts()
        {
            return profile =>
            {
                var valueLength = Math.Max(1, profile.DataLength);
                var storageValue = BenchmarkDataFactory.CreateByteArray(valueLength, 0x60);
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.Push(storageValue);
                        builder.Push(s_storageKey);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Local_Put);
                    });

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength * 2, profile.CollectionLength);
                var saturatedValue = BenchmarkDataFactory.CreateByteArray(Math.Max(1, saturatedProfile.DataLength), 0x62);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        builder.Push(saturatedValue);
                        builder.Push(s_storageKey);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Local_Put);
                    });

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedSimpleStorage);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageLocalDeleteScripts()
        {
            return profile =>
            {
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.Push(s_storageKey);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Local_Delete);
                    });

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        builder.Push(s_storageKey);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Local_Delete);
                    });

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedSimpleStorage);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageLocalFindScripts()
        {
            return profile =>
            {
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.Push((int)FindOptions.None);
                        builder.Push(s_iteratorPrefix);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Local_Find);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        builder.Push((int)FindOptions.None);
                        builder.Push(s_iteratorPrefix);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Local_Find);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedIteratorEntries);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateStorageFindScripts()
        {
            return profile =>
            {
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push((int)FindOptions.None);
                        builder.Push(s_iteratorPrefix);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Find);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push((int)FindOptions.None);
                        builder.Push(s_iteratorPrefix);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Find);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedIteratorEntries);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateIteratorNextScripts()
        {
            return profile =>
            {
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push((int)FindOptions.None);
                        builder.Push(s_iteratorPrefix);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Find);
                        builder.AddInstruction(VM.OpCode.DUP);
                        EmitSyscall(builder, ApplicationEngine.System_Iterator_Next);
                        builder.AddInstruction(VM.OpCode.DROP);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push((int)FindOptions.None);
                        builder.Push(s_iteratorPrefix);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Find);
                        builder.AddInstruction(VM.OpCode.DUP);
                        EmitSyscall(builder, ApplicationEngine.System_Iterator_Next);
                        builder.AddInstruction(VM.OpCode.DROP);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedIteratorEntries);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateIteratorValueScripts()
        {
            return profile =>
            {
                var baseline = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.AddInstruction(VM.OpCode.PUSH0);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push((int)FindOptions.None);
                        builder.Push(s_iteratorPrefix);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Find);
                        builder.AddInstruction(VM.OpCode.DUP);
                        EmitSyscall(builder, ApplicationEngine.System_Iterator_Next);
                        builder.AddInstruction(VM.OpCode.DROP);
                        EmitSyscall(builder, ApplicationEngine.System_Iterator_Value);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                var saturatedProfile = new ScenarioProfile(profile.Iterations * 8, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    prolog: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Storage_GetContext);
                        builder.AddInstruction(VM.OpCode.STLOC1);
                    },
                    iteration: builder =>
                    {
                        builder.Push((int)FindOptions.None);
                        builder.Push(s_iteratorPrefix);
                        builder.AddInstruction(VM.OpCode.LDLOC1);
                        EmitSyscall(builder, ApplicationEngine.System_Storage_Find);
                        builder.AddInstruction(VM.OpCode.DUP);
                        EmitSyscall(builder, ApplicationEngine.System_Iterator_Next);
                        builder.AddInstruction(VM.OpCode.DROP);
                        EmitSyscall(builder, ApplicationEngine.System_Iterator_Value);
                        builder.AddInstruction(VM.OpCode.DROP);
                    },
                    localCount: 2);

                return CreateStorageScriptSet(baseline, single, saturated, profile, saturatedProfile, SeedIteratorEntries);
            };
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScript CreateScript(byte[] script, ScenarioProfile profile)
            => new(script, profile);

        private static ApplicationEngineVmScenario.ApplicationEngineScriptSet CreateStorageScriptSet(
            byte[] baseline,
            byte[] single,
            byte[] saturated,
            ScenarioProfile profile,
            ScenarioProfile saturatedProfile,
            Action<DataCache, ContractState, ScenarioProfile>? seed)
        {
            return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(
                CreateStorageScript(baseline, profile, seed),
                CreateStorageScript(single, profile, seed),
                CreateStorageScript(saturated, saturatedProfile, seed));
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScript CreateStorageScript(
            byte[] script,
            ScenarioProfile profile,
            Action<DataCache, ContractState, ScenarioProfile>? seed)
        {
            var scriptHash = script.ToScriptHash();
            return new ApplicationEngineVmScenario.ApplicationEngineScript(script, profile, state =>
            {
                var cache = state.SnapshotCache ?? throw new InvalidOperationException("Snapshot cache not initialized.");
                var contract = CreateStorageContract(script);
                cache.Add(StorageKey.Create(NativeContract.ContractManagement.Id, ContractManagementPrefixContract, scriptHash), StorageItem.CreateSealed(contract));
                seed?.Invoke(cache, contract, profile);
                state.ScriptHash = contract.Hash;
                state.Contract = contract;
                state.CallFlags = CallFlags.All;
            });
        }

        private static ContractState CreateStorageContract(byte[] script)
        {
            return CreateScriptContract(script, "BenchmarkStorage", StorageContractId, "main");
        }

        private static void SeedSimpleStorage(DataCache cache, ContractState contract, ScenarioProfile profile)
        {
            var valueLength = Math.Max(1, profile.DataLength);
            var value = BenchmarkDataFactory.CreateByteArray(valueLength, fill: 0x42);
            cache.Add(new StorageKey { Id = contract.Id, Key = s_storageKey }, new StorageItem(value));
        }

        private static void SeedIteratorEntries(DataCache cache, ContractState contract, ScenarioProfile profile)
        {
            var entryCount = Math.Max(1, profile.CollectionLength);
            var iteratorEntries = BenchmarkDataFactory.CreateByteSegments(entryCount, Math.Max(1, profile.DataLength / entryCount));
            var prefix = s_iteratorPrefix[0];
            foreach (var (key, value) in iteratorEntries.Select((value, index) => (Key: BenchmarkDataFactory.CreateIteratorKey(index, prefix), Value: value)))
                cache.Add(new StorageKey { Id = contract.Id, Key = key }, new StorageItem(value));
        }

        private static void EmitPushArray(InstructionBuilder builder, IReadOnlyList<byte[]> items)
        {
            foreach (var item in items)
                builder.Push(item);
            builder.Push(items.Count);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitSyscall(InstructionBuilder builder, InteropDescriptor descriptor)
        {
            builder.AddInstruction(new Instruction
            {
                _opCode = VM.OpCode.SYSCALL,
                _operand = BitConverter.GetBytes(descriptor.Hash)
            });
        }

        private static byte[] BuildCalleeScript()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(VM.OpCode.RET);
            return builder.ToArray();
        }

        private static ContractState CreateScriptContract(byte[] script, string name, int id, string methodName, bool safe = true)
        {
            var nef = new NefFile
            {
                Compiler = "benchmark",
                Source = string.Empty,
                Tokens = System.Array.Empty<MethodToken>(),
                Script = script
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);

            var method = new ContractMethodDescriptor
            {
                Name = methodName,
                Parameters = System.Array.Empty<ContractParameterDefinition>(),
                ReturnType = ContractParameterType.Void,
                Offset = 0,
                Safe = safe
            };

            var abi = new ContractAbi
            {
                Methods = new[] { method },
                Events = System.Array.Empty<ContractEventDescriptor>()
            };

            var manifest = new ContractManifest
            {
                Name = name,
                Groups = System.Array.Empty<ContractGroup>(),
                SupportedStandards = System.Array.Empty<string>(),
                Abi = abi,
                Permissions = new[] { ContractPermission.DefaultPermission },
                Trusts = WildcardContainer<ContractPermissionDescriptor>.CreateWildcard(),
                Extra = new JObject()
            };

            return new ContractState
            {
                Id = id,
                UpdateCounter = 0,
                Hash = script.ToScriptHash(),
                Nef = nef,
                Manifest = manifest
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateContractCallScripts()
        {
            return profile =>
            {
                var baseline = BuildNoOpLoop(profile);
                var single = BuildContractCallLoop(profile);
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 4, profile.DataLength, profile.CollectionLength);
                var saturated = BuildContractCallLoop(saturatedProfile);

                var baselineProfile = profile.With(dataLength: 0, collectionLength: 0);
                return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(
                    CreateContractInvokerScript(baseline, baselineProfile, includeCallee: true),
                    CreateContractInvokerScript(single, profile, includeCallee: true),
                    CreateContractInvokerScript(saturated, saturatedProfile, includeCallee: true));
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateContractCallNativeScripts()
        {
            return profile =>
            {
                var baseline = BuildNoOpLoop(profile);
                var single = BuildContractCallNativeLoop(profile);
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 4, profile.DataLength, profile.CollectionLength);
                var saturated = BuildContractCallNativeLoop(saturatedProfile);
                var baselineProfile = profile.With(dataLength: 0, collectionLength: 0);
                return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(
                    CreateCallNativeInvokerScript(baseline, baselineProfile),
                    CreateCallNativeInvokerScript(single, profile),
                    CreateCallNativeInvokerScript(saturated, saturatedProfile));
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateNativeOnPersistScripts()
        {
            return profile =>
            {
                var baseline = BuildNoOpLoop(profile);
                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Contract_NativeOnPersist);
                    });
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 2, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Contract_NativeOnPersist);
                    });

                return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(baseline, single, saturated);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateNativePostPersistScripts()
        {
            return profile =>
            {
                var baseline = BuildNoOpLoop(profile);
                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Contract_NativePostPersist);
                    });
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 2, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        EmitSyscall(builder, ApplicationEngine.System_Contract_NativePostPersist);
                    });

                return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(baseline, single, saturated);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateCheckSigScripts()
        {
            return profile =>
            {
                var baseline = BuildNoOpLoop(profile);
                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        builder.Push(s_checkSigSignature);
                        builder.Push(s_checkSigPublicKey);
                        EmitSyscall(builder, ApplicationEngine.System_Crypto_CheckSig);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 4, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        builder.Push(s_checkSigSignature);
                        builder.Push(s_checkSigPublicKey);
                        EmitSyscall(builder, ApplicationEngine.System_Crypto_CheckSig);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(baseline, single, saturated);
            };
        }

        private static Func<ScenarioProfile, ApplicationEngineVmScenario.ApplicationEngineScriptSet> CreateCheckMultisigScripts()
        {
            return profile =>
            {
                var baseline = BuildNoOpLoop(profile);
                var single = LoopScriptFactory.BuildCountingLoop(profile,
                    iteration: builder =>
                    {
                        EmitPushArray(builder, s_multisigSignatures);
                        EmitPushArray(builder, s_multisigPubKeys);
                        EmitSyscall(builder, ApplicationEngine.System_Crypto_CheckMultisig);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });
                var saturatedProfile = new ScenarioProfile(profile.Iterations * 4, profile.DataLength, profile.CollectionLength);
                var saturated = LoopScriptFactory.BuildCountingLoop(saturatedProfile,
                    iteration: builder =>
                    {
                        EmitPushArray(builder, s_multisigSignatures);
                        EmitPushArray(builder, s_multisigPubKeys);
                        EmitSyscall(builder, ApplicationEngine.System_Crypto_CheckMultisig);
                        builder.AddInstruction(VM.OpCode.DROP);
                    });

                return new ApplicationEngineVmScenario.ApplicationEngineScriptSet(baseline, single, saturated);
            };
        }

        private static void ConfigureNativePersist(BenchmarkApplicationEngine engine, ScenarioProfile profile)
        {
            var cache = engine.SnapshotCache;
            foreach (var native in NativeContract.Contracts)
            {
                var state = native.GetContractState(engine.ProtocolSettings, engine.PersistingBlock?.Index ?? 0);
                if (state is null)
                    continue;
                SeedContract(cache, state);
            }
        }

        private static byte[] BuildContractCallLoop(ScenarioProfile profile)
        {
            return LoopScriptFactory.BuildCountingLoop(profile,
                iteration: builder =>
                {
                    builder.AddInstruction(VM.OpCode.NEWARRAY0);
                    builder.Push((int)CallFlags.All);
                    builder.Push("run");
                    builder.Push(s_calleeScriptHash.ToArray());
                    EmitSyscall(builder, ApplicationEngine.System_Contract_Call);
                    builder.AddInstruction(VM.OpCode.DROP);
                });
        }

        private static byte[] BuildContractCallNativeLoop(ScenarioProfile profile)
        {
            return LoopScriptFactory.BuildCountingLoop(profile,
                iteration: builder =>
                {
                    builder.Push((byte)0);
                    EmitSyscall(builder, ApplicationEngine.System_Contract_CallNative);
                });
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScript CreateContractInvokerScript(byte[] script, ScenarioProfile profile, bool includeCallee)
        {
            var invokerContract = CreateScriptContract(script, "Invoker", CallInvokerContractId, "run");
            return new ApplicationEngineVmScenario.ApplicationEngineScript(script, profile, state =>
            {
                var cache = state.SnapshotCache ?? throw new InvalidOperationException("Snapshot cache not initialized.");
                SeedContract(cache, invokerContract);
                if (includeCallee)
                {
                    SeedContract(cache, s_calleeContract);
                }
                state.ScriptHash = invokerContract.Hash;
                state.Contract = invokerContract;
                state.CallFlags = CallFlags.All;
            });
        }

        private static ApplicationEngineVmScenario.ApplicationEngineScript CreateCallNativeInvokerScript(byte[] script, ScenarioProfile profile)
        {
            return new ApplicationEngineVmScenario.ApplicationEngineScript(script, profile, state =>
            {
                state.ScriptHash = NativeContract.Policy.Hash;
                state.Contract = s_nativePolicyContract;
                state.CallFlags = CallFlags.All;
            });
        }

        private static void SeedContract(DataCache cache, ContractState contract)
        {
            var contractKey = StorageKey.Create(NativeContract.ContractManagement.Id, ContractManagementPrefixContract, contract.Hash);
            if (!cache.TryGet(contractKey, out _))
                cache.Add(contractKey, StorageItem.CreateSealed(contract));

            var hashKey = StorageKey.Create(NativeContract.ContractManagement.Id, ContractManagementPrefixContractHash, contract.Id);
            if (!cache.TryGet(hashKey, out _))
                cache.Add(hashKey, new StorageItem(contract.Hash.ToArray()));
        }

        private static byte[] BuildLoadScriptLoop(ScenarioProfile profile)
        {
            var targetBuilder = new InstructionBuilder();
            targetBuilder.AddInstruction(VM.OpCode.PUSH1);
            targetBuilder.AddInstruction(VM.OpCode.RET);
            var targetScript = targetBuilder.ToArray();

            return LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.AddInstruction(VM.OpCode.NEWARRAY0);
                builder.Push((int)CallFlags.All);
                builder.Push(targetScript);
                builder.AddInstruction(new Instruction
                {
                    _opCode = VM.OpCode.SYSCALL,
                    _operand = BitConverter.GetBytes(ApplicationEngine.System_Runtime_LoadScript.Hash)
                });
                builder.AddInstruction(VM.OpCode.DROP);
            });
        }

        private static byte[] BuildNoOpLoop(ScenarioProfile profile)
        {
            return LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.AddInstruction(VM.OpCode.NOP);
            });
        }

        private static byte[] BuildSyscallLoop(
            InteropDescriptor descriptor,
            ScenarioProfile profile,
            Action<InstructionBuilder, ScenarioProfile>? emitArguments,
            bool dropResult)
        {
            return LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                emitArguments?.Invoke(builder, profile);
                builder.AddInstruction(new Instruction
                {
                    _opCode = VM.OpCode.SYSCALL,
                    _operand = BitConverter.GetBytes(descriptor.Hash)
                });

                if (dropResult)
                {
                    builder.AddInstruction(VM.OpCode.DROP);
                }
            });
        }

        private static void EmitLogArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            var messageLength = Math.Max(1, profile.DataLength);
            builder.Push(BenchmarkDataFactory.CreateString(messageLength, 'l'));
        }

        private static void EmitNotifyArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            var elementCount = Math.Clamp(profile.CollectionLength, 1, 32);
            var elementSize = Math.Max(1, profile.DataLength / elementCount);
            for (int i = 0; i < elementCount; i++)
            {
                var seed = (byte)(0x60 + i);
                builder.Push(BenchmarkDataFactory.CreateByteArray(elementSize + i, seed));
            }
            builder.Push(elementCount);
            builder.AddInstruction(VM.OpCode.PACK);
            var eventNameLength = Math.Clamp(profile.DataLength / 4 + 4, 4, 48);
            builder.Push(BenchmarkDataFactory.CreateString(eventNameLength, 'e'));
        }

        private static void EmitGetNotificationsArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            builder.Push(s_calleeScriptHash.ToArray());
        }

        private static void EmitBurnGasArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            var datoshi = Math.Clamp(profile.DataLength + 1, 1, 10_000);
            builder.Push(datoshi);
        }

        private static void EmitCheckWitnessArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            var account = s_witnessAccounts[profile.Iterations % s_witnessAccounts.Length];
            builder.Push(account.GetSpan().ToArray());
        }

        private static void EmitCreateStandardAccountArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            var validators = s_standbyValidators;
            var validator = validators[profile.Iterations % validators.Count];
            builder.Push(validator.EncodePoint(true));
        }

        private static void EmitCreateMultisigAccountArguments(InstructionBuilder builder, ScenarioProfile profile)
        {
            var validators = s_standbyValidators;
            var available = validators.Count;
            var count = Math.Clamp(profile.CollectionLength, 2, Math.Min(available, 5));
            for (int i = 0; i < count; i++)
            {
                builder.Push(validators[i].EncodePoint(true));
            }
            builder.Push(count);
            builder.AddInstruction(VM.OpCode.PACK);
            var threshold = Math.Clamp(count / 2, 1, count);
            builder.Push(threshold);
        }

        private static BenchmarkApplicationEngine CreateTransactionBackedEngine(ScenarioProfile profile)
        {
            var transaction = new Transaction
            {
                Version = 0,
                Nonce = (uint)Math.Max(1, profile.Iterations),
                Signers = CreateSigners(),
                Witnesses = System.Array.Empty<Witness>(),
                Attributes = System.Array.Empty<TransactionAttribute>(),
                Script = System.Array.Empty<byte>(),
                NetworkFee = 0,
                SystemFee = 0,
                ValidUntilBlock = 100
            };

            return BenchmarkApplicationEngine.Create(container: transaction);
        }

        private static Signer[] CreateSigners()
        {
            var signers = new Signer[s_witnessAccounts.Length];
            for (int i = 0; i < s_witnessAccounts.Length; i++)
            {
                signers[i] = new Signer
                {
                    Account = s_witnessAccounts[i],
                    Scopes = WitnessScope.CalledByEntry
                };
            }
            return signers;
        }
    }
}
