// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine.Runtime.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using Array = System.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
        [TestMethod]
        public void TestGetNetworkAndAddressVersion()
        {
            var tx = TestUtils.GetTransaction(UInt160.Zero);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, null, _system.GenesisBlock, settings: TestProtocolSettings.Default, gas: 1100_00000000);

            Assert.AreEqual(TestProtocolSettings.Default.Network, engine.GetNetwork());
            Assert.AreEqual(TestProtocolSettings.Default.AddressVersion, engine.GetAddressVersion());
        }

        [TestMethod]
        public void TestNotSupportedNotification()
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, null, _system.GenesisBlock, settings: TestProtocolSettings.Default, gas: 1100_00000000);
            engine.LoadScript(Array.Empty<byte>());
            engine.CurrentContext.GetState<ExecutionContextState>().Contract = new()
            {
                Hash = UInt160.Zero,
                Nef = null!,
                Manifest = new()
                {
                    Name = "",
                    Groups = [],
                    SupportedStandards = [],
                    Abi = new()
                    {
                        Methods = [],
                        Events = new[]
                        {
                            new ContractEventDescriptor
                            {
                                Name = "e1",
                                Parameters = new[]
                                {
                                    new ContractParameterDefinition
                                    {
                                        Name = "p1",
                                        Type = ContractParameterType.Array
                                    }
                                }
                            }
                        }
                    },
                    Permissions = [],
                    Trusts = WildcardContainer<ContractPermissionDescriptor>.CreateWildcard()
                }
            };

            // circular

            VM.Types.Array array = new();
            array.Add(array);

            Assert.ThrowsExactly<NotSupportedException>(() => engine.RuntimeNotify(Encoding.ASCII.GetBytes("e1"), array));

            // Buffer

            array.Clear();
            array.Add(new Buffer(1));
            engine.CurrentContext.GetState<ExecutionContextState>().Contract.Manifest.Abi.Events[0].Parameters[0].Type = ContractParameterType.ByteArray;

            engine.RuntimeNotify(Encoding.ASCII.GetBytes("e1"), array);
            Assert.AreEqual(StackItemType.ByteString, engine.Notifications[0].State[0].Type);

            // Pointer

            array.Clear();
            array.Add(new Pointer(Array.Empty<byte>(), 1));

            Assert.ThrowsExactly<InvalidOperationException>(() => engine.RuntimeNotify(Encoding.ASCII.GetBytes("e1"), array));

            // InteropInterface

            array.Clear();
            array.Add(new InteropInterface(new object()));
            engine.CurrentContext.GetState<ExecutionContextState>().Contract.Manifest.Abi.Events[0].Parameters[0].Type = ContractParameterType.InteropInterface;

            Assert.ThrowsExactly<NotSupportedException>(() => engine.RuntimeNotify(Encoding.ASCII.GetBytes("e1"), array));
        }

        [TestMethod]
        public void TestGetRandomSameBlock()
        {
            var tx = TestUtils.GetTransaction(UInt160.Zero);
            // Even if persisting the same block, in different ApplicationEngine instance, the random number should be different
            using var engine_1 = ApplicationEngine.Create(TriggerType.Application, tx, null, _system.GenesisBlock, settings: TestProtocolSettings.Default, gas: 1100_00000000);
            using var engine_2 = ApplicationEngine.Create(TriggerType.Application, tx, null, _system.GenesisBlock, settings: TestProtocolSettings.Default, gas: 1100_00000000);

            engine_1.LoadScript(new byte[] { 0x01 });
            engine_2.LoadScript(new byte[] { 0x01 });

            var rand_1 = engine_1.GetRandom();
            var rand_2 = engine_1.GetRandom();
            var rand_3 = engine_1.GetRandom();
            var rand_4 = engine_1.GetRandom();
            var rand_5 = engine_1.GetRandom();

            var rand_6 = engine_2.GetRandom();
            var rand_7 = engine_2.GetRandom();
            var rand_8 = engine_2.GetRandom();
            var rand_9 = engine_2.GetRandom();
            var rand_10 = engine_2.GetRandom();

            // HF_Huyao returns [0, 2^255 - 1] so the value always fits Neo VM's 32-byte integer.
            var maxInclusive = (BigInteger.One << 255) - BigInteger.One;

            Assert.IsTrue(rand_1 >= BigInteger.Zero && rand_1 <= maxInclusive);
            Assert.IsTrue(rand_2 >= BigInteger.Zero && rand_2 <= maxInclusive);
            Assert.IsTrue(rand_3 >= BigInteger.Zero && rand_3 <= maxInclusive);
            Assert.IsTrue(rand_4 >= BigInteger.Zero && rand_4 <= maxInclusive);
            Assert.IsTrue(rand_5 >= BigInteger.Zero && rand_5 <= maxInclusive);

            Assert.AreEqual(rand_6, rand_1);
            Assert.AreEqual(rand_7, rand_2);
            Assert.AreEqual(rand_8, rand_3);
            Assert.AreEqual(rand_9, rand_4);
            Assert.AreEqual(rand_10, rand_5);

            // Consecutive draws in one engine must differ (counter advances twice per call).
            Assert.AreNotEqual(rand_1, rand_2);
            Assert.AreNotEqual(rand_2, rand_3);
        }

        [TestMethod]
        public void TestGetRandomDifferentBlock()
        {
            var tx_1 = TestUtils.GetTransaction(UInt160.Zero);

            var tx_2 = new Transaction
            {
                Version = 0,
                Nonce = 2083236893,
                ValidUntilBlock = 0,
                Signers = Array.Empty<Signer>(),
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = Array.Empty<byte>(),
                SystemFee = 0,
                NetworkFee = 0,
                Witnesses = Array.Empty<Witness>()
            };

            using var engine_1 = ApplicationEngine.Create(TriggerType.Application, tx_1, null, _system.GenesisBlock, settings: TestProtocolSettings.Default, gas: 1100_00000000);
            // The next_nonce shuld be reinitialized when a new block is persisting
            using var engine_2 = ApplicationEngine.Create(TriggerType.Application, tx_2, null, _system.GenesisBlock, settings: TestProtocolSettings.Default, gas: 1100_00000000);

            var rand_1 = engine_1.GetRandom();
            var rand_2 = engine_1.GetRandom();
            var rand_3 = engine_1.GetRandom();
            var rand_4 = engine_1.GetRandom();
            var rand_5 = engine_1.GetRandom();

            var rand_6 = engine_2.GetRandom();
            var rand_7 = engine_2.GetRandom();
            var rand_8 = engine_2.GetRandom();
            var rand_9 = engine_2.GetRandom();
            var rand_10 = engine_2.GetRandom();

            var maxInclusive = (BigInteger.One << 255) - BigInteger.One;

            Assert.IsTrue(rand_1 >= BigInteger.Zero && rand_1 <= maxInclusive);
            Assert.IsTrue(rand_2 >= BigInteger.Zero && rand_2 <= maxInclusive);
            Assert.IsTrue(rand_3 >= BigInteger.Zero && rand_3 <= maxInclusive);
            Assert.IsTrue(rand_4 >= BigInteger.Zero && rand_4 <= maxInclusive);
            Assert.IsTrue(rand_5 >= BigInteger.Zero && rand_5 <= maxInclusive);

            Assert.AreNotEqual(rand_6, rand_1);
            Assert.AreNotEqual(rand_7, rand_2);
            Assert.AreNotEqual(rand_8, rand_3);
            Assert.AreNotEqual(rand_9, rand_4);
            Assert.AreNotEqual(rand_10, rand_5);
        }

        [TestMethod]
        public void TestGetRandom_Huyao_FitsVmIntegerAndRange()
        {
            var tx = TestUtils.GetTransaction(UInt160.Zero);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, null, _system.GenesisBlock,
                settings: TestProtocolSettings.Default, gas: 1100_00000000);

            var maxInclusive = (BigInteger.One << 255) - BigInteger.One;
            var twoPow128 = BigInteger.One << 128;
            var sawAbove128Bits = false;

            for (var i = 0; i < 64; i++)
            {
                var value = engine.GetRandom();
                Assert.IsGreaterThanOrEqualTo(BigInteger.Zero, value);
                Assert.IsLessThanOrEqualTo(maxInclusive, value);
                // Neo VM integers are max 32 bytes (signed); high bit is cleared under HF_Huyao.
                Assert.IsLessThanOrEqualTo(32, value.ToByteArray().Length);
                if (value >= twoPow128)
                    sawAbove128Bits = true;
            }

            // New path must be able to produce values outside the old 128-bit range.
            Assert.IsTrue(sawAbove128Bits);
        }

        [TestMethod]
        public void TestGetRandom_PreHuyao_Remains128Bit()
        {
            var settings = CreateProtocolSettingsUpTo(Hardfork.HF_Gorgon);
            var tx = TestUtils.GetTransaction(UInt160.Zero);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, null, _system.GenesisBlock,
                settings: settings, gas: 1100_00000000);

            Assert.IsFalse(engine.IsHardforkEnabled(Hardfork.HF_Huyao));
            Assert.IsTrue(engine.IsHardforkEnabled(Hardfork.HF_Aspidochelone));

            var maxExclusive = BigInteger.One << 128;
            for (var i = 0; i < 32; i++)
            {
                var value = engine.GetRandom();
                Assert.IsGreaterThanOrEqualTo(BigInteger.Zero, value);
                Assert.IsLessThan(maxExclusive, value);
            }
        }

        [TestMethod]
        public void TestGetRandom_Huyao_IsDeterministicForSameSeed()
        {
            var tx = TestUtils.GetTransaction(UInt160.Zero);
            using var engine1 = ApplicationEngine.Create(TriggerType.Application, tx, null, _system.GenesisBlock,
                settings: TestProtocolSettings.Default, gas: 1100_00000000);
            using var engine2 = ApplicationEngine.Create(TriggerType.Application, tx, null, _system.GenesisBlock,
                settings: TestProtocolSettings.Default, gas: 1100_00000000);

            for (var i = 0; i < 8; i++)
                Assert.AreEqual(engine1.GetRandom(), engine2.GetRandom());
        }

        private static ProtocolSettings CreateProtocolSettingsUpTo(Hardfork maxEnabledHardfork)
        {
            var hardforks = Enum.GetValues(typeof(Hardfork))
                .Cast<Hardfork>()
                .Where(hf => hf <= maxEnabledHardfork)
                .ToDictionary(hf => hf, _ => 0u);

            return TestProtocolSettings.Default with
            {
                Hardforks = hardforks.ToImmutableDictionary()
            };
        }

        [TestMethod]
        public void TestInvalidUtf8LogMessage()
        {
            var tx_1 = TestUtils.GetTransaction(UInt160.Zero);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx_1, null, _system.GenesisBlock, settings: TestProtocolSettings.Default, gas: 1100_00000000);
            var msg = new byte[]
            {
                68, 216, 160, 6, 89, 102, 86, 72, 37, 15, 132, 45, 76, 221, 170, 21, 128, 51, 34, 168, 205, 56, 10, 228, 51, 114, 4, 218, 245, 155, 172, 132
            };
            Assert.ThrowsExactly<ArgumentException>(() => engine.RuntimeLog(msg));
        }

        [TestMethod]
        public void FeeConsumed_ExcessiveFee_DoesNotOverflow()
        {
            var snapshot = _snapshotCache.CloneCache();
            const long gas = 100_000_000;

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, gas: gas);
            var feeConsumedField = typeof(ApplicationEngine).GetField("_feeConsumed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            feeConsumedField.SetValue(engine, BigInteger.Parse("92233720368547758080000000000000"));

            Assert.AreEqual(gas, engine.FeeConsumed);

            var executed = new Blockchain.ApplicationExecuted(engine);
            Assert.AreEqual(gas, executed.GasConsumed);
        }

        [TestMethod]
        public void GasLeft_AfterInsufficientGas_ReturnsZero()
        {
            var snapshot = _snapshotCache.CloneCache();
            const long gas = 100_000;

            using var script = new ScriptBuilder();
            script.EmitPush(gas + 1);
            script.EmitSysCall(ApplicationEngine.System_Runtime_BurnGas);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, gas: gas);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());

            Assert.AreEqual(0L, engine.GasLeft);
        }

        [TestMethod]
        public void FeeConsumed_InsufficientGas_DoesNotCapInternalFee()
        {
            var snapshot = _snapshotCache.CloneCache();
            const long gas = 10_000;

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, gas: gas);
            var excessFee = (BigInteger)gas * ApplicationEngine.FeeFactor + 1;
            var exception = Assert.ThrowsExactly<InvalidOperationException>(() => engine.AddFee(excessFee, false));
            Assert.AreEqual("Insufficient GAS.", exception.Message);

            var feeConsumedField = typeof(ApplicationEngine).GetField("_feeConsumed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            Assert.AreEqual(excessFee, (BigInteger)feeConsumedField.GetValue(engine)!);
            Assert.AreEqual(gas + 1, engine.FeeConsumed);
        }
    }
}
