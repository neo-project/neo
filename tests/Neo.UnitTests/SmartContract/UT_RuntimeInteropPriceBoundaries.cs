// Copyright (C) 2015-2026 The Neo Project.
//
// UT_RuntimeInteropPriceBoundaries.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;

#nullable enable

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_RuntimeInteropPriceBoundaries
    {
        /// <summary>Default exec fee factor, in datoshi. 1 datoshi = 1e-8 GAS.</summary>
        private const int ExecFeeFactor = 30;

        private const uint HuyaoHeight = 10;

        private DataCache _snapshot = null!;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void EveryInteropPriceListHasUniqueForksAndAFallback()
        {
            Assert.IsGreaterThan(0, ApplicationEngine.Services.Count);
            foreach (var descriptor in ApplicationEngine.Services.Values)
            {
                Assert.IsGreaterThan(0, descriptor.Prices.Count, descriptor.Name);
                Assert.IsNull(descriptor.Prices[^1].Key, descriptor.Name);
                Assert.IsGreaterThanOrEqualTo(0, descriptor.Prices[^1].Value, descriptor.Name);

                var seen = new HashSet<Hardfork>();
                Hardfork? newer = null;
                for (int i = 0; i < descriptor.Prices.Count - 1; i++)
                {
                    var fork = descriptor.Prices[i].Key;
                    Assert.IsNotNull(fork, descriptor.Name);
                    Assert.IsTrue(seen.Add(fork!.Value), $"{descriptor.Name} prices {fork} twice.");
                    if (newer is not null)
                        Assert.IsTrue(newer.Value > fork.Value, $"{descriptor.Name} prices are not ordered from the latest hardfork.");
                    newer = fork;
                    Assert.IsGreaterThan(0, descriptor.Prices[i].Value, descriptor.Name);
                }
            }

            AssertPrice(ApplicationEngine.System_Runtime_GasLeft, 1 << 4, 8900);
            AssertPrice(ApplicationEngine.System_Runtime_GetRandom, 0, 15833);
            AssertPrice(ApplicationEngine.System_Runtime_GetScriptContainer, 1 << 3, 36533);
            AssertPrice(ApplicationEngine.System_Runtime_BurnGas, 1 << 4, 3067);
            // Not repriced: still a single pre-Huyao entry, so native-adjacent syscalls keep their old fee.
            Assert.HasCount(1, ApplicationEngine.System_Runtime_CheckWitness.Prices);
            Assert.HasCount(1, ApplicationEngine.System_Runtime_CurrentSigners.Prices);
            Assert.HasCount(1, ApplicationEngine.System_Contract_Call.Prices);
            Assert.HasCount(1, ApplicationEngine.System_Contract_CallNative.Prices);
        }

        [TestMethod]
        public void PriceSwitchesOnTheActivationBlockInclusive()
        {
            long before = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight - 1)).Fee;
            long onFork = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight)).Fee;
            long after = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight + 1)).Fee;

            Assert.AreEqual(LegacyDatoshi(1 << 4), before);
            Assert.AreEqual(HuyaoDatoshi(8900), onFork);
            Assert.AreEqual(onFork, after);
        }

        [TestMethod]
        public void VerificationUsesLedgerHeightPlusOne()
        {
            uint ledger = NativeContract.Ledger.CurrentIndex(_snapshot);
            uint persisting = ledger + 1;

            long before = Run(
                Syscall(ApplicationEngine.System_Runtime_GasLeft),
                Settings(persisting + 1),
                1_000_000,
                block: null,
                trigger: TriggerType.Verification).Fee;
            long activated = Run(
                Syscall(ApplicationEngine.System_Runtime_GasLeft),
                Settings(persisting),
                1_000_000,
                block: null,
                trigger: TriggerType.Verification).Fee;

            Assert.AreEqual(LegacyDatoshi(1 << 4), before);
            Assert.AreEqual(HuyaoDatoshi(8900), activated);
        }

        [TestMethod]
        public void GasLimitEqualToFeeHaltsAndOneDatoshiLessFaults()
        {
            AssertGasBoundary(ApplicationEngine.System_Runtime_GasLeft, LegacyDatoshi(1 << 4), huyao: false);
            AssertGasBoundary(ApplicationEngine.System_Runtime_GasLeft, HuyaoDatoshi(8900), huyao: true);

            // GetScriptContainer is one of the few runtime interops whose price goes up.
            // A limit that paid the old fee faults once Huyao is active, and nowhere earlier.
            Assert.AreEqual(VMState.HALT, Run(ScriptContainerScript(), Settings(HuyaoHeight), LegacyDatoshi(1 << 3), BlockAt(HuyaoHeight - 1), Container()).State);
            var tight = Run(ScriptContainerScript(), Settings(HuyaoHeight), LegacyDatoshi(1 << 3), BlockAt(HuyaoHeight), Container());
            Assert.AreEqual(VMState.FAULT, tight.State);
            Assert.Contains("Insufficient GAS.", tight.Fault);

            Assert.AreEqual(VMState.HALT, Run(ScriptContainerScript(), Settings(HuyaoHeight), HuyaoDatoshi(36533), BlockAt(HuyaoHeight), Container()).State);
        }

        [TestMethod]
        public void LowerPriceDoesNotFaultAScriptThatPaidTheOldFee()
        {
            long oldFee = LegacyDatoshi(1 << 4);
            var before = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), oldFee, BlockAt(HuyaoHeight - 1));
            var after = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), oldFee, BlockAt(HuyaoHeight));

            Assert.AreEqual(VMState.HALT, before.State, before.Fault);
            Assert.AreEqual(oldFee, before.Fee);
            Assert.AreEqual(VMState.HALT, after.State, after.Fault);
            Assert.AreEqual(HuyaoDatoshi(8900), after.Fee);
            Assert.IsGreaterThan(after.Fee, oldFee);
        }

        [TestMethod]
        public void GasLeftObservesItsOwnFeeAndAccountsExactly()
        {
            const long gas = 1_000_000;
            var before = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), gas, BlockAt(HuyaoHeight - 1));
            var after = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), gas, BlockAt(HuyaoHeight));

            Assert.AreEqual(VMState.HALT, before.State, before.Fault);
            Assert.AreEqual(gas - LegacyDatoshi(1 << 4), before.Top);
            Assert.AreEqual(gas, before.Top + before.Fee);

            Assert.AreEqual(VMState.HALT, after.State, after.Fault);
            Assert.AreEqual(gas - HuyaoDatoshi(8900), after.Top);
            Assert.AreEqual(gas, after.Top + after.Fee);
        }

        [TestMethod]
        public void ZeroGasFaultsInsteadOfThrowing()
        {
            var before = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), gas: 0, BlockAt(HuyaoHeight - 1));
            var after = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), gas: 0, BlockAt(HuyaoHeight));

            Assert.AreEqual(VMState.FAULT, before.State);
            Assert.AreEqual(VMState.FAULT, after.State);
            Assert.Contains("Insufficient GAS.", before.Fault);
            Assert.Contains("Insufficient GAS.", after.Fault);
            Assert.AreEqual(0, before.GasLeft);
            Assert.AreEqual(0, after.GasLeft);
        }

        [TestMethod]
        public void GetRandomChargesOnlyOneSchedule()
        {
            var before = Run(Syscall(ApplicationEngine.System_Runtime_GetRandom), Settings(HuyaoHeight), 10_000_000, BlockAt(HuyaoHeight - 1));
            var after = Run(Syscall(ApplicationEngine.System_Runtime_GetRandom), Settings(HuyaoHeight), 10_000_000, BlockAt(HuyaoHeight));

            Assert.AreEqual(VMState.HALT, before.State, before.Fault);
            Assert.AreEqual(VMState.HALT, after.State, after.Fault);
            // Pre-Huyao the descriptor price is 0 and the handler adds 1 << 13. Post-Huyao only the coefficient is charged.
            Assert.AreEqual(LegacyDatoshi(1 << 13), before.Fee);
            Assert.AreEqual(HuyaoDatoshi(15833), after.Fee);
            Assert.IsGreaterThan(after.Fee, before.Fee);
        }

        [TestMethod]
        public void HandlerFailureStillChargesTheSyscallOnce()
        {
            var before = Run(Syscall(ApplicationEngine.System_Runtime_GetTime), Settings(HuyaoHeight), 1_000_000, block: null, trigger: TriggerType.Verification);
            var after = Run(Syscall(ApplicationEngine.System_Runtime_GetTime), Settings(0), 1_000_000, block: null, trigger: TriggerType.Verification);

            Assert.AreEqual(VMState.FAULT, before.State);
            Assert.AreEqual(VMState.FAULT, after.State);
            Assert.Contains("GetTime can only be called with Application trigger.", before.Fault);
            Assert.Contains("GetTime can only be called with Application trigger.", after.Fault);
            Assert.AreEqual(LegacyDatoshi(1 << 3), before.Fee);
            Assert.AreEqual(HuyaoDatoshi(9033), after.Fee);

            var missingContainer = Run(ScriptContainerScript(), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight), container: null);
            Assert.AreEqual(VMState.FAULT, missingContainer.State);
            Assert.AreEqual(HuyaoDatoshi(36533), missingContainer.Fee);

            var burned = Run(BurnScript(0), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight));
            var burnedNegative = Run(BurnScript(-1), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight));
            Assert.AreEqual(VMState.FAULT, burned.State);
            Assert.AreEqual(VMState.FAULT, burnedNegative.State);
            Assert.Contains("GAS must be positive.", burned.Fault);
            Assert.Contains("GAS must be positive.", burnedNegative.Fault);
            Assert.AreEqual(burned.Fee, burnedNegative.Fee);
            Assert.DoesNotContain("AddFemtoGas can't be negative.", burnedNegative.Fault);
        }

        [TestMethod]
        public void BurnGasAddsTheRequestedAmountOnTopOfTheBaseFee()
        {
            var rejected = Run(BurnScript(0), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight));
            var accepted = Run(BurnScript(1), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight));

            Assert.AreEqual(VMState.FAULT, rejected.State);
            Assert.AreEqual(VMState.HALT, accepted.State, accepted.Fault);
            Assert.AreEqual(rejected.Fee + 1, accepted.Fee);
        }

        [TestMethod]
        public void WhitelistedContextSkipsTheHuyaoPrice()
        {
            const long gas = 1_000_000;
            var charged = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), gas, BlockAt(HuyaoHeight));
            var skipped = Run(Syscall(ApplicationEngine.System_Runtime_GasLeft), Settings(HuyaoHeight), gas, BlockAt(HuyaoHeight), whitelist: true);

            Assert.AreEqual(VMState.HALT, skipped.State, skipped.Fault);
            Assert.AreEqual(0, skipped.Fee);
            Assert.AreEqual(gas, skipped.Top);
            Assert.IsGreaterThan(0, charged.Fee);
        }

        [TestMethod]
        public void DirectNativeCallsDoNotChargeOrFault()
        {
            var settings = Settings(HuyaoHeight);
            var block = BlockAt(HuyaoHeight);
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, _snapshot.CloneCache(), block, settings, 1_000_000);
            engine.LoadScript(new byte[] { (byte)OpCode.RET });

            long fee = engine.FeeConsumed;
            Assert.AreEqual(block.Timestamp, engine.GetTime());
            Assert.AreEqual(settings.Network, engine.GetNetwork());
            Assert.AreEqual(1, engine.GetInvocationCounter());
            Assert.AreEqual(1, engine.GetInvocationCounter());
            _ = engine.GetRandom();
            Assert.AreEqual(fee, engine.FeeConsumed);

            using var verification = ApplicationEngine.Create(TriggerType.Verification, null, _snapshot.CloneCache(), null, settings, 1_000_000);
            Assert.ThrowsExactly<InvalidOperationException>(() => verification.GetTime());
            Assert.AreEqual(0, verification.FeeConsumed);
        }

        [TestMethod]
        public void UnpricedRuntimeSyscallsKeepTheSameFeeAcrossHuyao()
        {
            long before = Run(Syscall(ApplicationEngine.System_Runtime_CurrentSigners), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight - 1)).Fee;
            long after = Run(Syscall(ApplicationEngine.System_Runtime_CurrentSigners), Settings(HuyaoHeight), 1_000_000, BlockAt(HuyaoHeight)).Fee;

            Assert.AreEqual(LegacyDatoshi(1 << 4), before);
            Assert.AreEqual(before, after);
        }

        [TestMethod]
        public void RepricedRuntimeSyscallsHaltWhenGasIsSufficient()
        {
            var calls = new[]
            {
                ApplicationEngine.System_Runtime_Platform,
                ApplicationEngine.System_Runtime_GetNetwork,
                ApplicationEngine.System_Runtime_GetAddressVersion,
                ApplicationEngine.System_Runtime_GetTrigger,
                ApplicationEngine.System_Runtime_GetExecutingScriptHash,
                ApplicationEngine.System_Runtime_GetCallingScriptHash,
                ApplicationEngine.System_Runtime_GetEntryScriptHash,
                ApplicationEngine.System_Runtime_GetInvocationCounter,
                ApplicationEngine.System_Runtime_GetRandom,
                ApplicationEngine.System_Runtime_GasLeft,
                ApplicationEngine.System_Runtime_CurrentSigners,
            };

            foreach (bool huyao in new[] { false, true })
            {
                var block = BlockAt(huyao ? HuyaoHeight : HuyaoHeight - 1);
                foreach (var descriptor in calls)
                {
                    var result = Run(Syscall(descriptor), Settings(HuyaoHeight), 10_000_000, block);
                    Assert.AreEqual(VMState.HALT, result.State, $"{descriptor.Name} huyao={huyao}: {result.Fault}");
                    Assert.IsGreaterThan(0, result.Fee, descriptor.Name);
                }

                var time = Run(Syscall(ApplicationEngine.System_Runtime_GetTime), Settings(HuyaoHeight), 10_000_000, block);
                Assert.AreEqual(VMState.HALT, time.State, time.Fault);
                Assert.AreEqual((long)block.Timestamp, time.Top);

                var container = Run(ScriptContainerScript(), Settings(HuyaoHeight), 10_000_000, block, Container());
                Assert.AreEqual(VMState.HALT, container.State, container.Fault);
            }
        }

        [TestMethod]
        public void RegisterRejectsUnknownHandlerBeforePublishing()
        {
            int services = ApplicationEngine.Services.Count;

            var missing = Assert.ThrowsExactly<ArgumentException>(() => InvokeRegister("NoSuchInteropHandler"));
            Assert.AreEqual("handler", missing.ParamName);
            Assert.Contains("NoSuchInteropHandler", missing.Message);
            Assert.Contains("is not found", missing.Message);

            // The lookup throws before the descriptor is published.
            Assert.AreEqual(services, ApplicationEngine.Services.Count);
        }

        [TestMethod]
        public void GetSysCallPriceThrowsWhenNoEntryMatches()
        {
            PriceProbe();
            var handler = GetType().GetMethod(nameof(PriceProbe), BindingFlags.NonPublic | BindingFlags.Static)!;
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.Remove(Hardfork.HF_Huyao)
            };
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, _snapshot.CloneCache(), null, settings, 1_000_000);

            var empty = new InteropDescriptor
            {
                Name = "Coverage.EmptyPrices",
                Handler = handler,
                Prices = [],
            };
            var disabled = new InteropDescriptor
            {
                Name = "Coverage.DisabledFork",
                Handler = handler,
                Prices = [new KeyValuePair<Hardfork?, long>(Hardfork.HF_Huyao, 1)],
            };

            var emptyEx = Assert.ThrowsExactly<UnreachableException>(() => InvokeGetSysCallPrice(engine, empty));
            Assert.Contains("Coverage.EmptyPrices", emptyEx.Message);
            var disabledEx = Assert.ThrowsExactly<UnreachableException>(() => InvokeGetSysCallPrice(engine, disabled));
            Assert.Contains("Coverage.DisabledFork", disabledEx.Message);
        }

        private void AssertGasBoundary(InteropDescriptor descriptor, long fee, bool huyao)
        {
            var block = BlockAt(huyao ? HuyaoHeight : HuyaoHeight - 1);
            var settings = Settings(HuyaoHeight);
            var script = Syscall(descriptor);

            var exact = Run(script, settings, fee, block);
            var shortByOne = Run(script, settings, fee - 1, block);

            Assert.AreEqual(VMState.HALT, exact.State, exact.Fault);
            Assert.AreEqual(fee, exact.Fee);
            Assert.AreEqual(VMState.FAULT, shortByOne.State);
            Assert.Contains("Insufficient GAS.", shortByOne.Fault);
            Assert.AreEqual(0, shortByOne.GasLeft);
        }

        private RunResult Run(byte[] script, ProtocolSettings settings, long gas, Block? block, IVerifiable? container = null, TriggerType trigger = TriggerType.Application, bool whitelist = false)
        {
            using var engine = ApplicationEngine.Create(trigger, container, _snapshot.CloneCache(), block, settings, gas);
            if (whitelist)
                engine.LoadScript(script, configureState: p => p.WhiteListed = true);
            else if (trigger == TriggerType.Verification)
                engine.LoadScript(script, configureState: p => p.CallFlags = CallFlags.ReadOnly);
            else
                engine.LoadScript(script);

            var state = engine.Execute();
            BigInteger? top = null;
            if (state == VMState.HALT && engine.ResultStack.Count == 1 && engine.ResultStack.Peek() is VM.Types.Integer)
                top = engine.ResultStack.Peek().GetInteger();
            return new RunResult(state, engine.FeeConsumed, engine.FaultException?.ToString() ?? "", engine.GasLeft, top);
        }

        private static void AssertPrice(InteropDescriptor descriptor, long fixedPrice, long huyaoCoefficient)
        {
            Assert.HasCount(2, descriptor.Prices, descriptor.Name);
            Assert.AreEqual(Hardfork.HF_Huyao, descriptor.Prices[0].Key);
            Assert.AreEqual(huyaoCoefficient, descriptor.Prices[0].Value);
            Assert.IsNull(descriptor.Prices[1].Key);
            Assert.AreEqual(fixedPrice * ApplicationEngine.OpcodePriceMultiplier, descriptor.Prices[1].Value);
        }

        private static ProtocolSettings Settings(uint huyaoHeight) => TestProtocolSettings.Default with
        {
            Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, huyaoHeight)
        };

        private static Block BlockAt(uint index) => new()
        {
            Header = new Header
            {
                Index = index,
                Timestamp = 1_700_000_000_000,
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                Witness = Witness.Empty,
            },
            Transactions = [],
        };

        private static Transaction Container() => new()
        {
            Script = new byte[] { 0x01 },
            Attributes = [],
            Signers = [new() { Account = UInt160.Zero }],
            NetworkFee = 0,
            SystemFee = 0,
            Nonce = 1,
            ValidUntilBlock = 100,
            Version = 0,
            Witnesses = [new() { VerificationScript = new byte[] { 0x01 } }],
        };

        private static byte[] Syscall(InteropDescriptor descriptor)
        {
            using var script = new ScriptBuilder();
            script.EmitSysCall(descriptor);
            return script.ToArray();
        }

        private static byte[] ScriptContainerScript() => Syscall(ApplicationEngine.System_Runtime_GetScriptContainer);

        private static byte[] BurnScript(long datoshi)
        {
            using var script = new ScriptBuilder();
            script.EmitPush(datoshi);
            script.EmitSysCall(ApplicationEngine.System_Runtime_BurnGas);
            return script.ToArray();
        }

        private static void InvokeRegister(string handler)
        {
            var method = typeof(ApplicationEngine).GetMethod("Register", BindingFlags.NonPublic | BindingFlags.Static)!;
            try
            {
                method.Invoke(null, ["Coverage." + handler, handler, 1L, CallFlags.None, null]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        private static void InvokeGetSysCallPrice(ApplicationEngine engine, InteropDescriptor descriptor)
        {
            var method = typeof(ApplicationEngine).GetMethod("GetSysCallPrice", BindingFlags.NonPublic | BindingFlags.Instance)!;
            try
            {
                method.Invoke(engine, [descriptor]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        private static void PriceProbe()
        {
        }

        private static long LegacyDatoshi(long fixedPrice) => fixedPrice * ExecFeeFactor;

        private static long HuyaoDatoshi(long coefficient) => (coefficient * ExecFeeFactor + 999) / 1000;

        private readonly record struct RunResult(VMState State, long Fee, string Fault, long GasLeft, BigInteger? Top);
    }
}
