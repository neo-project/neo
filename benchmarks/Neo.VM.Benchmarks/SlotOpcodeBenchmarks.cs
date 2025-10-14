// Copyright (C) 2015-2025 The Neo Project.
//
// SlotOpcodeBenchmarks.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Benchmark
{
    /// <summary>
    /// Benchmarks slot-related opcodes (INITSSLOT, INITSLOT, LDSFLD/STSFLD, LDLOC/STLOC, LDARG/STARG)
    /// using concrete scripts emitted through <see cref="InstructionBuilder"/>.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 8)]
    public class SlotOpcodeBenchmarks
    {
        private const int Iterations = 32;

        public sealed record OpcodeCase(string Name, byte[] Script, Func<ExecutionEngine>? EngineFactory = null)
        {
            public override string ToString() => Name;
        }

        private OpcodeCase[] _initStaticCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _initLocalCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _staticLoadCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _staticStoreCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _localLoadCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _localStoreCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _argLoadCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _argStoreCases = System.Array.Empty<OpcodeCase>();

        [GlobalSetup]
        public void Setup()
        {
            _initStaticCases = BuildInitStaticCases();
            _initLocalCases = BuildInitLocalCases();
            _staticLoadCases = BuildStaticLoadCases().ToArray();
            _staticStoreCases = BuildStaticStoreCases().ToArray();
            _localLoadCases = BuildLocalLoadCases().ToArray();
            _localStoreCases = BuildLocalStoreCases().ToArray();
            _argLoadCases = BuildArgLoadCases().ToArray();
            _argStoreCases = BuildArgStoreCases().ToArray();
        }

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.INITSSLOT))]
        [ArgumentsSource(nameof(InitStaticCases))]
        public void INITSSLOT(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.INITSLOT))]
        [ArgumentsSource(nameof(InitLocalCases))]
        public void INITSLOT(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("LDSFLD")]
        [ArgumentsSource(nameof(StaticLoadCases))]
        public void StaticLoads(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("STSFLD")]
        [ArgumentsSource(nameof(StaticStoreCases))]
        public void StaticStores(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("LDLOC")]
        [ArgumentsSource(nameof(LocalLoadCases))]
        public void LocalLoads(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("STLOC")]
        [ArgumentsSource(nameof(LocalStoreCases))]
        public void LocalStores(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("LDARG")]
        [ArgumentsSource(nameof(ArgLoadCases))]
        public void ArgLoads(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("STARG")]
        [ArgumentsSource(nameof(ArgStoreCases))]
        public void ArgStores(OpcodeCase @case) => ExecuteCase(@case);

        public IEnumerable<OpcodeCase> InitStaticCases() => _initStaticCases;
        public IEnumerable<OpcodeCase> InitLocalCases() => _initLocalCases;
        public IEnumerable<OpcodeCase> StaticLoadCases() => _staticLoadCases;
        public IEnumerable<OpcodeCase> StaticStoreCases() => _staticStoreCases;
        public IEnumerable<OpcodeCase> LocalLoadCases() => _localLoadCases;
        public IEnumerable<OpcodeCase> LocalStoreCases() => _localStoreCases;
        public IEnumerable<OpcodeCase> ArgLoadCases() => _argLoadCases;
        public IEnumerable<OpcodeCase> ArgStoreCases() => _argStoreCases;

        #region Case builders

        private static OpcodeCase[] BuildInitStaticCases()
        {
            return new[]
            {
                new OpcodeCase("INITSSLOT_1", BuildInitStaticScript(slotSize: 1, callCount: 64)),
                new OpcodeCase("INITSSLOT_16", BuildInitStaticScript(slotSize: 16, callCount: 64))
            };
        }

        private static OpcodeCase[] BuildInitLocalCases()
        {
            return new[]
            {
                new OpcodeCase("INITSLOT_LOCALS4", BuildInitSlotScript(localCount: 4, argCount: 0, callCount: 64)),
                new OpcodeCase("INITSLOT_LOCALS4_ARGS4", BuildInitSlotScript(localCount: 4, argCount: 4, callCount: 64))
            };
        }

        private static IEnumerable<OpcodeCase> BuildStaticLoadCases()
        {
            foreach (var (opcode, index) in StaticLoadVariants())
            {
                yield return new OpcodeCase(
                    $"{opcode}{(index.HasValue ? "_" + index.Value : string.Empty)}",
                    BuildStaticLoadScript(opcode, index, Iterations));
            }
        }

        private static IEnumerable<OpcodeCase> BuildStaticStoreCases()
        {
            foreach (var (opcode, index) in StaticStoreVariants())
            {
                yield return new OpcodeCase(
                    $"{opcode}{(index.HasValue ? "_" + index.Value : string.Empty)}",
                    BuildStaticStoreScript(opcode, index, Iterations));
            }
        }

        private static IEnumerable<OpcodeCase> BuildLocalLoadCases()
        {
            foreach (var (opcode, index) in LocalLoadVariants())
            {
                yield return new OpcodeCase(
                    $"{opcode}{(index.HasValue ? "_" + index.Value : string.Empty)}",
                    BuildLocalLoadScript(opcode, index, Iterations));
            }
        }

        private static IEnumerable<OpcodeCase> BuildLocalStoreCases()
        {
            foreach (var (opcode, index) in LocalStoreVariants())
            {
                yield return new OpcodeCase(
                    $"{opcode}{(index.HasValue ? "_" + index.Value : string.Empty)}",
                    BuildLocalStoreScript(opcode, index, Iterations));
            }
        }

        private static IEnumerable<OpcodeCase> BuildArgLoadCases()
        {
            foreach (var (opcode, index) in ArgLoadVariants())
            {
                yield return new OpcodeCase(
                    $"{opcode}{(index.HasValue ? "_" + index.Value : string.Empty)}",
                    BuildArgLoadScript(opcode, index, Iterations));
            }
        }

        private static IEnumerable<OpcodeCase> BuildArgStoreCases()
        {
            foreach (var (opcode, index) in ArgStoreVariants())
            {
                yield return new OpcodeCase(
                    $"{opcode}{(index.HasValue ? "_" + index.Value : string.Empty)}",
                    BuildArgStoreScript(opcode, index, Iterations));
            }
        }

        #endregion

        #region Script builders

        private static byte[] BuildInitStaticScript(byte slotSize, int callCount)
        {
            var builder = new InstructionBuilder();
            var target = new JumpTarget();

            for (int i = 0; i < callCount; i++)
            {
                builder.Jump(OpCode.CALL, target);
            }
            builder.AddInstruction(OpCode.RET);

            var initInstruction = new Instruction
            {
                _opCode = OpCode.INITSSLOT,
                _operand = new[] { slotSize }
            };
            target._instruction = builder.AddInstruction(initInstruction);
            builder.AddInstruction(OpCode.RET);

            return builder.ToArray();
        }

        private static byte[] BuildInitSlotScript(byte localCount, byte argCount, int callCount)
        {
            var builder = new InstructionBuilder();
            var target = new JumpTarget();

            for (int i = 0; i < callCount; i++)
            {
                if (argCount > 0)
                {
                    for (int arg = 0; arg < argCount; arg++)
                    {
                        builder.Push(arg);
                    }
                }
                builder.Jump(OpCode.CALL, target);
            }
            builder.AddInstruction(OpCode.RET);

            var initInstruction = new Instruction
            {
                _opCode = OpCode.INITSLOT,
                _operand = new[] { localCount, argCount }
            };
            target._instruction = builder.AddInstruction(initInstruction);
            builder.AddInstruction(OpCode.RET);

            return builder.ToArray();
        }

        private static byte[] BuildStaticLoadScript(OpCode loadOpcode, byte? operand, int iterations)
        {
            byte slotIndex = DetermineIndex(loadOpcode, operand);
            byte slotSize = (byte)(slotIndex + 1);
            var builder = new InstructionBuilder();

            builder.AddInstruction(new Instruction
            {
                _opCode = OpCode.INITSSLOT,
                _operand = new[] { slotSize }
            });

            for (int index = 0; index < slotSize; index++)
            {
                builder.Push(index);
                AppendStore(builder, OpCode.STSFLD, (byte)index);
            }

            for (int i = 0; i < iterations; i++)
            {
                AppendLoad(builder, loadOpcode, operand);
                builder.AddInstruction(OpCode.DROP);
            }

            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildStaticStoreScript(OpCode storeOpcode, byte? operand, int iterations)
        {
            byte index = DetermineIndex(storeOpcode, operand);
            byte slotSize = (byte)(index + 1);
            var builder = new InstructionBuilder();

            builder.AddInstruction(new Instruction
            {
                _opCode = OpCode.INITSSLOT,
                _operand = new[] { slotSize }
            });

            for (int i = 0; i < iterations; i++)
            {
                builder.Push(i);
                AppendStore(builder, storeOpcode, index);
            }

            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildLocalLoadScript(OpCode loadOpcode, byte? operand, int iterations)
        {
            byte index = DetermineIndex(loadOpcode, operand);
            byte localCount = (byte)(index + 1);
            var builder = new InstructionBuilder();

            builder.AddInstruction(new Instruction
            {
                _opCode = OpCode.INITSLOT,
                _operand = new[] { localCount, (byte)0 }
            });

            builder.Push(123);
            AppendStore(builder, OpCode.STLOC, index);

            for (int i = 0; i < iterations; i++)
            {
                AppendLoad(builder, loadOpcode, operand);
                builder.AddInstruction(OpCode.DROP);
            }

            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildLocalStoreScript(OpCode storeOpcode, byte? operand, int iterations)
        {
            byte index = DetermineIndex(storeOpcode, operand);
            byte localCount = (byte)(index + 1);
            var builder = new InstructionBuilder();

            builder.AddInstruction(new Instruction
            {
                _opCode = OpCode.INITSLOT,
                _operand = new[] { localCount, (byte)0 }
            });

            for (int i = 0; i < iterations; i++)
            {
                builder.Push(i);
                AppendStore(builder, storeOpcode, index);
            }

            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildArgLoadScript(OpCode loadOpcode, byte? operand, int iterations)
        {
            byte index = DetermineIndex(loadOpcode, operand);
            byte argCount = (byte)(index + 1);
            var builder = new InstructionBuilder();

            for (int arg = argCount - 1; arg >= 0; arg--)
            {
                builder.Push(arg);
            }

            builder.AddInstruction(new Instruction
            {
                _opCode = OpCode.INITSLOT,
                _operand = new[] { (byte)0, argCount }
            });

            for (int i = 0; i < iterations; i++)
            {
                AppendLoad(builder, loadOpcode, operand);
                builder.AddInstruction(OpCode.DROP);
            }

            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildArgStoreScript(OpCode storeOpcode, byte? operand, int iterations)
        {
            byte index = DetermineIndex(storeOpcode, operand);
            byte argCount = (byte)(index + 1);
            var builder = new InstructionBuilder();

            for (int arg = argCount - 1; arg >= 0; arg--)
            {
                builder.Push(arg);
            }

            builder.AddInstruction(new Instruction
            {
                _opCode = OpCode.INITSLOT,
                _operand = new[] { (byte)0, argCount }
            });

            for (int i = 0; i < iterations; i++)
            {
                builder.Push(i);
                AppendStore(builder, storeOpcode, index);
            }

            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        #endregion

        #region Helpers

        private static void ExecuteCase(OpcodeCase @case)
        {
            using var engine = @case.EngineFactory?.Invoke() ?? new ExecutionEngine();
            engine.LoadScript(@case.Script);
            var state = engine.Execute();
            if (state != VMState.HALT)
                throw new InvalidOperationException($"Benchmark case '{@case.Name}' ended with VM state {state}.");
        }

        private static IEnumerable<(OpCode opcode, byte? operand)> StaticLoadVariants()
        {
            return new (OpCode, byte?)[]
            {
                (OpCode.LDSFLD0, null),
                (OpCode.LDSFLD1, null),
                (OpCode.LDSFLD2, null),
                (OpCode.LDSFLD3, null),
                (OpCode.LDSFLD4, null),
                (OpCode.LDSFLD5, null),
                (OpCode.LDSFLD6, null),
                (OpCode.LDSFLD, 12)
            };
        }

        private static IEnumerable<(OpCode opcode, byte? operand)> StaticStoreVariants()
        {
            return new (OpCode, byte?)[]
            {
                (OpCode.STSFLD0, null),
                (OpCode.STSFLD1, null),
                (OpCode.STSFLD2, null),
                (OpCode.STSFLD3, null),
                (OpCode.STSFLD4, null),
                (OpCode.STSFLD5, null),
                (OpCode.STSFLD6, null),
                (OpCode.STSFLD, 12)
            };
        }

        private static IEnumerable<(OpCode opcode, byte? operand)> LocalLoadVariants()
        {
            return new (OpCode, byte?)[]
            {
                (OpCode.LDLOC0, null),
                (OpCode.LDLOC1, null),
                (OpCode.LDLOC2, null),
                (OpCode.LDLOC3, null),
                (OpCode.LDLOC4, null),
                (OpCode.LDLOC5, null),
                (OpCode.LDLOC6, null),
                (OpCode.LDLOC, 10)
            };
        }

        private static IEnumerable<(OpCode opcode, byte? operand)> LocalStoreVariants()
        {
            return new (OpCode, byte?)[]
            {
                (OpCode.STLOC0, null),
                (OpCode.STLOC1, null),
                (OpCode.STLOC2, null),
                (OpCode.STLOC3, null),
                (OpCode.STLOC4, null),
                (OpCode.STLOC5, null),
                (OpCode.STLOC6, null),
                (OpCode.STLOC, 10)
            };
        }

        private static IEnumerable<(OpCode opcode, byte? operand)> ArgLoadVariants()
        {
            return new (OpCode, byte?)[]
            {
                (OpCode.LDARG0, null),
                (OpCode.LDARG1, null),
                (OpCode.LDARG2, null),
                (OpCode.LDARG3, null),
                (OpCode.LDARG4, null),
                (OpCode.LDARG5, null),
                (OpCode.LDARG6, null),
                (OpCode.LDARG, 6)
            };
        }

        private static IEnumerable<(OpCode opcode, byte? operand)> ArgStoreVariants()
        {
            return new (OpCode, byte?)[]
            {
                (OpCode.STARG0, null),
                (OpCode.STARG1, null),
                (OpCode.STARG2, null),
                (OpCode.STARG3, null),
                (OpCode.STARG4, null),
                (OpCode.STARG5, null),
                (OpCode.STARG6, null),
                (OpCode.STARG, 6)
            };
        }

        private static void AppendLoad(InstructionBuilder builder, OpCode opcode, byte? operandOverride)
        {
            if (RequiresOperand(opcode))
            {
                var index = DetermineIndex(opcode, operandOverride);
                builder.AddInstruction(new Instruction
                {
                    _opCode = opcode,
                    _operand = new[] { index }
                });
            }
            else
            {
                builder.AddInstruction(opcode);
            }
        }

        private static void AppendStore(InstructionBuilder builder, OpCode opcode, byte index)
        {
            if (RequiresOperand(opcode))
            {
                builder.AddInstruction(new Instruction
                {
                    _opCode = opcode,
                    _operand = new[] { index }
                });
            }
            else
            {
                builder.AddInstruction(opcode);
            }
        }

        private static bool RequiresOperand(OpCode opcode)
        {
            return opcode switch
            {
                OpCode.LDSFLD or OpCode.STSFLD or OpCode.LDLOC or OpCode.STLOC or OpCode.LDARG or OpCode.STARG => true,
                _ => false
            };
        }

        private static byte? GetFixedIndex(OpCode opcode)
        {
            return opcode switch
            {
                OpCode.LDSFLD0 or OpCode.STSFLD0 or OpCode.LDLOC0 or OpCode.STLOC0 or OpCode.LDARG0 or OpCode.STARG0 => 0,
                OpCode.LDSFLD1 or OpCode.STSFLD1 or OpCode.LDLOC1 or OpCode.STLOC1 or OpCode.LDARG1 or OpCode.STARG1 => 1,
                OpCode.LDSFLD2 or OpCode.STSFLD2 or OpCode.LDLOC2 or OpCode.STLOC2 or OpCode.LDARG2 or OpCode.STARG2 => 2,
                OpCode.LDSFLD3 or OpCode.STSFLD3 or OpCode.LDLOC3 or OpCode.STLOC3 or OpCode.LDARG3 or OpCode.STARG3 => 3,
                OpCode.LDSFLD4 or OpCode.STSFLD4 or OpCode.LDLOC4 or OpCode.STLOC4 or OpCode.LDARG4 or OpCode.STARG4 => 4,
                OpCode.LDSFLD5 or OpCode.STSFLD5 or OpCode.LDLOC5 or OpCode.STLOC5 or OpCode.LDARG5 or OpCode.STARG5 => 5,
                OpCode.LDSFLD6 or OpCode.STSFLD6 or OpCode.LDLOC6 or OpCode.STLOC6 or OpCode.LDARG6 or OpCode.STARG6 => 6,
                _ => null
            };
        }

        private static byte DetermineIndex(OpCode opcode, byte? operand)
        {
            if (operand.HasValue)
                return operand.Value;
            var fixedIndex = GetFixedIndex(opcode);
            if (!fixedIndex.HasValue)
                throw new InvalidOperationException($"Unable to resolve index for opcode {opcode}.");
            return fixedIndex.Value;
        }

        #endregion
    }
}
