// Copyright (C) 2015-2025 The Neo Project.
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
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
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
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            Assert.AreEqual(TestBlockchain.TheNeoSystem.Settings.Network, engine.GetNetwork());
            Assert.AreEqual(TestBlockchain.TheNeoSystem.Settings.AddressVersion, engine.GetAddressVersion());
        }

        [TestMethod]
        public void TestNotSupportedNotification()
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            engine.LoadScript(Array.Empty<byte>());
            engine.CurrentContext.GetState<ExecutionContextState>().Contract = new()
            {
                Manifest = new()
                {
                    Abi = new()
                    {
                        Events = new[]
                        {
                            new ContractEventDescriptor
                            {
                                Name = "e1",
                                Parameters = new[]
                                {
                                    new ContractParameterDefinition
                                    {
                                        Type = ContractParameterType.Array
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // circular

            VM.Types.Array array = new();
            array.Add(array);

            Assert.ThrowsException<NotSupportedException>(() => engine.RuntimeNotify(Encoding.ASCII.GetBytes("e1"), array));

            // Buffer

            array.Clear();
            array.Add(new Buffer(1));
            engine.CurrentContext.GetState<ExecutionContextState>().Contract.Manifest.Abi.Events[0].Parameters[0].Type = ContractParameterType.ByteArray;

            engine.RuntimeNotify(Encoding.ASCII.GetBytes("e1"), array);
            Assert.AreEqual(StackItemType.ByteString, engine.Notifications[0].State[0].Type);

            // Pointer

            array.Clear();
            array.Add(new Pointer(Array.Empty<byte>(), 1));

            Assert.ThrowsException<InvalidOperationException>(() => engine.RuntimeNotify(Encoding.ASCII.GetBytes("e1"), array));

            // InteropInterface

            array.Clear();
            array.Add(new InteropInterface(new object()));
            engine.CurrentContext.GetState<ExecutionContextState>().Contract.Manifest.Abi.Events[0].Parameters[0].Type = ContractParameterType.InteropInterface;

            Assert.ThrowsException<NotSupportedException>(() => engine.RuntimeNotify(Encoding.ASCII.GetBytes("e1"), array));
        }

        [TestMethod]
        public void TestGetRandomSameBlock()
        {
            var tx = TestUtils.GetTransaction(UInt160.Zero);
            // Even if persisting the same block, in different ApplicationEngine instance, the random number should be different
            using var engine_1 = ApplicationEngine.Create(TriggerType.Application, tx, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            using var engine_2 = ApplicationEngine.Create(TriggerType.Application, tx, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

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

            Assert.AreEqual(BigInteger.Parse("271339657438512451304577787170704246350"), rand_1);
            Assert.AreEqual(BigInteger.Parse("98548189559099075644778613728143131367"), rand_2);
            Assert.AreEqual(BigInteger.Parse("247654688993873392544380234598471205121"), rand_3);
            Assert.AreEqual(BigInteger.Parse("291082758879475329976578097236212073607"), rand_4);
            Assert.AreEqual(BigInteger.Parse("247152297361212656635216876565962360375"), rand_5);

            Assert.AreEqual(rand_6, rand_1);
            Assert.AreEqual(rand_7, rand_2);
            Assert.AreEqual(rand_8, rand_3);
            Assert.AreEqual(rand_9, rand_4);
            Assert.AreEqual(rand_10, rand_5);
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

            using var engine_1 = ApplicationEngine.Create(TriggerType.Application, tx_1, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            // The next_nonce shuld be reinitialized when a new block is persisting
            using var engine_2 = ApplicationEngine.Create(TriggerType.Application, tx_2, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

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

            Assert.AreEqual(BigInteger.Parse("271339657438512451304577787170704246350"), rand_1);
            Assert.AreEqual(BigInteger.Parse("98548189559099075644778613728143131367"), rand_2);
            Assert.AreEqual(BigInteger.Parse("247654688993873392544380234598471205121"), rand_3);
            Assert.AreEqual(BigInteger.Parse("291082758879475329976578097236212073607"), rand_4);
            Assert.AreEqual(BigInteger.Parse("247152297361212656635216876565962360375"), rand_5);

            Assert.AreNotEqual(rand_6, rand_1);
            Assert.AreNotEqual(rand_7, rand_2);
            Assert.AreNotEqual(rand_8, rand_3);
            Assert.AreNotEqual(rand_9, rand_4);
            Assert.AreNotEqual(rand_10, rand_5);
        }

        [TestMethod]
        public void TestInvalidUtf8LogMessage()
        {
            var tx_1 = TestUtils.GetTransaction(UInt160.Zero);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx_1, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            var msg = new byte[]
            {
                68, 216, 160, 6, 89, 102, 86, 72, 37, 15, 132, 45, 76, 221, 170, 21, 128, 51, 34, 168, 205, 56, 10, 228, 51, 114, 4, 218, 245, 155, 172, 132
            };
            Assert.ThrowsException<ArgumentException>(() => engine.RuntimeLog(msg));
        }
    }
}
