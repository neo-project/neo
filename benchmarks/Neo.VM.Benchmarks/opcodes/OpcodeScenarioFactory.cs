// Copyright (C) 2015-2025 The Neo Project.
//
// OpcodeScenarioFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Benchmark;
using Neo.VM.Benchmark.Infrastructure;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Benchmark.OpCode
{
    internal static class OpcodeScenarioFactory
    {
        private sealed record OpcodeScenarioBuilder(Func<ScenarioProfile, OpcodeVmScenario> Factory)
        {
            public VmBenchmarkCase BuildCase(VM.OpCode opcode, ScenarioComplexity complexity)
            {
                var profile = ScenarioProfile.For(complexity);
                return new VmBenchmarkCase(opcode.ToString(), BenchmarkComponent.Opcode, complexity, Factory(profile));
            }
        }

        public static IEnumerable<VmBenchmarkCase> CreateCases()
        {
            var builders = CreateBuilders();
            foreach (ScenarioComplexity complexity in Enum.GetValues<ScenarioComplexity>())
            {
                foreach (var (opcode, builder) in builders.OrderBy(static kv => kv.Key))
                {
                    yield return builder.BuildCase(opcode, complexity);
                }
            }
        }

        public static IReadOnlyCollection<VM.OpCode> GetSupportedOpcodes()
        {
            return CreateBuilders().Keys.ToArray();
        }

        private static IReadOnlyDictionary<VM.OpCode, OpcodeScenarioBuilder> CreateBuilders()
        {
            var map = new Dictionary<VM.OpCode, OpcodeScenarioBuilder>();
            RegisterPush(map);
            RegisterStack(map);
            RegisterArithmetic(map);
            RegisterBitwise(map);
            RegisterLogic(map);
            RegisterSplice(map);
            RegisterCompound(map);
            RegisterSlots(map);
            RegisterTypes(map);
            RegisterControl(map);
            return map;
        }

        #region Registry helpers

        private static void RegisterPush(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            foreach (var opcode in Enum.GetValues<VM.OpCode>())
            {
                if (IsScalarPush(opcode))
                    map[opcode] = new OpcodeScenarioBuilder(profile => CreateScalarPushScenario(opcode, profile));
                else if (opcode is VM.OpCode.PUSHNULL)
                    map[opcode] = new OpcodeScenarioBuilder(CreatePushNullScenario);
                else if (opcode is VM.OpCode.PUSHINT8 or VM.OpCode.PUSHINT16 or VM.OpCode.PUSHINT32 or VM.OpCode.PUSHINT64)
                    map[opcode] = new OpcodeScenarioBuilder(profile => CreateNumericPushScenario(opcode, profile));
                else if (opcode is VM.OpCode.PUSHINT128 or VM.OpCode.PUSHINT256)
                    map[opcode] = new OpcodeScenarioBuilder(profile => CreateBigIntegerPushScenario(opcode, profile));
                else if (opcode is VM.OpCode.PUSHDATA1 or VM.OpCode.PUSHDATA2 or VM.OpCode.PUSHDATA4)
                    map[opcode] = new OpcodeScenarioBuilder(profile => CreatePushDataScenario(opcode, profile));
                else if (opcode is VM.OpCode.PUSHA)
                    map[opcode] = new OpcodeScenarioBuilder(profile => CreatePointerPushScenario(profile));
            }
        }

        private static void RegisterStack(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            map[VM.OpCode.DEPTH] = new OpcodeScenarioBuilder(profile => CreateDepthScenario(profile));
            map[VM.OpCode.DROP] = new OpcodeScenarioBuilder(profile => CreateDropScenario(profile));
            map[VM.OpCode.DUP] = new OpcodeScenarioBuilder(profile => CreateDupScenario(profile));
            map[VM.OpCode.SWAP] = new OpcodeScenarioBuilder(profile => CreateSwapScenario(profile));
            map[VM.OpCode.CLEAR] = new OpcodeScenarioBuilder(profile => CreateClearScenario(profile));
            map[VM.OpCode.NIP] = new OpcodeScenarioBuilder(profile => CreateNipScenario(profile));
            map[VM.OpCode.XDROP] = new OpcodeScenarioBuilder(profile => CreateXDropScenario(profile));
            map[VM.OpCode.OVER] = new OpcodeScenarioBuilder(profile => CreateOverScenario(profile));
            map[VM.OpCode.PICK] = new OpcodeScenarioBuilder(profile => CreatePickScenario(profile));
            map[VM.OpCode.TUCK] = new OpcodeScenarioBuilder(profile => CreateTuckScenario(profile));
            map[VM.OpCode.ROT] = new OpcodeScenarioBuilder(profile => CreateRotScenario(profile));
            map[VM.OpCode.ROLL] = new OpcodeScenarioBuilder(profile => CreateRollScenario(profile));
            map[VM.OpCode.REVERSE3] = new OpcodeScenarioBuilder(profile => CreateReverse3Scenario(profile));
            map[VM.OpCode.REVERSE4] = new OpcodeScenarioBuilder(profile => CreateReverse4Scenario(profile));
            map[VM.OpCode.REVERSEN] = new OpcodeScenarioBuilder(profile => CreateReverseNScenario(profile));
        }

        private static void RegisterArithmetic(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            foreach (var opcode in new[] { VM.OpCode.SIGN, VM.OpCode.ABS, VM.OpCode.NEGATE, VM.OpCode.INC, VM.OpCode.DEC, VM.OpCode.NZ })
            {
                map[opcode] = new OpcodeScenarioBuilder(profile => CreateUnaryNumericScenario(opcode, profile));
            }

            foreach (var opcode in new[] { VM.OpCode.ADD, VM.OpCode.SUB, VM.OpCode.MUL, VM.OpCode.DIV, VM.OpCode.MOD, VM.OpCode.MIN, VM.OpCode.MAX, VM.OpCode.MODMUL })
            {
                map[opcode] = new OpcodeScenarioBuilder(profile => CreateBinaryNumericScenario(opcode, profile));
            }

            map[VM.OpCode.SHL] = new OpcodeScenarioBuilder(profile => CreateShiftScenario(VM.OpCode.SHL, profile));
            map[VM.OpCode.SHR] = new OpcodeScenarioBuilder(profile => CreateShiftScenario(VM.OpCode.SHR, profile));
            map[VM.OpCode.POW] = new OpcodeScenarioBuilder(profile => CreateBinaryNumericScenario(VM.OpCode.POW, profile, left: 3, right: 3));
            map[VM.OpCode.SQRT] = new OpcodeScenarioBuilder(profile => CreateUnaryNumericScenario(VM.OpCode.SQRT, profile, operand: 16));
            map[VM.OpCode.MODPOW] = new OpcodeScenarioBuilder(profile => CreateTernaryNumericScenario(VM.OpCode.MODPOW, profile));
        }

        private static void RegisterBitwise(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            map[VM.OpCode.INVERT] = new OpcodeScenarioBuilder(profile => CreateInvertScenario(profile));
            foreach (var opcode in new[] { VM.OpCode.AND, VM.OpCode.OR, VM.OpCode.XOR })
            {
                map[opcode] = new OpcodeScenarioBuilder(profile => CreateBinaryNumericScenario(opcode, profile));
            }
            map[VM.OpCode.NOT] = new OpcodeScenarioBuilder(profile => CreateUnaryNumericScenario(VM.OpCode.NOT, profile));
        }

        private static void RegisterLogic(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            foreach (var opcode in new[] { VM.OpCode.BOOLAND, VM.OpCode.BOOLOR })
            {
                map[opcode] = new OpcodeScenarioBuilder(profile => CreateBooleanBinaryScenario(opcode, profile));
            }

            foreach (var opcode in new[] { VM.OpCode.EQUAL, VM.OpCode.NOTEQUAL, VM.OpCode.NUMEQUAL, VM.OpCode.NUMNOTEQUAL, VM.OpCode.LT, VM.OpCode.LE, VM.OpCode.GT, VM.OpCode.GE })
            {
                map[opcode] = new OpcodeScenarioBuilder(profile => CreateBinaryNumericScenario(opcode, profile));
            }

            map[VM.OpCode.WITHIN] = new OpcodeScenarioBuilder(profile => CreateWithinScenario(profile));
        }

        private static void RegisterSplice(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            map[VM.OpCode.NEWBUFFER] = new OpcodeScenarioBuilder(profile => CreateNewBufferScenario(profile));
            map[VM.OpCode.MEMCPY] = new OpcodeScenarioBuilder(profile => CreateMemcpyScenario(profile));
            map[VM.OpCode.CAT] = new OpcodeScenarioBuilder(profile => CreateConcatScenario(profile));
            map[VM.OpCode.SUBSTR] = new OpcodeScenarioBuilder(profile => CreateSubstringScenario(VM.OpCode.SUBSTR, profile));
            map[VM.OpCode.LEFT] = new OpcodeScenarioBuilder(profile => CreateSubstringScenario(VM.OpCode.LEFT, profile));
            map[VM.OpCode.RIGHT] = new OpcodeScenarioBuilder(profile => CreateSubstringScenario(VM.OpCode.RIGHT, profile));
        }

        private static void RegisterCompound(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            map[VM.OpCode.PACK] = new OpcodeScenarioBuilder(profile => CreatePackScenario(VM.OpCode.PACK, profile));
            map[VM.OpCode.PACKSTRUCT] = new OpcodeScenarioBuilder(profile => CreatePackScenario(VM.OpCode.PACKSTRUCT, profile));
            map[VM.OpCode.PACKMAP] = new OpcodeScenarioBuilder(profile => CreatePackMapScenario(profile));
            map[VM.OpCode.UNPACK] = new OpcodeScenarioBuilder(profile => CreateUnpackScenario(profile));
            map[VM.OpCode.NEWARRAY0] = new OpcodeScenarioBuilder(profile => CreateSimpleAllocationScenario(VM.OpCode.NEWARRAY0, profile));
            map[VM.OpCode.NEWSTRUCT0] = new OpcodeScenarioBuilder(profile => CreateSimpleAllocationScenario(VM.OpCode.NEWSTRUCT0, profile));
            map[VM.OpCode.NEWARRAY] = new OpcodeScenarioBuilder(profile => CreateNewArrayScenario(VM.OpCode.NEWARRAY, profile));
            map[VM.OpCode.NEWARRAY_T] = new OpcodeScenarioBuilder(profile => CreateNewArrayTypedScenario(profile));
            map[VM.OpCode.NEWSTRUCT] = new OpcodeScenarioBuilder(profile => CreateNewArrayScenario(VM.OpCode.NEWSTRUCT, profile));
            map[VM.OpCode.NEWMAP] = new OpcodeScenarioBuilder(profile => CreateSimpleAllocationScenario(VM.OpCode.NEWMAP, profile));
            map[VM.OpCode.SIZE] = new OpcodeScenarioBuilder(profile => CreateSizeScenario(profile));
            map[VM.OpCode.HASKEY] = new OpcodeScenarioBuilder(profile => CreateHasKeyScenario(profile));
            map[VM.OpCode.KEYS] = new OpcodeScenarioBuilder(profile => CreateKeysScenario(profile));
            map[VM.OpCode.VALUES] = new OpcodeScenarioBuilder(profile => CreateValuesScenario(profile));
            map[VM.OpCode.PICKITEM] = new OpcodeScenarioBuilder(profile => CreatePickItemScenario(profile));
            map[VM.OpCode.SETITEM] = new OpcodeScenarioBuilder(profile => CreateSetItemScenario(profile));
            map[VM.OpCode.REMOVE] = new OpcodeScenarioBuilder(profile => CreateRemoveScenario(profile));
            map[VM.OpCode.CLEARITEMS] = new OpcodeScenarioBuilder(profile => CreateClearItemsScenario(profile));
            map[VM.OpCode.REVERSEITEMS] = new OpcodeScenarioBuilder(profile => CreateReverseItemsScenario(profile));
            map[VM.OpCode.APPEND] = new OpcodeScenarioBuilder(profile => CreateAppendScenario(profile));
            map[VM.OpCode.POPITEM] = new OpcodeScenarioBuilder(profile => CreatePopItemScenario(profile));
        }

        private static void RegisterSlots(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            map[VM.OpCode.INITSLOT] = new OpcodeScenarioBuilder(profile => CreateInitSlotScenario(VM.OpCode.INITSLOT, profile));
            map[VM.OpCode.INITSSLOT] = new OpcodeScenarioBuilder(profile => CreateInitSlotScenario(VM.OpCode.INITSSLOT, profile));

            RegisterLoadStores(map,
                new[] { VM.OpCode.LDLOC0, VM.OpCode.LDLOC1, VM.OpCode.LDLOC2, VM.OpCode.LDLOC3, VM.OpCode.LDLOC4, VM.OpCode.LDLOC5, VM.OpCode.LDLOC6 },
                VM.OpCode.LDLOC,
                CreateLocalLoadScenario);

            RegisterLoadStores(map,
                new[] { VM.OpCode.STLOC0, VM.OpCode.STLOC1, VM.OpCode.STLOC2, VM.OpCode.STLOC3, VM.OpCode.STLOC4, VM.OpCode.STLOC5, VM.OpCode.STLOC6 },
                VM.OpCode.STLOC,
                CreateLocalStoreScenario);

            RegisterLoadStores(map,
                new[] { VM.OpCode.LDARG0, VM.OpCode.LDARG1, VM.OpCode.LDARG2, VM.OpCode.LDARG3, VM.OpCode.LDARG4, VM.OpCode.LDARG5, VM.OpCode.LDARG6 },
                VM.OpCode.LDARG,
                CreateArgumentLoadScenario);

            RegisterLoadStores(map,
                new[] { VM.OpCode.STARG0, VM.OpCode.STARG1, VM.OpCode.STARG2, VM.OpCode.STARG3, VM.OpCode.STARG4, VM.OpCode.STARG5, VM.OpCode.STARG6 },
                VM.OpCode.STARG,
                CreateArgumentStoreScenario);

            RegisterLoadStores(map,
                new[] { VM.OpCode.LDSFLD0, VM.OpCode.LDSFLD1, VM.OpCode.LDSFLD2, VM.OpCode.LDSFLD3, VM.OpCode.LDSFLD4, VM.OpCode.LDSFLD5, VM.OpCode.LDSFLD6 },
                VM.OpCode.LDSFLD,
                CreateStaticLoadScenario);

            RegisterLoadStores(map,
                new[] { VM.OpCode.STSFLD0, VM.OpCode.STSFLD1, VM.OpCode.STSFLD2, VM.OpCode.STSFLD3, VM.OpCode.STSFLD4, VM.OpCode.STSFLD5, VM.OpCode.STSFLD6 },
                VM.OpCode.STSFLD,
                CreateStaticStoreScenario);
        }

        private static void RegisterTypes(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            map[VM.OpCode.ISNULL] = new OpcodeScenarioBuilder(profile => CreateIsNullScenario(profile));
            map[VM.OpCode.ISTYPE] = new OpcodeScenarioBuilder(profile => CreateIsTypeScenario(profile));
            map[VM.OpCode.CONVERT] = new OpcodeScenarioBuilder(profile => CreateConvertScenario(profile));
        }

        private static void RegisterControl(IDictionary<VM.OpCode, OpcodeScenarioBuilder> map)
        {
            map[VM.OpCode.NOP] = new OpcodeScenarioBuilder(profile => CreateSimpleControlScenario(VM.OpCode.NOP, profile));
            map[VM.OpCode.RET] = new OpcodeScenarioBuilder(profile => CreateSimpleControlScenario(VM.OpCode.RET, profile));

            foreach (var opcode in new[] { VM.OpCode.JMP, VM.OpCode.JMP_L })
                map[opcode] = new OpcodeScenarioBuilder(profile => CreateJumpScenario(opcode, profile));

            foreach (var opcode in new[]
                     {
                         VM.OpCode.JMPIF,
                         VM.OpCode.JMPIF_L,
                         VM.OpCode.JMPIFNOT,
                         VM.OpCode.JMPIFNOT_L,
                         VM.OpCode.JMPEQ,
                         VM.OpCode.JMPEQ_L,
                         VM.OpCode.JMPNE,
                         VM.OpCode.JMPNE_L,
                         VM.OpCode.JMPGT,
                         VM.OpCode.JMPGT_L,
                         VM.OpCode.JMPGE,
                         VM.OpCode.JMPGE_L,
                         VM.OpCode.JMPLT,
                         VM.OpCode.JMPLT_L,
                         VM.OpCode.JMPLE,
                         VM.OpCode.JMPLE_L
                     })
            {
                map[opcode] = new OpcodeScenarioBuilder(profile => CreateConditionalJumpScenario(opcode, profile));
            }

            foreach (var opcode in new[] { VM.OpCode.CALL, VM.OpCode.CALL_L, VM.OpCode.CALLA })
                map[opcode] = new OpcodeScenarioBuilder(profile => CreateCallScenario(opcode, profile));

            map[VM.OpCode.CALLT] = new OpcodeScenarioBuilder(profile => CreateCallTSimpleScenario(profile));
            map[VM.OpCode.ABORT] = new OpcodeScenarioBuilder(profile => CreateAbortScenario(VM.OpCode.ABORT, profile));
            map[VM.OpCode.ABORTMSG] = new OpcodeScenarioBuilder(profile => CreateAbortScenario(VM.OpCode.ABORTMSG, profile));
            map[VM.OpCode.ASSERT] = new OpcodeScenarioBuilder(profile => CreateAssertScenario(VM.OpCode.ASSERT, profile));
            map[VM.OpCode.ASSERTMSG] = new OpcodeScenarioBuilder(profile => CreateAssertScenario(VM.OpCode.ASSERTMSG, profile));
            map[VM.OpCode.THROW] = new OpcodeScenarioBuilder(profile => CreateThrowScenario(profile));
            map[VM.OpCode.TRY] = new OpcodeScenarioBuilder(profile => CreateTryScenario(VM.OpCode.TRY, profile));
            map[VM.OpCode.TRY_L] = new OpcodeScenarioBuilder(profile => CreateTryScenario(VM.OpCode.TRY_L, profile));
            map[VM.OpCode.ENDTRY] = new OpcodeScenarioBuilder(profile => CreateSimpleControlScenario(VM.OpCode.ENDTRY, profile));
            map[VM.OpCode.ENDTRY_L] = new OpcodeScenarioBuilder(profile => CreateSimpleControlScenario(VM.OpCode.ENDTRY_L, profile));
            map[VM.OpCode.ENDFINALLY] = new OpcodeScenarioBuilder(profile => CreateSimpleControlScenario(VM.OpCode.ENDFINALLY, profile));
            map[VM.OpCode.SYSCALL] = new OpcodeScenarioBuilder(profile => CreateSyscallScenario(profile));
        }

        private static void RegisterLoadStores(
            IDictionary<VM.OpCode, OpcodeScenarioBuilder> map,
            IReadOnlyList<VM.OpCode> fixedOpcodes,
            VM.OpCode paramOpcode,
            Func<VM.OpCode, ScenarioProfile, byte, byte?, OpcodeVmScenario> factory)
        {
            for (byte i = 0; i < fixedOpcodes.Count; i++)
            {
                var index = i;
                var fixedOpcode = fixedOpcodes[index];
                map[fixedOpcode] = new OpcodeScenarioBuilder(profile => factory(fixedOpcode, profile, index, null));
            }
            map[paramOpcode] = new OpcodeScenarioBuilder(profile => factory(paramOpcode, profile, 2, 2));
        }

        #endregion

        #region Scenario creators

        private static OpcodeVmScenario CreateScalarPushScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.AddInstruction(VM.OpCode.PUSH0);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder => builder.AddInstruction(opcode));
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder => builder.AddInstruction(opcode));
            return CreateScenario(opcode, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == opcode) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateNumericPushScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            int size = opcode switch
            {
                VM.OpCode.PUSHINT8 => 1,
                VM.OpCode.PUSHINT16 => 2,
                VM.OpCode.PUSHINT32 => 4,
                VM.OpCode.PUSHINT64 => 8,
                _ => throw new ArgumentOutOfRangeException(nameof(opcode))
            };
            var operand = new byte[size];
            operand[^1] = 0x01;
            return CreatePushWithOperand(opcode, profile, operand);
        }

        private static OpcodeVmScenario CreateBigIntegerPushScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            int size = opcode == VM.OpCode.PUSHINT128 ? 16 : 32;
            var operand = new byte[size];
            operand[^1] = 0x01;
            return CreatePushWithOperand(opcode, profile, operand);
        }

        private static OpcodeVmScenario CreatePushDataScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            int payloadSize = opcode switch
            {
                VM.OpCode.PUSHDATA1 => Math.Min(profile.DataLength, byte.MaxValue),
                VM.OpCode.PUSHDATA2 => Math.Clamp(profile.DataLength * 4, byte.MaxValue + 1, ushort.MaxValue),
                VM.OpCode.PUSHDATA4 => Math.Clamp(profile.DataLength * 16, ushort.MaxValue + 1, 128 * 1024),
                _ => throw new ArgumentOutOfRangeException(nameof(opcode))
            };
            var payload = Enumerable.Range(0, payloadSize).Select(i => (byte)(i % 256)).ToArray();
            var operand = opcode switch
            {
                VM.OpCode.PUSHDATA1 => BuildOperand(1, payload),
                VM.OpCode.PUSHDATA2 => BuildOperand(2, payload),
                VM.OpCode.PUSHDATA4 => BuildOperand(4, payload),
                _ => throw new ArgumentOutOfRangeException(nameof(opcode))
            };
            return CreatePushWithOperand(opcode, profile, operand);
        }

        private static OpcodeVmScenario CreatePointerPushScenario(ScenarioProfile profile)
        {
            var operand = BitConverter.GetBytes(0);
            return CreatePushWithOperand(VM.OpCode.PUSHA, profile, operand);
        }

        private static OpcodeVmScenario CreatePushNullScenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.AddInstruction(VM.OpCode.PUSH0);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.AddInstruction(VM.OpCode.PUSHNULL);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.AddInstruction(VM.OpCode.PUSHNULL);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.PUSHNULL, profile, baseline, single, saturated,
                after: (engine, instruction) =>
                {
                    if (instruction.OpCode == VM.OpCode.PUSHNULL)
                        engine.Pop();
                });
        }

        private static OpcodeVmScenario CreatePushWithOperand(VM.OpCode opcode, ScenarioProfile profile, byte[] operand)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.AddInstruction(VM.OpCode.PUSH0);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder => builder.AddInstruction(new Instruction { _opCode = opcode, _operand = operand }));
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder => builder.AddInstruction(new Instruction { _opCode = opcode, _operand = operand }));
            return CreateScenario(opcode, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == opcode) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateDepthScenario(ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder => builder.AddInstruction(VM.OpCode.DEPTH));
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder => builder.AddInstruction(VM.OpCode.DEPTH));
            return CreateScenario(VM.OpCode.DEPTH, profile, baseline, single, saturated,
                before: (engine, instruction) => SeedStack(engine, CreateSequentialIntegers(Math.Min(8, profile.CollectionLength))),
                after: (engine, instruction) => { if (instruction.OpCode == VM.OpCode.DEPTH) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateDropScenario(ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder => builder.AddInstruction(VM.OpCode.DROP));
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder => builder.AddInstruction(VM.OpCode.DROP));
            return CreateScenario(VM.OpCode.DROP, profile, baseline, single, saturated,
                before: (engine, instruction) => SeedStack(engine, true));
        }

        private static OpcodeVmScenario CreateDupScenario(ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder => builder.AddInstruction(VM.OpCode.DUP));
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder => builder.AddInstruction(VM.OpCode.DUP));
            return CreateScenario(VM.OpCode.DUP, profile, baseline, single, saturated,
                before: (engine, instruction) => SeedStack(engine, true),
                after: (engine, instruction) => { if (instruction.OpCode == VM.OpCode.DUP) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateSwapScenario(ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 2);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder => builder.AddInstruction(VM.OpCode.SWAP));
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder => builder.AddInstruction(VM.OpCode.SWAP));
            return CreateScenario(VM.OpCode.SWAP, profile, baseline, single, saturated,
                before: (engine, instruction) => SeedStack(engine, true, false));
        }

        private static OpcodeVmScenario CreateClearScenario(ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 2);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder => builder.AddInstruction(VM.OpCode.CLEAR));
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder => builder.AddInstruction(VM.OpCode.CLEAR));
            return CreateScenario(VM.OpCode.CLEAR, profile, baseline, single, saturated,
                before: (engine, instruction) => SeedStack(engine, true, false));
        }

        private static OpcodeVmScenario CreateNipScenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.NIP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.NIP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.NIP, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateXDropScenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(0);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(1);
                builder.AddInstruction(VM.OpCode.XDROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(1);
                builder.AddInstruction(VM.OpCode.XDROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.XDROP, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateOverScenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.OVER);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.OVER);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.OVER, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreatePickScenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(0);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.PICK);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.PICK);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.PICK, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateTuckScenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.TUCK);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.TUCK);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.TUCK, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateRotScenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.AddInstruction(VM.OpCode.ROT);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.AddInstruction(VM.OpCode.ROT);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.ROT, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateRollScenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(0);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.ROLL);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(2);
                builder.AddInstruction(VM.OpCode.ROLL);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.ROLL, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateReverse3Scenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.AddInstruction(VM.OpCode.REVERSE3);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.AddInstruction(VM.OpCode.REVERSE3);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.REVERSE3, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateReverse4Scenario(ScenarioProfile profile)
        {
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(4);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(4);
                builder.AddInstruction(VM.OpCode.REVERSE4);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(1);
                builder.Push(2);
                builder.Push(3);
                builder.Push(4);
                builder.AddInstruction(VM.OpCode.REVERSE4);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.REVERSE4, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateReverseNScenario(ScenarioProfile profile)
        {
            var count = Math.Clamp(profile.CollectionLength, 2, 8);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitSequentialPushes(builder, count);
                builder.Push(count);
                builder.AddInstruction(VM.OpCode.DROP);
                for (int i = 0; i < count; i++) builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitSequentialPushes(builder, count);
                builder.Push(count);
                builder.AddInstruction(VM.OpCode.REVERSEN);
                for (int i = 0; i < count; i++) builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitSequentialPushes(builder, count);
                builder.Push(count);
                builder.AddInstruction(VM.OpCode.REVERSEN);
                for (int i = 0; i < count; i++) builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.REVERSEN, profile, baseline, single, saturated);
        }

        private static void EmitSequentialPushes(InstructionBuilder builder, int count)
        {
            for (int i = 0; i < count; i++)
                builder.Push(i + 1);
        }

        private static OpcodeVmScenario CreateUnaryNumericScenario(VM.OpCode opcode, ScenarioProfile profile, int operand = 3)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(operand);
                builder.AddInstruction(opcode);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(operand);
                builder.AddInstruction(opcode);
            });
            return CreateScenario(opcode, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == opcode) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateBinaryNumericScenario(VM.OpCode opcode, ScenarioProfile profile, int left = 5, int right = 2)
        {
            var baseline = CreateBaselineScript(profile, 2);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(left);
                builder.Push(right);
                builder.AddInstruction(opcode);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(left);
                builder.Push(right);
                builder.AddInstruction(opcode);
            });
            return CreateScenario(opcode, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == opcode) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateTernaryNumericScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 3);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(5);
                builder.Push(3);
                builder.Push(7);
                builder.AddInstruction(opcode);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(5);
                builder.Push(3);
                builder.Push(7);
                builder.AddInstruction(opcode);
            });
            return CreateScenario(opcode, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == opcode) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateShiftScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 2);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(10);
                builder.Push(1);
                builder.AddInstruction(opcode);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(10);
                builder.Push(1);
                builder.AddInstruction(opcode);
            });
            return CreateScenario(opcode, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == opcode) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateInvertScenario(ScenarioProfile profile)
        {
            var data = Enumerable.Repeat((byte)0xAA, Math.Max(8, profile.DataLength / 4)).ToArray();
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(data);
                builder.AddInstruction(VM.OpCode.INVERT);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(data);
                builder.AddInstruction(VM.OpCode.INVERT);
            });
            return CreateScenario(VM.OpCode.INVERT, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == VM.OpCode.INVERT) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateBooleanBinaryScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 2);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(true);
                builder.Push(false);
                builder.AddInstruction(opcode);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(true);
                builder.Push(false);
                builder.AddInstruction(opcode);
            });
            return CreateScenario(opcode, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == opcode) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateWithinScenario(ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 3);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(2);
                builder.Push(1);
                builder.Push(4);
                builder.AddInstruction(VM.OpCode.WITHIN);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(2);
                builder.Push(1);
                builder.Push(4);
                builder.AddInstruction(VM.OpCode.WITHIN);
            });
            return CreateScenario(VM.OpCode.WITHIN, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == VM.OpCode.WITHIN) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateNewBufferScenario(ScenarioProfile profile)
        {
            var size = Math.Max(16, profile.DataLength);
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(size);
                builder.AddInstruction(VM.OpCode.NEWBUFFER);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(size);
                builder.AddInstruction(VM.OpCode.NEWBUFFER);
            });
            return CreateScenario(VM.OpCode.NEWBUFFER, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == VM.OpCode.NEWBUFFER) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateMemcpyScenario(ScenarioProfile profile)
        {
            var size = Math.Max(16, profile.DataLength / 2);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(new byte[size]);
                builder.Push(0);
                builder.Push(new byte[size]);
                builder.Push(0);
                builder.Push(size);
                for (int i = 0; i < 5; i++) builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(new byte[size]);
                builder.Push(0);
                builder.Push(new byte[size]);
                builder.Push(0);
                builder.Push(size);
                builder.AddInstruction(VM.OpCode.MEMCPY);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(new byte[size]);
                builder.Push(0);
                builder.Push(new byte[size]);
                builder.Push(0);
                builder.Push(size);
                builder.AddInstruction(VM.OpCode.MEMCPY);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.MEMCPY, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateConcatScenario(ScenarioProfile profile)
        {
            var size = Math.Max(16, profile.DataLength / 4);
            var payloadA = Enumerable.Repeat((byte)0xAA, size).ToArray();
            var payloadB = Enumerable.Repeat((byte)0xBB, size).ToArray();
            var baseline = CreateBaselineScript(profile, 2);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(payloadA);
                builder.Push(payloadB);
                builder.AddInstruction(VM.OpCode.CAT);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(payloadA);
                builder.Push(payloadB);
                builder.AddInstruction(VM.OpCode.CAT);
            });
            return CreateScenario(VM.OpCode.CAT, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == VM.OpCode.CAT) engine.Pop(); });
        }

        private static OpcodeVmScenario CreateSubstringScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var dataLength = Math.Max(32, profile.DataLength);
            var payload = Enumerable.Range(0, dataLength).Select(i => (byte)(i % 256)).ToArray();
            var count = Math.Max(8, dataLength / 4);
            var baseline = CreateBaselineScript(profile, opcode == VM.OpCode.SUBSTR ? 3 : 2);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(payload);
                if (opcode == VM.OpCode.SUBSTR)
                {
                    builder.Push(0);
                    builder.Push(count);
                }
                else
                {
                    builder.Push(count);
                }
                builder.AddInstruction(opcode);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(payload);
                if (opcode == VM.OpCode.SUBSTR)
                {
                    builder.Push(0);
                    builder.Push(count);
                }
                else
                {
                    builder.Push(count);
                }
                builder.AddInstruction(opcode);
            });
            return CreateScenario(opcode, profile, baseline, single, saturated,
                after: (engine, instruction) => { if (instruction.OpCode == opcode) engine.Pop(); });
        }

        private static OpcodeVmScenario CreatePackScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                foreach (var value in values) builder.Push(value);
                builder.Push(values.Length);
                for (var i = 0; i < values.Length + 1; i++) builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                foreach (var value in values) builder.Push(value);
                builder.Push(values.Length);
                builder.AddInstruction(opcode);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                foreach (var value in values) builder.Push(value);
                builder.Push(values.Length);
                builder.AddInstruction(opcode);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(opcode, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreatePackMapScenario(ScenarioProfile profile)
        {
            var entries = GetSampleMapEntries();
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                foreach (var entry in entries)
                {
                    builder.Push(entry.value);
                    builder.Push(entry.key);
                }
                builder.Push(entries.Count);
                for (int i = 0; i < entries.Count * 2 + 1; i++) builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                foreach (var entry in entries)
                {
                    builder.Push(entry.value);
                    builder.Push(entry.key);
                }
                builder.Push(entries.Count);
                builder.AddInstruction(VM.OpCode.PACKMAP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                foreach (var entry in entries)
                {
                    builder.Push(entry.value);
                    builder.Push(entry.key);
                }
                builder.Push(entries.Count);
                builder.AddInstruction(VM.OpCode.PACKMAP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.PACKMAP, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateUnpackScenario(ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                foreach (var value in values) builder.Push(value);
                builder.Push(values.Length);
                builder.AddInstruction(VM.OpCode.PACK);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                foreach (var value in values) builder.Push(value);
                builder.Push(values.Length);
                builder.AddInstruction(VM.OpCode.PACK);
                builder.AddInstruction(VM.OpCode.UNPACK);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                foreach (var value in values) builder.Push(value);
                builder.Push(values.Length);
                builder.AddInstruction(VM.OpCode.PACK);
                builder.AddInstruction(VM.OpCode.UNPACK);
            });
            return CreateScenario(VM.OpCode.UNPACK, profile, baseline, single, saturated,
                after: (engine, instruction) =>
                {
                    if (instruction.OpCode != VM.OpCode.UNPACK) return;
                    var count = (int)engine.Pop().GetInteger();
                    for (int i = 0; i < count; i++) engine.Pop();
                });
        }

        private static OpcodeVmScenario CreateSimpleAllocationScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 0);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.AddInstruction(opcode);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.AddInstruction(opcode);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(opcode, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateNewArrayScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var length = Math.Clamp(profile.CollectionLength, 1, 32);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(length);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(length);
                builder.AddInstruction(opcode);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(length);
                builder.AddInstruction(opcode);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(opcode, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateNewArrayTypedScenario(ScenarioProfile profile)
        {
            var length = Math.Clamp(profile.CollectionLength, 1, 32);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push((byte)StackItemType.Integer);
                builder.Push(length);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push((byte)StackItemType.Integer);
                builder.Push(length);
                builder.AddInstruction(VM.OpCode.NEWARRAY_T);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push((byte)StackItemType.Integer);
                builder.Push(length);
                builder.AddInstruction(VM.OpCode.NEWARRAY_T);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.NEWARRAY_T, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateSizeScenario(ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.SIZE);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.SIZE);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.SIZE, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateHasKeyScenario(ScenarioProfile profile)
        {
            var entries = GetSampleMapEntries();
            var key = entries[0].key;
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedMap(builder, entries);
                builder.Push(key);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedMap(builder, entries);
                builder.Push(key);
                builder.AddInstruction(VM.OpCode.HASKEY);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedMap(builder, entries);
                builder.Push(key);
                builder.AddInstruction(VM.OpCode.HASKEY);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.HASKEY, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateKeysScenario(ScenarioProfile profile)
        {
            var entries = GetSampleMapEntries();
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedMap(builder, entries);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedMap(builder, entries);
                builder.AddInstruction(VM.OpCode.KEYS);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedMap(builder, entries);
                builder.AddInstruction(VM.OpCode.KEYS);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.KEYS, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateValuesScenario(ScenarioProfile profile)
        {
            var entries = GetSampleMapEntries();
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedMap(builder, entries);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedMap(builder, entries);
                builder.AddInstruction(VM.OpCode.VALUES);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedMap(builder, entries);
                builder.AddInstruction(VM.OpCode.VALUES);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.VALUES, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreatePickItemScenario(ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var index = 1 % values.Length;
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.Push(index);
                builder.AddInstruction(VM.OpCode.PICKITEM);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedArray(builder, values);
                builder.Push(index);
                builder.AddInstruction(VM.OpCode.PICKITEM);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.PICKITEM, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateSetItemScenario(ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var index = 1 % values.Length;
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.Push(index);
                builder.Push(99);
                builder.AddInstruction(VM.OpCode.SETITEM);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedArray(builder, values);
                builder.Push(index);
                builder.Push(99);
                builder.AddInstruction(VM.OpCode.SETITEM);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.SETITEM, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateRemoveScenario(ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var index = 1 % values.Length;
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.Push(index);
                builder.AddInstruction(VM.OpCode.REMOVE);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedArray(builder, values);
                builder.Push(index);
                builder.AddInstruction(VM.OpCode.REMOVE);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.REMOVE, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateClearItemsScenario(ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.CLEARITEMS);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.CLEARITEMS);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.CLEARITEMS, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateReverseItemsScenario(ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.REVERSEITEMS);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.REVERSEITEMS);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.REVERSEITEMS, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateAppendScenario(ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.Push(99);
                builder.AddInstruction(VM.OpCode.APPEND);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedArray(builder, values);
                builder.Push(99);
                builder.AddInstruction(VM.OpCode.APPEND);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.APPEND, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreatePopItemScenario(ScenarioProfile profile)
        {
            var values = GetSampleArrayValues(profile);
            var baseline = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.POPITEM);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitPackedArray(builder, values);
                builder.AddInstruction(VM.OpCode.POPITEM);
                builder.AddInstruction(VM.OpCode.DROP);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.POPITEM, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateInitSlotScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var operand = opcode == VM.OpCode.INITSLOT ? new byte[] { 1, 0 } : new byte[] { 1 };
            var script = BuildScript(builder =>
            {
                builder.AddInstruction(new Instruction { _opCode = opcode, _operand = operand });
                builder.AddInstruction(VM.OpCode.RET);
            });
            var baseline = BuildRetScript();
            return CreateScenario(opcode, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateLocalLoadScenario(VM.OpCode opcode, ScenarioProfile profile, byte index, byte? operand)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitInstruction(builder, opcode, operand);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitInstruction(builder, opcode, operand);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var seeded = false;
            return CreateScenario(opcode, profile, baseline, single, saturated,
                before: (engine, instruction) =>
                {
                    if (seeded) return;
                    PrepareLocalSlot(engine, index, 7);
                    seeded = true;
                });
        }

        private static OpcodeVmScenario CreateLocalStoreScenario(VM.OpCode opcode, ScenarioProfile profile, byte index, byte? operand)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(8);
                EmitInstruction(builder, opcode, operand);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(8);
                EmitInstruction(builder, opcode, operand);
            });
            var seeded = false;
            return CreateScenario(opcode, profile, baseline, single, saturated,
                before: (engine, instruction) =>
                {
                    if (seeded) return;
                    EnsureLocalSlot(engine, index);
                    seeded = true;
                });
        }

        private static OpcodeVmScenario CreateArgumentLoadScenario(VM.OpCode opcode, ScenarioProfile profile, byte index, byte? operand)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitInstruction(builder, opcode, operand);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitInstruction(builder, opcode, operand);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var seeded = false;
            return CreateScenario(opcode, profile, baseline, single, saturated,
                before: (engine, instruction) =>
                {
                    if (seeded) return;
                    PrepareArgumentSlot(engine, index, 3);
                    seeded = true;
                });
        }

        private static OpcodeVmScenario CreateArgumentStoreScenario(VM.OpCode opcode, ScenarioProfile profile, byte index, byte? operand)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(4);
                EmitInstruction(builder, opcode, operand);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(4);
                EmitInstruction(builder, opcode, operand);
            });
            var seeded = false;
            return CreateScenario(opcode, profile, baseline, single, saturated,
                before: (engine, instruction) =>
                {
                    if (seeded) return;
                    EnsureArgumentSlot(engine, index);
                    seeded = true;
                });
        }

        private static OpcodeVmScenario CreateStaticLoadScenario(VM.OpCode opcode, ScenarioProfile profile, byte index, byte? operand)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                EmitInstruction(builder, opcode, operand);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                EmitInstruction(builder, opcode, operand);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var seeded = false;
            return CreateScenario(opcode, profile, baseline, single, saturated,
                before: (engine, instruction) =>
                {
                    if (seeded) return;
                    PrepareStaticSlot(engine, index, 9);
                    seeded = true;
                });
        }

        private static OpcodeVmScenario CreateStaticStoreScenario(VM.OpCode opcode, ScenarioProfile profile, byte index, byte? operand)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(6);
                EmitInstruction(builder, opcode, operand);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(6);
                EmitInstruction(builder, opcode, operand);
            });
            var seeded = false;
            return CreateScenario(opcode, profile, baseline, single, saturated,
                before: (engine, instruction) =>
                {
                    if (seeded) return;
                    EnsureStaticSlot(engine, index);
                    seeded = true;
                });
        }

        private static OpcodeVmScenario CreateIsNullScenario(ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.AddInstruction(VM.OpCode.PUSHNULL);
                builder.AddInstruction(VM.OpCode.ISNULL);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.AddInstruction(VM.OpCode.PUSHNULL);
                builder.AddInstruction(VM.OpCode.ISNULL);
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.ISNULL, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateIsTypeScenario(ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(5);
                builder.AddInstruction(new Instruction
                {
                    _opCode = VM.OpCode.ISTYPE,
                    _operand = new[] { (byte)StackItemType.Integer }
                });
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(5);
                builder.AddInstruction(new Instruction
                {
                    _opCode = VM.OpCode.ISTYPE,
                    _operand = new[] { (byte)StackItemType.Integer }
                });
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.ISTYPE, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateConvertScenario(ScenarioProfile profile)
        {
            var baseline = CreateBaselineScript(profile, 1);
            var single = LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                builder.Push(5);
                builder.AddInstruction(new Instruction
                {
                    _opCode = VM.OpCode.CONVERT,
                    _operand = new[] { (byte)StackItemType.ByteString }
                });
                builder.AddInstruction(VM.OpCode.DROP);
            });
            var saturated = LoopScriptFactory.BuildInfiniteLoop(builder =>
            {
                builder.Push(5);
                builder.AddInstruction(new Instruction
                {
                    _opCode = VM.OpCode.CONVERT,
                    _operand = new[] { (byte)StackItemType.ByteString }
                });
                builder.AddInstruction(VM.OpCode.DROP);
            });
            return CreateScenario(VM.OpCode.CONVERT, profile, baseline, single, saturated);
        }

        private static OpcodeVmScenario CreateSimpleControlScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var baseline = BuildRetScript();
            var script = BuildScript(builder =>
            {
                builder.AddInstruction(opcode);
                builder.AddInstruction(VM.OpCode.RET);
            });
            return CreateScenario(opcode, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateJumpScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var builder = new InstructionBuilder();
            var offset = opcode == VM.OpCode.JMP ? new[] { unchecked((byte)0x02) } : BitConverter.GetBytes(2);
            builder.AddInstruction(new Instruction { _opCode = opcode, _operand = offset });
            builder.AddInstruction(VM.OpCode.NOP);
            builder.AddInstruction(VM.OpCode.RET);
            var script = builder.ToArray();
            var baseline = BuildRetScript();
            return CreateScenario(opcode, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateConditionalJumpScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var offset = opcode switch
            {
                VM.OpCode.JMPIF or VM.OpCode.JMPIFNOT or VM.OpCode.JMPEQ or VM.OpCode.JMPNE or VM.OpCode.JMPGT or VM.OpCode.JMPGE or VM.OpCode.JMPLT or VM.OpCode.JMPLE => new[] { unchecked((byte)0x02) },
                _ => BitConverter.GetBytes(2)
            };
            var operands = GetConditionalOperands(opcode);
            var script = BuildScript(builder =>
            {
                foreach (var value in operands)
                    builder.Push(value);
                builder.AddInstruction(new Instruction { _opCode = opcode, _operand = offset });
                builder.AddInstruction(VM.OpCode.NOP);
                builder.AddInstruction(VM.OpCode.RET);
            });
            var baseline = BuildRetScript();
            return CreateScenario(opcode, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateCallScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var builder = new InstructionBuilder();
            var method = builder.AddInstruction(VM.OpCode.NOP);
            builder.AddInstruction(VM.OpCode.RET);
            var methodOffset = method._offset;

            builder.AddInstruction(VM.OpCode.PUSH0);
            switch (opcode)
            {
                case VM.OpCode.CALL:
                    builder.AddInstruction(new Instruction
                    {
                        _opCode = VM.OpCode.CALL,
                        _operand = new[] { unchecked((byte)(methodOffset - builder._instructions[^1]._offset - 2)) }
                    });
                    break;
                case VM.OpCode.CALL_L:
                    builder.AddInstruction(new Instruction
                    {
                        _opCode = VM.OpCode.CALL_L,
                        _operand = BitConverter.GetBytes(methodOffset - builder._instructions[^1]._offset - 5)
                    });
                    break;
                case VM.OpCode.CALLA:
                    builder.AddInstruction(new Instruction
                    {
                        _opCode = VM.OpCode.PUSHA,
                        _operand = BitConverter.GetBytes(methodOffset)
                    });
                    builder.AddInstruction(VM.OpCode.CALLA);
                    break;
                case VM.OpCode.CALLT:
                    builder.AddInstruction(new Instruction
                    {
                        _opCode = VM.OpCode.CALLT,
                        _operand = BitConverter.GetBytes((ushort)0)
                    });
                    break;
            }
            builder.AddInstruction(VM.OpCode.RET);
            var script = builder.ToArray();
            var baseline = BuildRetScript();
            return CreateScenario(opcode, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateCallTSimpleScenario(ScenarioProfile profile)
        {
            var script = BuildScript(builder =>
            {
                builder.AddInstruction(VM.OpCode.CALLT);
                builder.AddInstruction(VM.OpCode.RET);
            });
            var baseline = BuildRetScript();
            return CreateScenario(VM.OpCode.CALLT, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateAbortScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var script = BuildScript(builder =>
            {
                builder.AddInstruction(opcode);
                builder.AddInstruction(VM.OpCode.RET);
            });
            var baseline = BuildRetScript();
            return CreateScenario(opcode, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateAssertScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var script = BuildScript(builder =>
            {
                builder.AddInstruction(VM.OpCode.PUSHT);
                builder.AddInstruction(opcode);
                builder.AddInstruction(VM.OpCode.RET);
            });
            var baseline = BuildRetScript();
            return CreateScenario(opcode, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateThrowScenario(ScenarioProfile profile)
        {
            var script = BuildScript(builder =>
            {
                builder.AddInstruction(VM.OpCode.THROW);
                builder.AddInstruction(VM.OpCode.RET);
            });
            var baseline = BuildRetScript();
            return CreateScenario(VM.OpCode.THROW, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateTryScenario(VM.OpCode opcode, ScenarioProfile profile)
        {
            var script = BuildScript(builder =>
            {
                builder.AddInstruction(new Instruction
                {
                    _opCode = opcode,
                    _operand = opcode == VM.OpCode.TRY ? new byte[] { 0, 0 } : new byte[8]
                });
                builder.AddInstruction(VM.OpCode.RET);
            });
            var baseline = BuildRetScript();
            return CreateScenario(opcode, profile, baseline, script, script);
        }

        private static OpcodeVmScenario CreateSyscallScenario(ScenarioProfile profile)
        {
            var script = BuildScript(builder =>
            {
                builder.AddInstruction(new Instruction
                {
                    _opCode = VM.OpCode.SYSCALL,
                    _operand = BitConverter.GetBytes(0x77777777)
                });
                builder.AddInstruction(VM.OpCode.RET);
            });
            var baseline = BuildRetScript();
            return CreateScenario(VM.OpCode.SYSCALL, profile, baseline, script, script);
        }

        #endregion

        #region Helper methods

        private static OpcodeVmScenario CreateScenario(
            VM.OpCode opcode,
            ScenarioProfile profile,
            byte[] baselineScript,
            byte[] singleScript,
            byte[] saturatedScript,
            Action<BenchmarkEngine, VM.Instruction>? before = null,
            Action<BenchmarkEngine, VM.Instruction>? after = null)
        {
            long price = Benchmark_Opcode.OpCodePrices.TryGetValue(opcode, out var p) ? p : 1;
            long budget = Math.Max(price * profile.Iterations * 8L, Benchmark_Opcode.OneGasDatoshi);
            return new OpcodeVmScenario(baselineScript, singleScript, saturatedScript, budget, before, after);
        }

        private static bool IsScalarPush(VM.OpCode opcode) =>
            opcode is VM.OpCode.PUSHT or VM.OpCode.PUSHF or VM.OpCode.PUSHM1 ||
            (opcode >= VM.OpCode.PUSH0 && opcode <= VM.OpCode.PUSH16);

        private static byte[] BuildOperand(int prefixSize, byte[] payload)
        {
            var operand = new byte[prefixSize + payload.Length];
            switch (prefixSize)
            {
                case 1:
                    operand[0] = (byte)payload.Length;
                    break;
                case 2:
                    BitConverter.GetBytes((ushort)payload.Length).CopyTo(operand, 0);
                    break;
                case 4:
                    BitConverter.GetBytes((uint)payload.Length).CopyTo(operand, 0);
                    break;
            }
            System.Buffer.BlockCopy(payload, 0, operand, prefixSize, payload.Length);
            return operand;
        }

        private static byte[] CreateBaselineScript(ScenarioProfile profile, int pushCount)
        {
            return LoopScriptFactory.BuildCountingLoop(profile, builder =>
            {
                for (int i = 0; i < pushCount; i++)
                    builder.Push(0);
                for (int i = 0; i < pushCount; i++)
                    builder.AddInstruction(VM.OpCode.DROP);
            });
        }

        private static void SeedStack(BenchmarkEngine engine, params object[] items)
        {
            var stack = engine.CurrentContext!.EvaluationStack;
            stack.Clear();
            foreach (var item in items)
                engine.Push(StackItem.FromInterface(item));
        }

        private static object[] CreateSequentialIntegers(int count) =>
            Enumerable.Range(0, count).Select(i => (object)i).ToArray();

        private static void PrepareLocalSlot(BenchmarkEngine engine, int index, object value)
        {
            var slot = EnsureLocalSlot(engine, index);
            slot[index] = StackItem.FromInterface(value);
        }

        private static Slot EnsureLocalSlot(BenchmarkEngine engine, int index)
        {
            var context = engine.CurrentContext!;
            var required = index + 1;
            var slot = EnsureSlot(context.LocalVariables, required, engine.ReferenceCounter);
            context.LocalVariables = slot;
            return slot;
        }

        private static Slot EnsureArgumentSlot(BenchmarkEngine engine, int index)
        {
            var context = engine.CurrentContext!;
            var required = index + 1;
            var slot = EnsureSlot(context.Arguments, required, engine.ReferenceCounter);
            context.Arguments = slot;
            return slot;
        }

        private static void PrepareArgumentSlot(BenchmarkEngine engine, int index, object value)
        {
            var slot = EnsureArgumentSlot(engine, index);
            slot[index] = StackItem.FromInterface(value);
        }

        private static Slot EnsureStaticSlot(BenchmarkEngine engine, int index)
        {
            var context = engine.CurrentContext!;
            var required = index + 1;
            var slot = EnsureSlot(context.StaticFields, required, engine.ReferenceCounter);
            context.StaticFields = slot;
            return slot;
        }

        private static void PrepareStaticSlot(BenchmarkEngine engine, int index, object value)
        {
            var slot = EnsureStaticSlot(engine, index);
            slot[index] = StackItem.FromInterface(value);
        }

        private static Slot EnsureSlot(Slot? existing, int requiredSize, IReferenceCounter referenceCounter)
        {
            if (existing is null)
                return new Slot(requiredSize, referenceCounter);
            if (existing.Count >= requiredSize)
                return existing;
            existing.ClearReferences();
            return new Slot(requiredSize, referenceCounter);
        }

        private static byte[] BuildRetScript() => BuildScript(builder => builder.AddInstruction(VM.OpCode.RET));

        private static byte[] BuildScript(Action<InstructionBuilder> emitter)
        {
            var builder = new InstructionBuilder();
            emitter(builder);
            return builder.ToArray();
        }

        private static void EmitInstruction(InstructionBuilder builder, VM.OpCode opcode, byte? operand)
        {
            if (operand is null)
            {
                builder.AddInstruction(opcode);
            }
            else
            {
                builder.AddInstruction(new Instruction
                {
                    _opCode = opcode,
                    _operand = new[] { operand.Value }
                });
            }
        }

        private static object[] GetConditionalOperands(VM.OpCode opcode) => opcode switch
        {
            VM.OpCode.JMPIF or VM.OpCode.JMPIF_L => new object[] { true },
            VM.OpCode.JMPIFNOT or VM.OpCode.JMPIFNOT_L => new object[] { false },
            VM.OpCode.JMPEQ or VM.OpCode.JMPEQ_L => new object[] { 3, 3 },
            VM.OpCode.JMPNE or VM.OpCode.JMPNE_L => new object[] { 3, 4 },
            VM.OpCode.JMPGT or VM.OpCode.JMPGT_L => new object[] { 5, 3 },
            VM.OpCode.JMPGE or VM.OpCode.JMPGE_L => new object[] { 3, 3 },
            VM.OpCode.JMPLT or VM.OpCode.JMPLT_L => new object[] { 2, 4 },
            VM.OpCode.JMPLE or VM.OpCode.JMPLE_L => new object[] { 3, 3 },
            _ => System.Array.Empty<object>()
        };

        private static object[] GetSampleArrayValues(ScenarioProfile profile)
        {
            var count = Math.Clamp(profile.CollectionLength, 2, 4);
            return Enumerable.Range(1, count).Select(i => (object)i).ToArray();
        }

        private static IReadOnlyList<(object key, object value)> GetSampleMapEntries()
        {
            return new List<(object key, object value)>
            {
                ("a", 1),
                ("b", 2)
            };
        }

        private static void EmitPackedArray(InstructionBuilder builder, IReadOnlyList<object> values)
        {
            foreach (var value in values) builder.Push(value);
            builder.Push(values.Count);
            builder.AddInstruction(VM.OpCode.PACK);
        }

        private static void EmitPackedMap(InstructionBuilder builder, IReadOnlyList<(object key, object value)> entries)
        {
            foreach (var (key, value) in entries)
            {
                builder.Push(value);
                builder.Push(key);
            }
            builder.Push(entries.Count);
            builder.AddInstruction(VM.OpCode.PACKMAP);
        }

        #endregion
    }
}
