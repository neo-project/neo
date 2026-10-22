// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks.OpCodes.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using System.Collections.Immutable;
using System.Numerics;

namespace Neo.SmartContract.Benchmark
{
    [InvocationCount(1)]
    [Config(typeof(Config))]
    public class Benchmarks_OpCodes
    {
        private class Config : ManualConfig
        {
            public Config() => WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }

        private const long GasLimit = 50_0000_0000;
        private const long OldModPowPrice = 12300;

        private static readonly byte[] nopScript = CreateNopScript(100);
        private static readonly byte[] modPowBestScript = CreateModPowScript(2, 2, 3);
        private static readonly byte[] modPowWorstScript = CreateModPowWorstScript();

        private NeoSystem system;
        private StoreCache snapshot;
        private ApplicationEngine engine;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var hardforks = Enum.GetValues<Hardfork>().ToImmutableDictionary(hf => hf, _ => 0u);
            var settings = ProtocolSettings.Load("config.json") with { Hardforks = hardforks };
            system = new NeoSystem(settings, (string)null);
            snapshot = system.GetSnapshotCache();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            snapshot.Dispose();
            system.Dispose();
        }

        [IterationSetup(Target = nameof(NOP))]
        public void SetupNOP() => LoadScript(nopScript);

        [IterationSetup(Target = nameof(MODPOW_Best))]
        public void SetupMODPOWBest() => LoadScript(modPowBestScript);

        [IterationSetup(Target = nameof(MODPOW_Worst))]
        public void SetupMODPOWWorst() => LoadScript(modPowWorstScript);

        [IterationSetup(Target = nameof(MODPOW_Worst_OldPrice))]
        public void SetupMODPOWWorstOldPrice()
        {
            LoadScript(modPowWorstScript);
            engine.DynamicPriceTable[OpCode.MODPOW] = _ => OldModPowPrice;
        }

        [IterationCleanup]
        public void IterationCleanup() => engine.Dispose();

        [Benchmark(Baseline = true)]
        public void NOP() => Run();

        [Benchmark]
        public void MODPOW_Best() => Run();

        [Benchmark]
        public void MODPOW_Worst() => Run();

        [Benchmark]
        public void MODPOW_Worst_OldPrice() => Run();

        private void LoadScript(byte[] script)
        {
            Transaction tx = new()
            {
                Script = script,
                Signers = [],
                Attributes = [],
                Witnesses = []
            };
            engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: system.Settings, gas: GasLimit);
            engine.LoadScript(script);
        }

        private void Run()
        {
            engine.Execute();
            if (engine.State != VMState.FAULT || engine.FaultException?.Message != "Insufficient GAS.")
                throw new InvalidOperationException($"Unexpected execution result: {engine.State}, {engine.FaultException}");
        }

        private static byte[] CreateNopScript(int count)
        {
            using ScriptBuilder sb = new();
            for (int i = 0; i < count; i++)
                sb.Emit(OpCode.NOP);
            return EmitLoop(sb);
        }

        private static byte[] CreateModPowScript(BigInteger value, BigInteger exponent, BigInteger modulus)
        {
            using ScriptBuilder sb = new();
            sb.EmitPush(value);
            sb.EmitPush(exponent);
            sb.EmitPush(modulus);
            sb.Emit(OpCode.MODPOW);
            sb.Emit(OpCode.DROP);
            return EmitLoop(sb);
        }

        private static byte[] CreateModPowWorstScript()
        {
            var maxInt = (BigInteger.One << 255) - 1;
            var value = BigInteger.Pow(3, 160);
            return CreateModPowScript(value, maxInt, maxInt);
        }

        private static byte[] EmitLoop(ScriptBuilder sb)
        {
            sb.Emit(OpCode.JMP, [(byte)-sb.Length]);
            return sb.ToArray();
        }
    }
}
