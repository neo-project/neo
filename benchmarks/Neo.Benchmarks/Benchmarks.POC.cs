// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks.POC.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Benchmark
{
    public class Benchmarks_PoCs
    {
        private static readonly ProtocolSettings protocol = ProtocolSettings.Load("config.json");
        private static readonly NeoSystem system = new(protocol, (string)null);
        private readonly Random rnd = new();
        private StoreCache snapshot;
        private ApplicationEngine engine;
        private Transaction tx;
        private byte[] script;

        [ParamsSource(nameof(PocFiles))]
        public string PocFile { get; set; } = string.Empty;

        public static IEnumerable<string> PocFiles()
        {
            var pocsFolder = Path.Combine(AppContext.BaseDirectory, "pocs");
            if (!Directory.Exists(pocsFolder))
                yield break;

            foreach (var f in Directory.GetFiles(pocsFolder, "*.b64").OrderBy(x => x))
            {
                yield return Path.GetFileName(f);
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "pocs", PocFile);
            var base64 = File.ReadAllText(path).Trim();
            script = Convert.FromBase64String(base64);

            snapshot = system.GetSnapshotCache();
            tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)rnd.Next(),
                SystemFee = 20_00000000,
                NetworkFee = 1_00000000,
                ValidUntilBlock = ProtocolSettings.Default.MaxTraceableBlocks,
                Signers = Array.Empty<Signer>(),
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = script,
                Witnesses = Array.Empty<Witness>()
            };
            engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, system.GenesisBlock, protocol, tx.SystemFee);
            engine.LoadScript(tx.Script);
        }

        [Benchmark]
        public void ExecuteOnly()
        {
            engine!.Execute();
            if (engine.State != VMState.FAULT) throw new InvalidOperationException($"Bad state for {PocFile}");
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            engine?.Dispose(); engine = null;
            snapshot?.Dispose(); snapshot = null;
            tx = null; script = null;
        }
    }
}
