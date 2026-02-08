// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks.NativeContractDispatch.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System.Reflection;

namespace Neo.Benchmark
{
    /// <summary>
    /// Benchmarks comparing reflection-based method invocation vs cached delegate invocation
    /// for native contract methods.
    /// </summary>
    public class Benchmarks_NativeContractDispatch
    {
        private static readonly ProtocolSettings _protocol = ProtocolSettings.Default;
        private static readonly NeoSystem _system = new(_protocol, (string)null);
        private DataCache _snapshot = null!;
        private Block _persistingBlock = null!;
        private MethodInfo _methodInfo = null!;
        private System.Func<object, object[], object> _cachedDelegate = null!;
        private NativeContract _contract = null!;
        private object[] _parameters = null!;

        [GlobalSetup]
        public void Setup()
        {
            _snapshot = _system.GetSnapshotCache();
            _persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = 1,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = Witness.Empty,
                },
                Transactions = System.Array.Empty<Transaction>()
            };

            // Get a native contract method for benchmarking
            _contract = NativeContract.NEO;
            _methodInfo = typeof(NeoToken).GetMethod("Decimals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

            // Pre-compile the delegate
            _cachedDelegate = _methodInfo.CreateDelegate<System.Func<object, object[], object>>(_contract);

            // Setup parameters (Decimals method takes DataCache)
            _parameters = new object[] { _snapshot }; 
        }

        /// <summary>
        /// Benchmark the traditional reflection-based method invocation.
        /// </summary>
        [Benchmark(Baseline = true)]
        public object Reflection_Invoke()
        {
            return _methodInfo.Invoke(_contract, _parameters);
        }

        /// <summary>
        /// Benchmark the cached delegate invocation approach.
        /// </summary>
        [Benchmark]
        public object CachedDelegate_Invoke()
        {
            return _cachedDelegate(_contract, _parameters);
        }

        /// <summary>
        /// Benchmark creating a delegate (one-time cost per method).
        /// </summary>
        [Benchmark]
        public System.Func<object, object[], object> Create_Delegate()
        {
            return _methodInfo.CreateDelegate<System.Func<object, object[], object>>(_contract);
        }

        /// <summary>
        /// Benchmark full native contract invocation through ApplicationEngine
        /// with the optimized dispatch cache.
        /// </summary>
        [Benchmark]
        public void FullNativeContractCall_Decimals()
        {
            using var engine = ApplicationEngine.Create(
                TriggerType.Application,
                null,
                _snapshot.CloneCache(),
                _persistingBlock,
                settings: _protocol);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "decimals");
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        /// <summary>
        /// Benchmark full native contract invocation through ApplicationEngine
        /// with the optimized dispatch cache - BalanceOf method.
        /// </summary>
        [Benchmark]
        public void FullNativeContractCall_BalanceOf()
        {
            using var engine = ApplicationEngine.Create(
                TriggerType.Application,
                null,
                _snapshot.CloneCache(),
                _persistingBlock,
                settings: _protocol);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "balanceOf", UInt160.Zero);
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        /// <summary>
        /// Benchmark full native contract invocation through ApplicationEngine
        /// with the optimized dispatch cache - Symbol method.
        /// </summary>
        [Benchmark]
        public void FullNativeContractCall_Symbol()
        {
            using var engine = ApplicationEngine.Create(
                TriggerType.Application,
                null,
                _snapshot.CloneCache(),
                _persistingBlock,
                settings: _protocol);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.GAS.Hash, "symbol");
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        /// <summary>
        /// Benchmark StdLib method invocation - common utility contract.
        /// </summary>
        [Benchmark]
        public void FullNativeContractCall_StdLib_Base64Encode()
        {
            using var engine = ApplicationEngine.Create(
                TriggerType.Application,
                null,
                _snapshot.CloneCache(),
                _persistingBlock,
                settings: _protocol);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "base64Encode", new byte[] { 0x01, 0x02, 0x03 });
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        /// <summary>
        /// Benchmark Policy contract method invocation.
        /// </summary>
        [Benchmark]
        public void FullNativeContractCall_Policy_GetFeePerByte()
        {
            using var engine = ApplicationEngine.Create(
                TriggerType.Application,
                null,
                _snapshot.CloneCache(),
                _persistingBlock,
                settings: _protocol);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Policy.Hash, "getFeePerByte");
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }
    }
}
