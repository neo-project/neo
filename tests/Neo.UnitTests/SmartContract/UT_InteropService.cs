using System;
using System.Linq;
using System.Text;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public partial class UT_InteropService : TestKit
    {
        [TestMethod]
        public void Runtime_GetNotifications_Test()
        {
            UInt160 scriptHash2;
            var snapshot = TestBlockchain.GetTestSnapshot();

            using (var script = new ScriptBuilder())
            {
                // Notify method

                script.Emit(OpCode.SWAP, OpCode.NEWARRAY, OpCode.SWAP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_Notify);

                // Add return

                script.EmitPush(true);
                script.Emit(OpCode.RET);

                // Mock contract

                scriptHash2 = script.ToArray().ToScriptHash();

                snapshot.DeleteContract(scriptHash2);
                ContractState contract = TestUtils.GetContract(script.ToArray(), TestUtils.CreateManifest("test", ContractParameterType.Any, ContractParameterType.Integer, ContractParameterType.Integer));
                contract.Manifest.Abi.Events = new[]
                {
                    new ContractEventDescriptor
                    {
                        Name = "testEvent2",
                        Parameters = new[]
                        {
                            new ContractParameterDefinition
                            {
                                Type = ContractParameterType.Any
                            }
                        }
                    }
                };
                snapshot.AddContract(scriptHash2, contract);
            }

            // Wrong length

            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot))
            using (var script = new ScriptBuilder())
            {
                // Retrive

                script.EmitPush(1);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GetNotifications);

                // Execute

                engine.LoadScript(script.ToArray());

                Assert.AreEqual(VMState.FAULT, engine.Execute());
            }

            // All test

            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot))
            using (var script = new ScriptBuilder())
            {
                // Notification

                script.EmitPush(0);
                script.Emit(OpCode.NEWARRAY);
                script.EmitPush("testEvent1");
                script.EmitSysCall(ApplicationEngine.System_Runtime_Notify);

                // Call script

                script.EmitDynamicCall(scriptHash2, "test", "testEvent2", 1);

                // Drop return

                script.Emit(OpCode.DROP);

                // Receive all notifications

                script.Emit(OpCode.PUSHNULL);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GetNotifications);

                // Execute

                engine.LoadScript(script.ToArray());
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
                                    Name = "testEvent1",
                                    Parameters = System.Array.Empty<ContractParameterDefinition>()
                                }
                            }
                        }
                    }
                };
                var currentScriptHash = engine.EntryScriptHash;

                Assert.AreEqual(VMState.HALT, engine.Execute());
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.AreEqual(2, engine.Notifications.Count);

                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(VM.Types.Array));

                var array = (VM.Types.Array)engine.ResultStack.Pop();

                // Check syscall result

                AssertNotification(array[1], scriptHash2, "testEvent2");
                AssertNotification(array[0], currentScriptHash, "testEvent1");

                // Check notifications

                Assert.AreEqual(scriptHash2, engine.Notifications[1].ScriptHash);
                Assert.AreEqual("testEvent2", engine.Notifications[1].EventName);

                Assert.AreEqual(currentScriptHash, engine.Notifications[0].ScriptHash);
                Assert.AreEqual("testEvent1", engine.Notifications[0].EventName);
            }

            // Script notifications

            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot))
            using (var script = new ScriptBuilder())
            {
                // Notification

                script.EmitPush(0);
                script.Emit(OpCode.NEWARRAY);
                script.EmitPush("testEvent1");
                script.EmitSysCall(ApplicationEngine.System_Runtime_Notify);

                // Call script

                script.EmitDynamicCall(scriptHash2, "test", "testEvent2", 1);

                // Drop return

                script.Emit(OpCode.DROP);

                // Receive all notifications

                script.EmitPush(scriptHash2.ToArray());
                script.EmitSysCall(ApplicationEngine.System_Runtime_GetNotifications);

                // Execute

                engine.LoadScript(script.ToArray());
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
                                    Name = "testEvent1",
                                    Parameters = System.Array.Empty<ContractParameterDefinition>()
                                }
                            }
                        }
                    }
                };
                var currentScriptHash = engine.EntryScriptHash;

                Assert.AreEqual(VMState.HALT, engine.Execute());
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.AreEqual(2, engine.Notifications.Count);

                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(VM.Types.Array));

                var array = (VM.Types.Array)engine.ResultStack.Pop();

                // Check syscall result

                AssertNotification(array[0], scriptHash2, "testEvent2");

                // Check notifications

                Assert.AreEqual(scriptHash2, engine.Notifications[1].ScriptHash);
                Assert.AreEqual("testEvent2", engine.Notifications[1].EventName);

                Assert.AreEqual(currentScriptHash, engine.Notifications[0].ScriptHash);
                Assert.AreEqual("testEvent1", engine.Notifications[0].EventName);
            }

            // Clean storage

            snapshot.DeleteContract(scriptHash2);
        }

        private static void AssertNotification(StackItem stackItem, UInt160 scriptHash, string notification)
        {
            Assert.IsInstanceOfType(stackItem, typeof(VM.Types.Array));

            var array = (VM.Types.Array)stackItem;
            Assert.AreEqual(3, array.Count);
            CollectionAssert.AreEqual(scriptHash.ToArray(), array[0].GetSpan().ToArray());
            Assert.AreEqual(notification, array[1].GetString());
        }

        [TestMethod]
        public void TestExecutionEngine_GetScriptContainer()
        {
            GetEngine(true).GetScriptContainer().Should().BeOfType<VM.Types.Array>();
        }

        [TestMethod]
        public void TestExecutionEngine_GetCallingScriptHash()
        {
            // Test without

            var engine = GetEngine(true);
            engine.CallingScriptHash.Should().BeNull();

            // Test real

            using ScriptBuilder scriptA = new();
            scriptA.Emit(OpCode.DROP); // Drop arguments
            scriptA.Emit(OpCode.DROP); // Drop method
            scriptA.EmitSysCall(ApplicationEngine.System_Runtime_GetCallingScriptHash);

            var contract = TestUtils.GetContract(scriptA.ToArray(), TestUtils.CreateManifest("test", ContractParameterType.Any, ContractParameterType.String, ContractParameterType.Integer));
            engine = GetEngine(true, true, addScript: false);
            engine.Snapshot.AddContract(contract.Hash, contract);

            using ScriptBuilder scriptB = new();
            scriptB.EmitDynamicCall(contract.Hash, "test", "0", 1);
            engine.LoadScript(scriptB.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());

            engine.ResultStack.Pop().GetSpan().ToHexString().Should().Be(scriptB.ToArray().ToScriptHash().ToArray().ToHexString());
        }

        [TestMethod]
        public void TestContract_GetCallFlags()
        {
            GetEngine().GetCallFlags().Should().Be(CallFlags.All);
        }

        [TestMethod]
        public void TestRuntime_Platform()
        {
            ApplicationEngine.GetPlatform().Should().Be("NEO");
        }

        [TestMethod]
        public void TestRuntime_CheckWitness()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new(privateKey);
            ECPoint pubkey = keyPair.PublicKey;

            var engine = GetEngine(true);
            ((Transaction)engine.ScriptContainer).Signers[0].Account = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
            ((Transaction)engine.ScriptContainer).Signers[0].Scopes = WitnessScope.CalledByEntry;

            engine.CheckWitness(pubkey.EncodePoint(true)).Should().BeTrue();
            engine.CheckWitness(((Transaction)engine.ScriptContainer).Sender.ToArray()).Should().BeTrue();

            ((Transaction)engine.ScriptContainer).Signers = System.Array.Empty<Signer>();
            engine.CheckWitness(pubkey.EncodePoint(true)).Should().BeFalse();

            Action action = () => engine.CheckWitness(System.Array.Empty<byte>());
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestRuntime_CheckWitness_Null_ScriptContainer()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new(privateKey);
            ECPoint pubkey = keyPair.PublicKey;

            var engine = GetEngine();

            engine.CheckWitness(pubkey.EncodePoint(true)).Should().BeFalse();
        }

        [TestMethod]
        public void TestRuntime_Log()
        {
            var engine = GetEngine(true);
            string message = "hello";
            ApplicationEngine.Log += LogEvent;
            engine.RuntimeLog(Encoding.UTF8.GetBytes(message));
            ((Transaction)engine.ScriptContainer).Script.Span.ToHexString().Should().Be(new byte[] { 0x01, 0x02, 0x03 }.ToHexString());
            ApplicationEngine.Log -= LogEvent;
        }

        [TestMethod]
        public void TestRuntime_GetTime()
        {
            Block block = new() { Header = new Header() };
            var engine = GetEngine(true, true, hasBlock: true);
            engine.GetTime().Should().Be(block.Timestamp);
        }

        [TestMethod]
        public void TestRuntime_GetInvocationCounter()
        {
            var engine = GetEngine();
            Assert.AreEqual(1, engine.GetInvocationCounter());
        }

        [TestMethod]
        public void TestRuntime_GetCurrentSigners()
        {
            using var engine = GetEngine(hasContainer: true);
            Assert.AreEqual(UInt160.Zero, engine.GetCurrentSigners()[0].Account);
        }

        [TestMethod]
        public void TestRuntime_GetCurrentSigners_SysCall()
        {
            using ScriptBuilder script = new();
            script.EmitSysCall(ApplicationEngine.System_Runtime_CurrentSigners.Hash);

            // Null

            using var engineA = GetEngine(hasSnapshot: true, addScript: false, hasContainer: false);

            engineA.LoadScript(script.ToArray());
            engineA.Execute();
            Assert.AreEqual(engineA.State, VMState.HALT);

            var result = engineA.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Null));

            // Not null

            using var engineB = GetEngine(hasSnapshot: true, addScript: false, hasContainer: true);

            engineB.LoadScript(script.ToArray());
            engineB.Execute();
            Assert.AreEqual(engineB.State, VMState.HALT);

            result = engineB.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));
            (result as VM.Types.Array).Count.Should().Be(1);
            result = (result as VM.Types.Array)[0];
            result.Should().BeOfType(typeof(VM.Types.Array));
            (result as VM.Types.Array).Count.Should().Be(5);
            result = (result as VM.Types.Array)[0]; // Address
            Assert.AreEqual(UInt160.Zero, new UInt160(result.GetSpan()));
        }

        [TestMethod]
        public void TestCrypto_Verify()
        {
            var engine = GetEngine(true);
            IVerifiable iv = engine.ScriptContainer;
            byte[] message = iv.GetSignData(TestProtocolSettings.Default.Network);
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new(privateKey);
            ECPoint pubkey = keyPair.PublicKey;
            byte[] signature = Crypto.Sign(message, privateKey, pubkey.EncodePoint(false).Skip(1).ToArray());
            engine.CheckSig(pubkey.EncodePoint(false), signature).Should().BeTrue();

            byte[] wrongkey = pubkey.EncodePoint(false);
            wrongkey[0] = 5;
            Assert.ThrowsException<FormatException>(() => engine.CheckSig(wrongkey, signature));
        }

        [TestMethod]
        public void TestBlockchain_GetHeight()
        {
            var engine = GetEngine(true, true);
            NativeContract.Ledger.CurrentIndex(engine.Snapshot).Should().Be(0);
        }

        [TestMethod]
        public void TestBlockchain_GetBlock()
        {
            var engine = GetEngine(true, true);

            NativeContract.Ledger.GetBlock(engine.Snapshot, UInt256.Zero).Should().BeNull();

            byte[] data1 = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            NativeContract.Ledger.GetBlock(engine.Snapshot, new UInt256(data1)).Should().BeNull();
            NativeContract.Ledger.GetBlock(engine.Snapshot, TestBlockchain.TheNeoSystem.GenesisBlock.Hash).Should().NotBeNull();
        }

        [TestMethod]
        public void TestBlockchain_GetTransaction()
        {
            var engine = GetEngine(true, true);
            byte[] data1 = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            NativeContract.Ledger.GetTransaction(engine.Snapshot, new UInt256(data1)).Should().BeNull();
        }

        [TestMethod]
        public void TestBlockchain_GetTransactionHeight()
        {
            var engine = GetEngine(hasSnapshot: true, addScript: false);
            var state = new TransactionState()
            {
                BlockIndex = 0,
                Transaction = TestUtils.CreateRandomHashTransaction()
            };
            UT_SmartContractHelper.TransactionAdd(engine.Snapshot, state);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Ledger.Hash, "getTransactionHeight", state.Transaction.Hash);
            engine.LoadScript(script.ToArray());
            engine.Execute();
            Assert.AreEqual(engine.State, VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            result.GetInteger().Should().Be(0);
        }

        [TestMethod]
        public void TestBlockchain_GetContract()
        {
            var engine = GetEngine(true, true);
            byte[] data1 = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01,
                                        0x01, 0x01, 0x01, 0x01, 0x01 };
            NativeContract.ContractManagement.GetContract(engine.Snapshot, new UInt160(data1)).Should().BeNull();

            var snapshot = TestBlockchain.GetTestSnapshot();
            var state = TestUtils.GetContract();
            snapshot.AddContract(state.Hash, state);
            engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(new byte[] { 0x01 });
            NativeContract.ContractManagement.GetContract(engine.Snapshot, state.Hash).Hash.Should().Be(state.Hash);
        }

        [TestMethod]
        public void TestStorage_GetContext()
        {
            var engine = GetEngine(false, true);
            var state = TestUtils.GetContract();
            engine.Snapshot.AddContract(state.Hash, state);
            engine.LoadScript(state.Script);
            engine.GetStorageContext().IsReadOnly.Should().BeFalse();
        }

        [TestMethod]
        public void TestStorage_GetReadOnlyContext()
        {
            var engine = GetEngine(false, true);
            var state = TestUtils.GetContract();
            engine.Snapshot.AddContract(state.Hash, state);
            engine.LoadScript(state.Script);
            engine.GetReadOnlyContext().IsReadOnly.Should().BeTrue();
        }

        [TestMethod]
        public void TestStorage_Get()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var state = TestUtils.GetContract();

            var storageKey = new StorageKey
            {
                Id = state.Id,
                Key = new byte[] { 0x01 }
            };

            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 }
            };
            snapshot.AddContract(state.Hash, state);
            snapshot.Add(storageKey, storageItem);
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(new byte[] { 0x01 });

            engine.Get(new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            }, new byte[] { 0x01 }).Value.Span.ToHexString().Should().Be(storageItem.Value.Span.ToHexString());
        }

        [TestMethod]
        public void TestStorage_Put()
        {
            var engine = GetEngine(false, true);

            //CheckStorageContext fail
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };
            var state = TestUtils.GetContract();
            var storageContext = new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            };
            engine.Put(storageContext, key, value);

            //key.Length > MaxStorageKeySize
            key = new byte[ApplicationEngine.MaxStorageKeySize + 1];
            value = new byte[] { 0x02 };
            Assert.ThrowsException<ArgumentException>(() => engine.Put(storageContext, key, value));

            //value.Length > MaxStorageValueSize
            key = new byte[] { 0x01 };
            value = new byte[ushort.MaxValue + 1];
            Assert.ThrowsException<ArgumentException>(() => engine.Put(storageContext, key, value));

            //context.IsReadOnly
            key = new byte[] { 0x01 };
            value = new byte[] { 0x02 };
            storageContext.IsReadOnly = true;
            Assert.ThrowsException<ArgumentException>(() => engine.Put(storageContext, key, value));

            //storage value is constant
            var snapshot = TestBlockchain.GetTestSnapshot();

            var storageKey = new StorageKey
            {
                Id = state.Id,
                Key = new byte[] { 0x01 }
            };
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 }
            };
            snapshot.AddContract(state.Hash, state);
            snapshot.Add(storageKey, storageItem);
            engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(new byte[] { 0x01 });
            key = new byte[] { 0x01 };
            value = new byte[] { 0x02 };
            storageContext.IsReadOnly = false;
            engine.Put(storageContext, key, value);

            //value length == 0
            key = new byte[] { 0x01 };
            value = System.Array.Empty<byte>();
            engine.Put(storageContext, key, value);
        }

        [TestMethod]
        public void TestStorage_Delete()
        {
            var engine = GetEngine(false, true);
            var snapshot = TestBlockchain.GetTestSnapshot();
            var state = TestUtils.GetContract();
            var storageKey = new StorageKey
            {
                Id = 0x42000000,
                Key = new byte[] { 0x01 }
            };
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 }
            };
            snapshot.AddContract(state.Hash, state);
            snapshot.Add(storageKey, storageItem);
            engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(new byte[] { 0x01 });
            var key = new byte[] { 0x01 };
            var storageContext = new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            };
            engine.Delete(storageContext, key);

            //context is readonly
            storageContext.IsReadOnly = true;
            Assert.ThrowsException<ArgumentException>(() => engine.Delete(storageContext, key));
        }

        [TestMethod]
        public void TestStorageContext_AsReadOnly()
        {
            var state = TestUtils.GetContract();
            var storageContext = new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            };
            ApplicationEngine.AsReadOnly(storageContext).IsReadOnly.Should().BeTrue();
        }

        [TestMethod]
        public void TestContract_Call()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            string method = "method";
            var args = new VM.Types.Array { 0, 1 };
            var state = TestUtils.GetContract(method, args.Count);

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(new byte[] { 0x01 });
            engine.Snapshot.AddContract(state.Hash, state);

            engine.CallContract(state.Hash, method, CallFlags.All, args);
            engine.CurrentContext.EvaluationStack.Pop().Should().Be(args[0]);
            engine.CurrentContext.EvaluationStack.Pop().Should().Be(args[1]);

            state.Manifest.Permissions[0].Methods = WildcardContainer<string>.Create("a");
            engine.Snapshot.DeleteContract(state.Hash);
            engine.Snapshot.AddContract(state.Hash, state);
            Assert.ThrowsException<InvalidOperationException>(() => engine.CallContract(state.Hash, method, CallFlags.All, args));

            state.Manifest.Permissions[0].Methods = WildcardContainer<string>.CreateWildcard();
            engine.Snapshot.DeleteContract(state.Hash);
            engine.Snapshot.AddContract(state.Hash, state);
            engine.CallContract(state.Hash, method, CallFlags.All, args);

            engine.Snapshot.DeleteContract(state.Hash);
            engine.Snapshot.AddContract(state.Hash, state);
            Assert.ThrowsException<InvalidOperationException>(() => engine.CallContract(UInt160.Zero, method, CallFlags.All, args));
        }

        [TestMethod]
        public void TestContract_Destroy()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var state = TestUtils.GetContract();
            var scriptHash = UInt160.Parse("0xcb9f3b7c6fb1cf2c13a40637c189bdd066a272b4");
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 }
            };

            var storageKey = new StorageKey
            {
                Id = 0x43000000,
                Key = new byte[] { 0x01 }
            };
            snapshot.AddContract(scriptHash, state);
            snapshot.Add(storageKey, storageItem);
            snapshot.DestroyContract(scriptHash);
            snapshot.Find(BitConverter.GetBytes(0x43000000)).Any().Should().BeFalse();

            //storages are removed
            state = TestUtils.GetContract();
            snapshot.AddContract(scriptHash, state);
            snapshot.DestroyContract(scriptHash);
            snapshot.Find(BitConverter.GetBytes(0x43000000)).Any().Should().BeFalse();
        }

        [TestMethod]
        public void TestContract_CreateStandardAccount()
        {
            ECPoint pubkey = ECPoint.Parse("024b817ef37f2fc3d4a33fe36687e592d9f30fe24b3e28187dc8f12b3b3b2b839e", ECCurve.Secp256r1);
            GetEngine().CreateStandardAccount(pubkey).ToArray().ToHexString().Should().Be("c44ea575c5f79638f0e73f39d7bd4b3337c81691");
        }

        public static void LogEvent(object sender, LogEventArgs args)
        {
            Transaction tx = (Transaction)args.ScriptContainer;
            tx.Script = new byte[] { 0x01, 0x02, 0x03 };
        }

        private static ApplicationEngine GetEngine(bool hasContainer = false, bool hasSnapshot = false, bool hasBlock = false, bool addScript = true, long gas = 20_00000000)
        {
            var tx = hasContainer ? TestUtils.GetTransaction(UInt160.Zero) : null;
            var snapshot = hasSnapshot ? TestBlockchain.GetTestSnapshot() : null;
            var block = hasBlock ? new Block { Header = new Header() } : null;
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, block, TestBlockchain.TheNeoSystem.Settings, gas: gas);
            if (addScript) engine.LoadScript(new byte[] { 0x01 });
            return engine;
        }
    }
}
