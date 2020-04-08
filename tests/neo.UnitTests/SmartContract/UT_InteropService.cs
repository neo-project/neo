using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public partial class UT_InteropService
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void Runtime_GetNotifications_Test()
        {
            UInt160 scriptHash2;
            var snapshot = Blockchain.Singleton.GetSnapshot();

            using (var script = new ScriptBuilder())
            {
                // Drop arguments

                script.Emit(OpCode.NIP);

                // Notify method

                script.EmitSysCall(InteropService.Runtime.Notify);

                // Add return

                script.EmitPush(true);

                // Mock contract

                scriptHash2 = script.ToArray().ToScriptHash();

                snapshot.Contracts.Delete(scriptHash2);
                snapshot.Contracts.Add(scriptHash2, new Neo.Ledger.ContractState()
                {
                    Script = script.ToArray(),
                    Manifest = ContractManifest.CreateDefault(scriptHash2),
                });
            }

            // Wrong length

            using (var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true))
            using (var script = new ScriptBuilder())
            {
                // Retrive

                script.EmitPush(1);
                script.EmitSysCall(InteropService.Runtime.GetNotifications);

                // Execute

                engine.LoadScript(script.ToArray());

                Assert.AreEqual(VMState.FAULT, engine.Execute());
            }

            // All test

            using (var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true))
            using (var script = new ScriptBuilder())
            {
                // Notification 1 -> 13

                script.EmitPush(13);
                script.EmitSysCall(InteropService.Runtime.Notify);

                // Call script

                script.EmitAppCall(scriptHash2, "test");

                // Drop return

                script.Emit(OpCode.DROP);

                // Receive all notifications

                script.Emit(OpCode.PUSHNULL);
                script.EmitSysCall(InteropService.Runtime.GetNotifications);

                // Execute

                engine.LoadScript(script.ToArray());
                var currentScriptHash = engine.EntryScriptHash;

                Assert.AreEqual(VMState.HALT, engine.Execute());
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.AreEqual(2, engine.Notifications.Count);

                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(VM.Types.Array));

                var array = (VM.Types.Array)engine.ResultStack.Pop();

                // Check syscall result

                AssertNotification(array[1], scriptHash2, "test");
                AssertNotification(array[0], currentScriptHash, 13);

                // Check notifications

                Assert.AreEqual(scriptHash2, engine.Notifications[1].ScriptHash);
                Assert.AreEqual("test", engine.Notifications[1].State.GetString());

                Assert.AreEqual(currentScriptHash, engine.Notifications[0].ScriptHash);
                Assert.AreEqual(13, engine.Notifications[0].State.GetBigInteger());
            }

            // Script notifications

            using (var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true))
            using (var script = new ScriptBuilder())
            {
                // Notification 1 -> 13

                script.EmitPush(13);
                script.EmitSysCall(InteropService.Runtime.Notify);

                // Call script

                script.EmitAppCall(scriptHash2, "test");

                // Drop return

                script.Emit(OpCode.DROP);

                // Receive all notifications

                script.EmitPush(scriptHash2.ToArray());
                script.EmitSysCall(InteropService.Runtime.GetNotifications);

                // Execute

                engine.LoadScript(script.ToArray());
                var currentScriptHash = engine.EntryScriptHash;

                Assert.AreEqual(VMState.HALT, engine.Execute());
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.AreEqual(2, engine.Notifications.Count);

                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(VM.Types.Array));

                var array = (VM.Types.Array)engine.ResultStack.Pop();

                // Check syscall result

                AssertNotification(array[0], scriptHash2, "test");

                // Check notifications

                Assert.AreEqual(scriptHash2, engine.Notifications[1].ScriptHash);
                Assert.AreEqual("test", engine.Notifications[1].State.GetString());

                Assert.AreEqual(currentScriptHash, engine.Notifications[0].ScriptHash);
                Assert.AreEqual(13, engine.Notifications[0].State.GetBigInteger());
            }

            // Clean storage

            snapshot.Contracts.Delete(scriptHash2);
        }

        private void AssertNotification(StackItem stackItem, UInt160 scriptHash, string notification)
        {
            Assert.IsInstanceOfType(stackItem, typeof(VM.Types.Array));

            var array = (VM.Types.Array)stackItem;
            Assert.AreEqual(2, array.Count);
            CollectionAssert.AreEqual(scriptHash.ToArray(), array[0].GetSpan().ToArray());
            Assert.AreEqual(notification, array[1].GetString());
        }

        private void AssertNotification(StackItem stackItem, UInt160 scriptHash, int notification)
        {
            Assert.IsInstanceOfType(stackItem, typeof(VM.Types.Array));

            var array = (VM.Types.Array)stackItem;
            Assert.AreEqual(2, array.Count);
            CollectionAssert.AreEqual(scriptHash.ToArray(), array[0].GetSpan().ToArray());
            Assert.AreEqual(notification, array[1].GetBigInteger());
        }

        [TestMethod]
        public void TestExecutionEngine_GetScriptContainer()
        {
            var engine = GetEngine(true);
            InteropService.Invoke(engine, InteropService.Runtime.GetScriptContainer).Should().BeTrue();
            var stackItem = ((VM.Types.Array)engine.CurrentContext.EvaluationStack.Pop()).ToArray();
            stackItem.Length.Should().Be(8);
            stackItem[0].GetSpan().ToHexString().Should().Be(TestUtils.GetTransaction().Hash.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestExecutionEngine_GetExecutingScriptHash()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.Runtime.GetExecutingScriptHash).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString()
                .Should().Be(engine.CurrentScriptHash.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestExecutionEngine_GetCallingScriptHash()
        {
            // Test without

            var engine = GetEngine(true);
            InteropService.Invoke(engine, InteropService.Runtime.GetCallingScriptHash).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().Should().Be(StackItem.Null);

            // Test real

            using ScriptBuilder scriptA = new ScriptBuilder();
            scriptA.Emit(OpCode.DROP); // Drop arguments
            scriptA.Emit(OpCode.DROP); // Drop method
            scriptA.EmitSysCall(InteropService.Runtime.GetCallingScriptHash);

            var contract = new ContractState()
            {
                Manifest = ContractManifest.CreateDefault(scriptA.ToArray().ToScriptHash()),
                Script = scriptA.ToArray()
            };

            engine = GetEngine(true, true, false);
            engine.Snapshot.Contracts.Add(contract.ScriptHash, contract);

            using ScriptBuilder scriptB = new ScriptBuilder();
            scriptB.EmitAppCall(contract.ScriptHash, "");
            engine.LoadScript(scriptB.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());

            engine.ResultStack.Pop().GetSpan().ToHexString().Should().Be(scriptB.ToArray().ToScriptHash().ToArray().ToHexString());
        }

        [TestMethod]
        public void TestExecutionEngine_GetEntryScriptHash()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.Runtime.GetEntryScriptHash).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString()
                .Should().Be(engine.EntryScriptHash.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestRuntime_Platform()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.Runtime.Platform).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString()
                .Should().Be(Encoding.ASCII.GetBytes("NEO").ToHexString());
        }

        [TestMethod]
        public void TestRuntime_GetTrigger()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.Runtime.GetTrigger).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger()
                .Should().Be((int)engine.Trigger);
        }

        [TestMethod]
        public void TestRuntime_CheckWitness()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            ECPoint pubkey = keyPair.PublicKey;

            var engine = GetEngine(true);
            ((Transaction)engine.ScriptContainer).Sender = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();

            engine.CurrentContext.EvaluationStack.Push(pubkey.EncodePoint(true));
            InteropService.Invoke(engine, InteropService.Runtime.CheckWitness).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Peek().GetType().Should().Be(typeof(Neo.VM.Types.Boolean));
            engine.CurrentContext.EvaluationStack.Pop().ToBoolean().Should().Be(false);

            engine.CurrentContext.EvaluationStack.Push(((Transaction)engine.ScriptContainer).Sender.ToArray());
            InteropService.Invoke(engine, InteropService.Runtime.CheckWitness).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Peek().GetType().Should().Be(typeof(Neo.VM.Types.Boolean));
            engine.CurrentContext.EvaluationStack.Pop().ToBoolean().Should().Be(false);

            engine.CurrentContext.EvaluationStack.Push(new byte[0]);
            InteropService.Invoke(engine, InteropService.Runtime.CheckWitness).Should().BeFalse();
        }

        [TestMethod]
        public void TestRuntime_Log()
        {
            var engine = GetEngine(true);
            string message = "hello";
            engine.CurrentContext.EvaluationStack.Push(Encoding.UTF8.GetBytes(message));
            ApplicationEngine.Log += LogEvent;
            InteropService.Invoke(engine, InteropService.Runtime.Log).Should().BeTrue();
            ((Transaction)engine.ScriptContainer).Script.ToHexString().Should().Be(new byte[] { 0x01, 0x02, 0x03 }.ToHexString());
            ApplicationEngine.Log -= LogEvent;
        }

        [TestMethod]
        public void TestRuntime_GetTime()
        {
            Block block = new Block();
            TestUtils.SetupBlockWithValues(block, UInt256.Zero, out var merkRootVal, out var val160, out var timestampVal, out var indexVal, out var scriptVal, out var transactionsVal, 0);
            var engine = GetEngine(true, true);
            engine.Snapshot.PersistingBlock = block;

            InteropService.Invoke(engine, InteropService.Runtime.GetTime).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger().Should().Be(block.Timestamp);
        }

        [TestMethod]
        public void TestRuntime_Serialize()
        {
            var engine = GetEngine();
            engine.CurrentContext.EvaluationStack.Push(100);
            InteropService.Invoke(engine, InteropService.Binary.Serialize).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString()
                .Should().Be(new byte[] { 0x21, 0x01, 0x64 }.ToHexString());

            engine.CurrentContext.EvaluationStack.Push(new byte[1024 * 1024 * 2]); //Larger than MaxItemSize
            Assert.ThrowsException<InvalidOperationException>(() => InteropService.Invoke(engine, InteropService.Binary.Serialize));

            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(new object()));  //NotSupportedException
            Assert.ThrowsException<NotSupportedException>(() => InteropService.Invoke(engine, InteropService.Binary.Serialize));
        }

        [TestMethod]
        public void TestRuntime_Deserialize()
        {
            var engine = GetEngine();
            engine.CurrentContext.EvaluationStack.Push(100);
            InteropService.Invoke(engine, InteropService.Binary.Serialize).Should().BeTrue();
            InteropService.Invoke(engine, InteropService.Binary.Deserialize).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger().Should().Be(100);

            engine.CurrentContext.EvaluationStack.Push(new byte[] { 0xfa, 0x01 }); //FormatException
            Assert.ThrowsException<FormatException>(() => InteropService.Invoke(engine, InteropService.Binary.Deserialize));
        }

        [TestMethod]
        public void TestRuntime_GetInvocationCounter()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.Runtime.GetInvocationCounter).Should().BeFalse();
            engine.InvocationCounter.TryAdd(engine.CurrentScriptHash, 10);
            InteropService.Invoke(engine, InteropService.Runtime.GetInvocationCounter).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger().Should().Be(10);
        }

        [TestMethod]
        public void TestCrypto_Verify()
        {
            var engine = GetEngine(true);
            IVerifiable iv = engine.ScriptContainer;
            byte[] message = iv.GetHashData();
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            ECPoint pubkey = keyPair.PublicKey;
            byte[] signature = Crypto.Sign(message, privateKey, pubkey.EncodePoint(false).Skip(1).ToArray());

            engine.CurrentContext.EvaluationStack.Push(signature);
            engine.CurrentContext.EvaluationStack.Push(pubkey.EncodePoint(false));
            engine.CurrentContext.EvaluationStack.Push(message);
            InteropService.Invoke(engine, InteropService.Crypto.ECDsaVerify).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().ToBoolean().Should().BeTrue();

            byte[] wrongkey = pubkey.EncodePoint(false);
            wrongkey[0] = 5;
            engine.CurrentContext.EvaluationStack.Push(signature);
            engine.CurrentContext.EvaluationStack.Push(wrongkey);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(engine.ScriptContainer));
            InteropService.Invoke(engine, InteropService.Crypto.ECDsaVerify).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Peek().ToBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void TestBlockchain_GetHeight()
        {
            var engine = GetEngine(true, true);
            InteropService.Invoke(engine, InteropService.Blockchain.GetHeight).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger().Should().Be(0);
        }

        [TestMethod]
        public void TestBlockchain_GetBlock()
        {
            var engine = GetEngine(true, true);

            engine.CurrentContext.EvaluationStack.Push(new byte[] { 0x01 });
            InteropService.Invoke(engine, InteropService.Blockchain.GetBlock).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().Should().Be(StackItem.Null);

            byte[] data1 = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            engine.CurrentContext.EvaluationStack.Push(data1);
            InteropService.Invoke(engine, InteropService.Blockchain.GetBlock).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().ToBoolean().Should().BeFalse();

            byte[] data2 = new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01 };
            engine.CurrentContext.EvaluationStack.Push(data2);
            InteropService.Invoke(engine, InteropService.Blockchain.GetBlock).Should().BeFalse();
        }

        [TestMethod]
        public void TestBlockchain_GetTransaction()
        {
            var engine = GetEngine(true, true);
            byte[] data1 = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            engine.CurrentContext.EvaluationStack.Push(data1);
            InteropService.Invoke(engine, InteropService.Blockchain.GetTransaction).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().ToBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void TestBlockchain_GetTransactionHeight()
        {
            var engine = GetEngine(true, true);
            byte[] data1 = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            engine.CurrentContext.EvaluationStack.Push(data1);
            InteropService.Invoke(engine, InteropService.Blockchain.GetTransactionHeight).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger().Should().Be(-1);
        }

        [TestMethod]
        public void TestBlockchain_GetContract()
        {
            var engine = GetEngine(true, true);
            byte[] data1 = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01 };
            engine.CurrentContext.EvaluationStack.Push(data1);
            InteropService.Invoke(engine, InteropService.Blockchain.GetContract).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().Should().Be(StackItem.Null);

            var snapshot = Blockchain.Singleton.GetSnapshot();
            var state = TestUtils.GetContract();
            snapshot.Contracts.Add(state.ScriptHash, state);
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            engine.LoadScript(new byte[] { 0x01 });
            engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
            InteropService.Invoke(engine, InteropService.Blockchain.GetContract).Should().BeTrue();
            var stackItems = ((VM.Types.Array)engine.CurrentContext.EvaluationStack.Pop()).ToArray();
            stackItems.Length.Should().Be(3);
            stackItems[0].GetType().Should().Be(typeof(ByteString));
            stackItems[0].GetSpan().ToHexString().Should().Be(state.Script.ToHexString());
            stackItems[1].ToBoolean().Should().BeFalse();
            stackItems[2].ToBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void TestStorage_GetContext()
        {
            var engine = GetEngine(false, true);
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            engine.Snapshot.Contracts.Add(state.ScriptHash, state);
            engine.LoadScript(state.Script);
            InteropService.Invoke(engine, InteropService.Storage.GetContext).Should().BeTrue();
            var ret = (InteropInterface)engine.CurrentContext.EvaluationStack.Pop();
            ret.GetInterface<StorageContext>().IsReadOnly.Should().BeFalse();
        }

        [TestMethod]
        public void TestStorage_GetReadOnlyContext()
        {
            var engine = GetEngine(false, true);
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            engine.Snapshot.Contracts.Add(state.ScriptHash, state);
            engine.LoadScript(state.Script);
            InteropService.Invoke(engine, InteropService.Storage.GetReadOnlyContext).Should().BeTrue();
            var ret = (InteropInterface)engine.CurrentContext.EvaluationStack.Pop();
            ret.GetInterface<StorageContext>().IsReadOnly.Should().BeTrue();
        }

        [TestMethod]
        public void TestStorage_Get()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;

            var storageKey = new StorageKey
            {
                Id = state.Id,
                Key = new byte[] { 0x01 }
            };

            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = true
            };
            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Storages.Add(storageKey, storageItem);
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            engine.LoadScript(new byte[] { 0x01 });

            engine.CurrentContext.EvaluationStack.Push(new byte[] { 0x01 });
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            }));
            InteropService.Invoke(engine, InteropService.Storage.Get).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString().Should().Be(storageItem.Value.ToHexString());
        }

        [TestMethod]
        public void TestStorage_Put()
        {
            var engine = GetEngine(false, true);
            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Storage.Put).Should().BeFalse();

            //CheckStorageContext fail
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            var state = TestUtils.GetContract();
            var storageContext = new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            };
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.Put).Should().BeTrue();

            //key.Length > MaxStorageKeySize
            key = new byte[InteropService.Storage.MaxKeySize + 1];
            value = new byte[] { 0x02 };
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.Put).Should().BeFalse();

            //value.Length > MaxStorageValueSize
            key = new byte[] { 0x01 };
            value = new byte[ushort.MaxValue + 1];
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.Put).Should().BeFalse();

            //context.IsReadOnly
            key = new byte[] { 0x01 };
            value = new byte[] { 0x02 };
            storageContext.IsReadOnly = true;
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.Put).Should().BeFalse();

            //storage value is constant
            var snapshot = Blockchain.Singleton.GetSnapshot();
            state.Manifest.Features = ContractFeatures.HasStorage;

            var storageKey = new StorageKey
            {
                Id = state.Id,
                Key = new byte[] { 0x01 }
            };
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = true
            };
            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Storages.Add(storageKey, storageItem);
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            engine.LoadScript(new byte[] { 0x01 });
            key = new byte[] { 0x01 };
            value = new byte[] { 0x02 };
            storageContext.IsReadOnly = false;
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.Put).Should().BeFalse();

            //success
            storageItem.IsConstant = false;
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.Put).Should().BeTrue();

            //value length == 0
            key = new byte[] { 0x01 };
            value = new byte[0];
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.Put).Should().BeTrue();
        }

        [TestMethod]
        public void TestStorage_PutEx()
        {
            var engine = GetEngine(false, true);
            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Storage.PutEx).Should().BeFalse();

            var snapshot = Blockchain.Singleton.GetSnapshot();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            var storageKey = new StorageKey
            {
                Id = 0x42000000,
                Key = new byte[] { 0x01 }
            };
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = false
            };
            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Storages.Add(storageKey, storageItem);
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            engine.LoadScript(new byte[] { 0x01 });
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };
            var storageContext = new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            };
            engine.CurrentContext.EvaluationStack.Push((int)StorageFlags.None);
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.PutEx).Should().BeTrue();
        }

        [TestMethod]
        public void TestStorage_Delete()
        {
            var engine = GetEngine(false, true);
            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Storage.Delete).Should().BeFalse();


            var snapshot = Blockchain.Singleton.GetSnapshot();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            var storageKey = new StorageKey
            {
                Id = 0x42000000,
                Key = new byte[] { 0x01 }
            };
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = false
            };
            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Storages.Add(storageKey, storageItem);
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            engine.LoadScript(new byte[] { 0x01 });
            state.Manifest.Features = ContractFeatures.HasStorage;
            var key = new byte[] { 0x01 };
            var storageContext = new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            };
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.Delete).Should().BeTrue();

            //context is readonly
            storageContext.IsReadOnly = true;
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.Delete).Should().BeFalse();
        }

        [TestMethod]
        public void TestStorageContext_AsReadOnly()
        {
            var engine = GetEngine();
            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Storage.AsReadOnly).Should().BeFalse();

            var state = TestUtils.GetContract();
            var storageContext = new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            };
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface(storageContext));
            InteropService.Invoke(engine, InteropService.Storage.AsReadOnly).Should().BeTrue();
            var ret = (InteropInterface)engine.CurrentContext.EvaluationStack.Pop();
            ret.GetInterface<StorageContext>().IsReadOnly.Should().Be(true);
        }

        [TestMethod]
        public void TestInvoke()
        {
            var engine = new ApplicationEngine(TriggerType.Verification, null, null, 0);
            InteropService.Invoke(engine, 10000).Should().BeFalse();
            InteropService.Invoke(engine, InteropService.Storage.AsReadOnly).Should().BeFalse();
        }

        [TestMethod]
        public void TestContract_Call()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            byte[] method = Encoding.UTF8.GetBytes("method");
            byte[] args = new byte[0];
            snapshot.Contracts.Add(state.ScriptHash, state);
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            engine.LoadScript(new byte[] { 0x01 });

            engine.CurrentContext.EvaluationStack.Push(args);
            engine.CurrentContext.EvaluationStack.Push(method);
            engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
            InteropService.Invoke(engine, InteropService.Contract.Call).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString().Should().Be(method.ToHexString());
            engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString().Should().Be(args.ToHexString());

            state.Manifest.Permissions[0].Methods = WildcardContainer<string>.Create("a");
            engine.CurrentContext.EvaluationStack.Push(args);
            engine.CurrentContext.EvaluationStack.Push(method);
            engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
            InteropService.Invoke(engine, InteropService.Contract.Call).Should().BeFalse();
            state.Manifest.Permissions[0].Methods = WildcardContainer<string>.CreateWildcard();

            engine.CurrentContext.EvaluationStack.Push(args);
            engine.CurrentContext.EvaluationStack.Push(method);
            engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
            InteropService.Invoke(engine, InteropService.Contract.Call).Should().BeTrue();

            engine.CurrentContext.EvaluationStack.Push(args);
            engine.CurrentContext.EvaluationStack.Push(method);
            engine.CurrentContext.EvaluationStack.Push(UInt160.Zero.ToArray());
            InteropService.Invoke(engine, InteropService.Contract.Call).Should().BeFalse();
        }

        [TestMethod]
        public void TestContract_CallEx()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            snapshot.Contracts.Add(state.ScriptHash, state);

            byte[] method = Encoding.UTF8.GetBytes("method");
            byte[] args = new byte[0];

            foreach (var flags in new CallFlags[] { CallFlags.None, CallFlags.AllowCall, CallFlags.AllowModifyStates, CallFlags.All })
            {
                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
                engine.LoadScript(new byte[] { 0x01 });

                engine.CurrentContext.EvaluationStack.Push((int)CallFlags.All);
                engine.CurrentContext.EvaluationStack.Push(args);
                engine.CurrentContext.EvaluationStack.Push(method);
                engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
                InteropService.Invoke(engine, InteropService.Contract.CallEx).Should().BeTrue();
                engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString().Should().Be(method.ToHexString());
                engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString().Should().Be(args.ToHexString());

                // Contract doesn't exists

                engine.CurrentContext.EvaluationStack.Push((int)CallFlags.All);
                engine.CurrentContext.EvaluationStack.Push(args);
                engine.CurrentContext.EvaluationStack.Push(method);
                engine.CurrentContext.EvaluationStack.Push(UInt160.Zero.ToArray());
                InteropService.Invoke(engine, InteropService.Contract.CallEx).Should().BeFalse();

                // Call with rights

                engine.CurrentContext.EvaluationStack.Push((int)flags);
                engine.CurrentContext.EvaluationStack.Push(args);
                engine.CurrentContext.EvaluationStack.Push(method);
                engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
                InteropService.Invoke(engine, InteropService.Contract.CallEx).Should().BeTrue();
                engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString().Should().Be(method.ToHexString());
                engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToHexString().Should().Be(args.ToHexString());

                // Check rights

                engine.CurrentContext.EvaluationStack.Push((int)CallFlags.All);
                engine.CurrentContext.EvaluationStack.Push(args);
                engine.CurrentContext.EvaluationStack.Push(method);
                engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
                InteropService.Invoke(engine, InteropService.Contract.CallEx).Should().Be(flags.HasFlag(CallFlags.AllowCall));
            }
        }

        [TestMethod]
        public void TestContract_Destroy()
        {
            var engine = GetEngine(false, true);
            InteropService.Invoke(engine, InteropService.Contract.Destroy).Should().BeTrue();

            var snapshot = Blockchain.Singleton.GetSnapshot();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            var scriptHash = UInt160.Parse("0xcb9f3b7c6fb1cf2c13a40637c189bdd066a272b4");
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = false
            };

            var storageKey = new StorageKey
            {
                Id = 0x43000000,
                Key = new byte[] { 0x01 }
            };
            snapshot.Contracts.Add(scriptHash, state);
            snapshot.Storages.Add(storageKey, storageItem);
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            engine.LoadScript(new byte[0]);
            InteropService.Invoke(engine, InteropService.Contract.Destroy).Should().BeTrue();
            engine.Snapshot.Storages.Find(BitConverter.GetBytes(0x43000000)).Any().Should().BeFalse();

            //storages are removed
            snapshot = Blockchain.Singleton.GetSnapshot();
            state = TestUtils.GetContract();
            snapshot.Contracts.Add(scriptHash, state);
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            engine.LoadScript(new byte[0]);
            InteropService.Invoke(engine, InteropService.Contract.Destroy).Should().BeTrue();
            engine.Snapshot.Storages.Find(BitConverter.GetBytes(0x43000000)).Any().Should().BeFalse();

        }

        [TestMethod]
        public void TestContract_CreateStandardAccount()
        {
            var engine = GetEngine(true, true);
            byte[] data = "024b817ef37f2fc3d4a33fe36687e592d9f30fe24b3e28187dc8f12b3b3b2b839e".HexToBytes();

            engine.CurrentContext.EvaluationStack.Push(data);
            InteropService.Invoke(engine, InteropService.Contract.CreateStandardAccount).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray().Should().BeEquivalentTo(UInt160.Parse("0x2c847208959ec1cc94dd13bfe231fa622a404a8a").ToArray());

            data = "064b817ef37f2fc3d4a33fe36687e592d9f30fe24b3e28187dc8f12b3b3b2b839e".HexToBytes();
            engine.CurrentContext.EvaluationStack.Push(data);
            Assert.ThrowsException<FormatException>(() => InteropService.Invoke(engine, InteropService.Contract.CreateStandardAccount)).Message.Should().BeEquivalentTo("Invalid point encoding 6");

            data = "024b817ef37f2fc3d4a33fe36687e599f30fe24b3e28187dc8f12b3b3b2b839e".HexToBytes();
            engine.CurrentContext.EvaluationStack.Push(data);
            Assert.ThrowsException<FormatException>(() => InteropService.Invoke(engine, InteropService.Contract.CreateStandardAccount)).Message.Should().BeEquivalentTo("Incorrect length for compressed encoding");

            data = "02ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff".HexToBytes();
            engine.CurrentContext.EvaluationStack.Push(data);
            Assert.ThrowsException<ArgumentException>(() => InteropService.Invoke(engine, InteropService.Contract.CreateStandardAccount)).Message.Should().BeEquivalentTo("x value too large in field element");

            data = "020fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff".HexToBytes();
            engine.CurrentContext.EvaluationStack.Push(data);
            Assert.ThrowsException<ArithmeticException>(() => InteropService.Invoke(engine, InteropService.Contract.CreateStandardAccount)).Message.Should().BeEquivalentTo("Invalid point compression");

            data = "044b817ef37f2fc3d4a33fe36687e592d9f30fe24b3e28187dc8f12b3b3b2b839e".HexToBytes();
            engine.CurrentContext.EvaluationStack.Push(data);
            Assert.ThrowsException<FormatException>(() => InteropService.Invoke(engine, InteropService.Contract.CreateStandardAccount)).Message.Should().BeEquivalentTo("Incorrect length for uncompressed/hybrid encoding");

            data = "04ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff".HexToBytes();
            engine.CurrentContext.EvaluationStack.Push(data);
            Assert.ThrowsException<ArgumentException>(() => InteropService.Invoke(engine, InteropService.Contract.CreateStandardAccount)).Message.Should().BeEquivalentTo("x value too large in field element");

            data = "040fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff".HexToBytes();
            engine.CurrentContext.EvaluationStack.Push(data);
            Assert.ThrowsException<ArgumentException>(() => InteropService.Invoke(engine, InteropService.Contract.CreateStandardAccount)).Message.Should().BeEquivalentTo("x value too large in field element");
        }

        public static void LogEvent(object sender, LogEventArgs args)
        {
            Transaction tx = (Transaction)args.ScriptContainer;
            tx.Script = new byte[] { 0x01, 0x02, 0x03 };
        }

        private static ApplicationEngine GetEngine(bool hasContainer = false, bool hasSnapshot = false, bool addScript = true)
        {
            var tx = TestUtils.GetTransaction();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine engine;
            if (hasContainer && hasSnapshot)
            {
                engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, 0, true);
            }
            else if (hasContainer && !hasSnapshot)
            {
                engine = new ApplicationEngine(TriggerType.Application, tx, null, 0, true);
            }
            else if (!hasContainer && hasSnapshot)
            {
                engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            }
            else
            {
                engine = new ApplicationEngine(TriggerType.Application, null, null, 0, true);
            }
            if (addScript)
            {
                engine.LoadScript(new byte[] { 0x01 });
            }
            return engine;
        }
    }
}
