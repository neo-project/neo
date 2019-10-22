using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
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
            var snapshot = TestBlockchain.GetStore().GetSnapshot();

            using (var script = new ScriptBuilder())
            {
                // Drop arguments

                script.Emit(VM.OpCode.TOALTSTACK);
                script.Emit(VM.OpCode.DROP);
                script.Emit(VM.OpCode.FROMALTSTACK);

                // Notify method

                script.EmitSysCall(InteropService.System_Runtime_Notify);

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
                script.EmitSysCall(InteropService.System_Runtime_GetNotifications);

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
                script.EmitSysCall(InteropService.System_Runtime_Notify);

                // Call script

                script.EmitAppCall(scriptHash2, "test");

                // Drop return

                script.Emit(OpCode.DROP);

                // Receive all notifications

                script.Emit(OpCode.PUSHNULL);
                script.EmitSysCall(InteropService.System_Runtime_GetNotifications);

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
                script.EmitSysCall(InteropService.System_Runtime_Notify);

                // Call script

                script.EmitAppCall(scriptHash2, "test");

                // Drop return

                script.Emit(OpCode.DROP);

                // Receive all notifications

                script.EmitPush(scriptHash2.ToArray());
                script.EmitSysCall(InteropService.System_Runtime_GetNotifications);

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
            CollectionAssert.AreEqual(scriptHash.ToArray(), array[0].GetByteArray());
            Assert.AreEqual(notification, array[1].GetString());
        }

        private void AssertNotification(StackItem stackItem, UInt160 scriptHash, int notification)
        {
            Assert.IsInstanceOfType(stackItem, typeof(VM.Types.Array));

            var array = (VM.Types.Array)stackItem;
            Assert.AreEqual(2, array.Count);
            CollectionAssert.AreEqual(scriptHash.ToArray(), array[0].GetByteArray());
            Assert.AreEqual(notification, array[1].GetBigInteger());
        }

        [TestMethod]
        public void TestExecutionEngine_GetScriptContainer()
        {
            var engine = GetEngine(true);
            InteropService.Invoke(engine, InteropService.System_ExecutionEngine_GetScriptContainer).Should().BeTrue();
            var stackItem = ((VM.Types.Array)engine.CurrentContext.EvaluationStack.Pop()).ToArray();
            stackItem.Length.Should().Be(8);
            stackItem[0].GetByteArray().ToHexString().Should().Be(TestUtils.GetTransaction().Hash.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestExecutionEngine_GetExecutingScriptHash()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.System_ExecutionEngine_GetExecutingScriptHash).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetByteArray().ToHexString()
                .Should().Be(engine.CurrentScriptHash.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestExecutionEngine_GetCallingScriptHash()
        {
            var engine = GetEngine(true);
            InteropService.Invoke(engine, InteropService.System_ExecutionEngine_GetCallingScriptHash).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().Should().Be(StackItem.Null);

            engine = GetEngine(true);
            engine.LoadScript(new byte[] { 0x01 });
            InteropService.Invoke(engine, InteropService.System_ExecutionEngine_GetCallingScriptHash).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetByteArray().ToHexString()
                .Should().Be(engine.CallingScriptHash.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestExecutionEngine_GetEntryScriptHash()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.System_ExecutionEngine_GetEntryScriptHash).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetByteArray().ToHexString()
                .Should().Be(engine.EntryScriptHash.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestRuntime_Platform()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.System_Runtime_Platform).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetByteArray().ToHexString()
                .Should().Be(Encoding.ASCII.GetBytes("NEO").ToHexString());
        }

        [TestMethod]
        public void TestRuntime_GetTrigger()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.System_Runtime_GetTrigger).Should().BeTrue();
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
            InteropService.Invoke(engine, InteropService.System_Runtime_CheckWitness).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Peek().GetType().Should().Be(typeof(Neo.VM.Types.Boolean));
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().Be(false);

            engine.CurrentContext.EvaluationStack.Push(((Transaction)engine.ScriptContainer).Sender.ToArray());
            InteropService.Invoke(engine, InteropService.System_Runtime_CheckWitness).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Peek().GetType().Should().Be(typeof(Neo.VM.Types.Boolean));
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().Be(false);

            engine.CurrentContext.EvaluationStack.Push(new byte[0]);
            InteropService.Invoke(engine, InteropService.System_Runtime_CheckWitness).Should().BeFalse();
        }

        [TestMethod]
        public void TestRuntime_Log()
        {
            var engine = GetEngine(true);
            string message = "hello";
            engine.CurrentContext.EvaluationStack.Push(Encoding.UTF8.GetBytes(message));
            ApplicationEngine.Log += LogEvent;
            InteropService.Invoke(engine, InteropService.System_Runtime_Log).Should().BeTrue();
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

            InteropService.Invoke(engine, InteropService.System_Runtime_GetTime).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger().Should().Be(block.Timestamp);
        }

        [TestMethod]
        public void TestRuntime_Serialize()
        {
            var engine = GetEngine();
            engine.CurrentContext.EvaluationStack.Push(100);
            InteropService.Invoke(engine, InteropService.System_Runtime_Serialize).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetByteArray().ToHexString()
                .Should().Be(new byte[] { 0x02, 0x01, 0x64 }.ToHexString());

            engine.CurrentContext.EvaluationStack.Push(new byte[1024 * 1024 * 2]); //Larger than MaxItemSize
            InteropService.Invoke(engine, InteropService.System_Runtime_Serialize).Should().BeFalse();

            engine.CurrentContext.EvaluationStack.Push(new TestInteropInterface());  //NotSupportedException
            InteropService.Invoke(engine, InteropService.System_Runtime_Serialize).Should().BeFalse();
        }

        [TestMethod]
        public void TestRuntime_Deserialize()
        {
            var engine = GetEngine();
            engine.CurrentContext.EvaluationStack.Push(100);
            InteropService.Invoke(engine, InteropService.System_Runtime_Serialize).Should().BeTrue();
            InteropService.Invoke(engine, InteropService.System_Runtime_Deserialize).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger().Should().Be(100);

            engine.CurrentContext.EvaluationStack.Push(new byte[] { 0xfa, 0x01 }); //FormatException
            InteropService.Invoke(engine, InteropService.System_Runtime_Deserialize).Should().BeFalse();
        }

        [TestMethod]
        public void TestRuntime_GetInvocationCounter()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.System_Runtime_GetInvocationCounter).Should().BeFalse();
            engine.InvocationCounter.TryAdd(engine.CurrentScriptHash, 10);
            InteropService.Invoke(engine, InteropService.System_Runtime_GetInvocationCounter).Should().BeTrue();
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
            byte[] signature = Crypto.Default.Sign(message, privateKey, pubkey.EncodePoint(false).Skip(1).ToArray());

            engine.CurrentContext.EvaluationStack.Push(signature);
            engine.CurrentContext.EvaluationStack.Push(pubkey.EncodePoint(false));
            engine.CurrentContext.EvaluationStack.Push(message);
            InteropService.Invoke(engine, InteropService.System_Crypto_Verify).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeTrue();

            byte[] wrongkey = pubkey.EncodePoint(false);
            wrongkey[0] = 5;
            engine.CurrentContext.EvaluationStack.Push(signature);
            engine.CurrentContext.EvaluationStack.Push(wrongkey);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<IVerifiable>(engine.ScriptContainer));
            InteropService.Invoke(engine, InteropService.System_Crypto_Verify).Should().BeFalse();

        }

        [TestMethod]
        public void TestBlockchain_GetHeight()
        {
            var engine = GetEngine(true, true);
            InteropService.Invoke(engine, InteropService.System_Blockchain_GetHeight).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger().Should().Be(0);
        }

        [TestMethod]
        public void TestBlockchain_GetBlock()
        {
            var engine = GetEngine(true, true);

            engine.CurrentContext.EvaluationStack.Push(new byte[] { 0x01 });
            InteropService.Invoke(engine, InteropService.System_Blockchain_GetBlock).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().Should().Be(StackItem.Null);

            byte[] data1 = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            engine.CurrentContext.EvaluationStack.Push(data1);
            InteropService.Invoke(engine, InteropService.System_Blockchain_GetBlock).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeFalse();

            byte[] data2 = new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01 };
            engine.CurrentContext.EvaluationStack.Push(data2);
            InteropService.Invoke(engine, InteropService.System_Blockchain_GetBlock).Should().BeFalse();
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
            InteropService.Invoke(engine, InteropService.System_Blockchain_GetTransaction).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeFalse();
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
            InteropService.Invoke(engine, InteropService.System_Blockchain_GetTransactionHeight).Should().BeTrue();
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
            InteropService.Invoke(engine, InteropService.System_Blockchain_GetContract).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().Should().Be(StackItem.Null);

            var mockSnapshot = new Mock<Snapshot>();
            var state = TestUtils.GetContract();
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });
            engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
            InteropService.Invoke(engine, InteropService.System_Blockchain_GetContract).Should().BeTrue();
            var stackItems = ((VM.Types.Array)engine.CurrentContext.EvaluationStack.Pop()).ToArray();
            stackItems.Length.Should().Be(3);
            stackItems[0].GetType().Should().Be(typeof(ByteArray));
            stackItems[0].GetByteArray().ToHexString().Should().Be(state.Script.ToHexString());
            stackItems[1].GetBoolean().Should().BeFalse();
            stackItems[2].GetBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void TestStorage_GetContext()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.System_Storage_GetContext).Should().BeTrue();
            var ret = (InteropInterface<StorageContext>)engine.CurrentContext.EvaluationStack.Pop();
            ret.GetInterface<StorageContext>().ScriptHash.Should().Be(engine.CurrentScriptHash);
            ret.GetInterface<StorageContext>().IsReadOnly.Should().BeFalse();
        }

        [TestMethod]
        public void TestStorage_GetReadOnlyContext()
        {
            var engine = GetEngine();
            InteropService.Invoke(engine, InteropService.System_Storage_GetReadOnlyContext).Should().BeTrue();
            var ret = (InteropInterface<StorageContext>)engine.CurrentContext.EvaluationStack.Pop();
            ret.GetInterface<StorageContext>().ScriptHash.Should().Be(engine.CurrentScriptHash);
            ret.GetInterface<StorageContext>().IsReadOnly.Should().BeTrue();
        }

        [TestMethod]
        public void TestStorage_Get()
        {
            var mockSnapshot = new Mock<Snapshot>();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;

            var storageKey = new StorageKey
            {
                ScriptHash = state.ScriptHash,
                Key = new byte[] { 0x01 }
            };

            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = true
            };
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>(storageKey, storageItem));
            var engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });

            engine.CurrentContext.EvaluationStack.Push(new byte[] { 0x01 });
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(new StorageContext
            {
                ScriptHash = state.ScriptHash,
                IsReadOnly = false
            }));
            InteropService.Invoke(engine, InteropService.System_Storage_Get).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetByteArray().ToHexString().Should().Be(storageItem.Value.ToHexString());

            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>());
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });
            engine.CurrentContext.EvaluationStack.Push(new byte[] { 0x01 });
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(new StorageContext
            {
                ScriptHash = state.ScriptHash,
                IsReadOnly = false
            }));
            InteropService.Invoke(engine, InteropService.System_Storage_Get).Should().BeFalse();

            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.System_Storage_Get).Should().BeFalse();
        }

        [TestMethod]
        public void TestStorage_Put()
        {
            var engine = GetEngine(false, true);
            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.System_Storage_Put).Should().BeFalse();

            //CheckStorageContext fail
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            var state = TestUtils.GetContract();
            var storageContext = new StorageContext
            {
                ScriptHash = state.ScriptHash,
                IsReadOnly = false
            };
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Put).Should().BeFalse();

            //key.Length > MaxStorageKeySize
            key = new byte[InteropService.MaxStorageKeySize + 1];
            value = new byte[] { 0x02 };
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Put).Should().BeFalse();

            //value.Length > MaxStorageValueSize
            key = new byte[] { 0x01 };
            value = new byte[ushort.MaxValue + 1];
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Put).Should().BeFalse();

            //context.IsReadOnly
            key = new byte[] { 0x01 };
            value = new byte[] { 0x02 };
            storageContext.IsReadOnly = true;
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Put).Should().BeFalse();

            //storage value is constant
            var mockSnapshot = new Mock<Snapshot>();
            state.Manifest.Features = ContractFeatures.HasStorage;

            var storageKey = new StorageKey
            {
                ScriptHash = state.ScriptHash,
                Key = new byte[] { 0x01 }
            };
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = true
            };
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>(storageKey, storageItem));
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });
            key = new byte[] { 0x01 };
            value = new byte[] { 0x02 };
            storageContext.IsReadOnly = false;
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Put).Should().BeFalse();

            //success
            storageItem.IsConstant = false;
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Put).Should().BeTrue();

            //value length == 0
            key = new byte[] { 0x01 };
            value = new byte[0];
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Put).Should().BeTrue();
        }

        [TestMethod]
        public void TestStorage_PutEx()
        {
            var engine = GetEngine(false, true);
            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.System_Storage_PutEx).Should().BeFalse();

            var mockSnapshot = new Mock<Snapshot>();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            var storageKey = new StorageKey
            {
                ScriptHash = new UInt160(TestUtils.GetByteArray(20, 0x42)),
                Key = new byte[] { 0x01 }
            };
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = false
            };
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>(storageKey, storageItem));
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };
            var storageContext = new StorageContext
            {
                ScriptHash = state.ScriptHash,
                IsReadOnly = false
            };
            engine.CurrentContext.EvaluationStack.Push((int)StorageFlags.None);
            engine.CurrentContext.EvaluationStack.Push(value);
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_PutEx).Should().BeTrue();
        }

        [TestMethod]
        public void TestStorage_Delete()
        {
            var engine = GetEngine(false, true);
            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.System_Storage_Delete).Should().BeFalse();


            var mockSnapshot = new Mock<Snapshot>();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            var storageKey = new StorageKey
            {
                ScriptHash = new UInt160(TestUtils.GetByteArray(20, 0x42)),
                Key = new byte[] { 0x01 }
            };
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = false
            };
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>(storageKey, storageItem));
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });
            state.Manifest.Features = ContractFeatures.HasStorage;
            var key = new byte[] { 0x01 };
            var storageContext = new StorageContext
            {
                ScriptHash = state.ScriptHash,
                IsReadOnly = false
            };
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Delete).Should().BeTrue();

            //context is readonly
            storageContext.IsReadOnly = true;
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Delete).Should().BeFalse();

            //CheckStorageContext fail
            storageContext.IsReadOnly = false;
            state.Manifest.Features = ContractFeatures.NoProperty;
            engine.CurrentContext.EvaluationStack.Push(key);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_Storage_Delete).Should().BeFalse();
        }

        [TestMethod]
        public void TestStorageContext_AsReadOnly()
        {
            var engine = GetEngine();
            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.System_StorageContext_AsReadOnly).Should().BeFalse();

            var state = TestUtils.GetContract();
            var storageContext = new StorageContext
            {
                ScriptHash = state.ScriptHash,
                IsReadOnly = false
            };
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(storageContext));
            InteropService.Invoke(engine, InteropService.System_StorageContext_AsReadOnly).Should().BeTrue();
            var ret = (InteropInterface<StorageContext>)engine.CurrentContext.EvaluationStack.Pop();
            ret.GetInterface<StorageContext>().IsReadOnly.Should().Be(true);
        }

        [TestMethod]
        public void TestInvoke()
        {
            var engine = new ApplicationEngine(TriggerType.Verification, null, null, 0);
            InteropService.Invoke(engine, 10000).Should().BeFalse();
            InteropService.Invoke(engine, InteropService.System_StorageContext_AsReadOnly).Should().BeFalse();
        }

        [TestMethod]
        public void TestContract_Call()
        {
            var mockSnapshot = new Mock<Snapshot>();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            byte[] method = Encoding.UTF8.GetBytes("method");
            byte[] args = new byte[0];
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            var engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });

            engine.CurrentContext.EvaluationStack.Push(args);
            engine.CurrentContext.EvaluationStack.Push(method);
            engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
            InteropService.Invoke(engine, InteropService.System_Contract_Call).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetByteArray().ToHexString().Should().Be(method.ToHexString());
            engine.CurrentContext.EvaluationStack.Pop().GetByteArray().ToHexString().Should().Be(args.ToHexString());

            state.Manifest.Permissions[0].Methods = WildCardContainer<string>.Create("a");
            engine.CurrentContext.EvaluationStack.Push(args);
            engine.CurrentContext.EvaluationStack.Push(method);
            engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
            InteropService.Invoke(engine, InteropService.System_Contract_Call).Should().BeFalse();
            state.Manifest.Permissions[0].Methods = WildCardContainer<string>.CreateWildcard();

            engine.CurrentContext.EvaluationStack.Push(args);
            engine.CurrentContext.EvaluationStack.Push(method);
            engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
            InteropService.Invoke(engine, InteropService.System_Contract_Call).Should().BeTrue();

            engine.CurrentContext.EvaluationStack.Push(args);
            engine.CurrentContext.EvaluationStack.Push(method);
            engine.CurrentContext.EvaluationStack.Push(UInt160.Zero.ToArray());
            InteropService.Invoke(engine, InteropService.System_Contract_Call).Should().BeFalse();
        }

        [TestMethod]
        public void TestContract_Destroy()
        {
            var engine = GetEngine(false, true);
            InteropService.Invoke(engine, InteropService.System_Contract_Destroy).Should().BeTrue();

            var mockSnapshot = new Mock<Snapshot>();
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
                ScriptHash = scriptHash,
                Key = new byte[] { 0x01 }
            };
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(scriptHash, state));
            mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>(storageKey, storageItem));
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[0]);
            InteropService.Invoke(engine, InteropService.System_Contract_Destroy).Should().BeTrue();

            //storages are removed
            mockSnapshot = new Mock<Snapshot>();
            state = TestUtils.GetContract();
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(scriptHash, state));
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[0]);
            InteropService.Invoke(engine, InteropService.System_Contract_Destroy).Should().BeTrue();
        }

        public static void LogEvent(object sender, LogEventArgs args)
        {
            Transaction tx = (Transaction)args.ScriptContainer;
            tx.Script = new byte[] { 0x01, 0x02, 0x03 };
        }

        private static ApplicationEngine GetEngine(bool hasContainer = false, bool hasSnapshot = false)
        {
            var tx = TestUtils.GetTransaction();
            var snapshot = TestBlockchain.GetStore().GetSnapshot().Clone();
            ApplicationEngine engine;
            if (hasContainer && hasSnapshot)
            {
                engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, 0);
            }
            else if (hasContainer && !hasSnapshot)
            {
                engine = new ApplicationEngine(TriggerType.Application, tx, null, 0);
            }
            else if (!hasContainer && hasSnapshot)
            {
                engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            }
            else
            {
                engine = new ApplicationEngine(TriggerType.Application, null, null, 0);
            }
            engine.LoadScript(new byte[] { 0x01 });
            return engine;
        }
    }

    internal class TestInteropInterface : InteropInterface
    {
        public override bool Equals(StackItem other) => true;
        public override bool GetBoolean() => true;
        public override T GetInterface<T>() => throw new NotImplementedException();
    }
}
