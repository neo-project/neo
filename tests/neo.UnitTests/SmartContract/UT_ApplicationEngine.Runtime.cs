using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Numerics;
using Neo.Cryptography.ECC;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
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

            rand_1.Should().Be(BigInteger.Parse("271339657438512451304577787170704246350"));
            rand_2.Should().Be(BigInteger.Parse("3519468259280385525954723453894821326"));
            rand_3.Should().Be(BigInteger.Parse("109167038153789065876532298231776118857"));
            rand_4.Should().Be(BigInteger.Parse("278188388582393629262399165075733096984"));
            rand_5.Should().Be(BigInteger.Parse("252973537848551880583287107760169066816"));

            rand_1.Should().Be(rand_6);
            rand_2.Should().Be(rand_7);
            rand_3.Should().Be(rand_8);
            rand_4.Should().Be(rand_9);
            rand_5.Should().Be(rand_10);
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

            rand_1.Should().Be(BigInteger.Parse("271339657438512451304577787170704246350"));
            rand_2.Should().Be(BigInteger.Parse("3519468259280385525954723453894821326"));
            rand_3.Should().Be(BigInteger.Parse("109167038153789065876532298231776118857"));
            rand_4.Should().Be(BigInteger.Parse("278188388582393629262399165075733096984"));
            rand_5.Should().Be(BigInteger.Parse("252973537848551880583287107760169066816"));

            rand_1.Should().NotBe(rand_6);
            rand_2.Should().NotBe(rand_7);
            rand_3.Should().NotBe(rand_8);
            rand_4.Should().NotBe(rand_9);
            rand_5.Should().NotBe(rand_10);
        }

        /// <summary>
        /// Entry(CheckWitness)
        /// AllowedCallingContracts: [EntryHash]=[...]
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingContracts_Entry_Success()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, null, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_CheckWitness, sender);
            engine.LoadScript(script.ToArray());

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingContracts,
                    AllowedCallingContracts = new Dictionary<UInt160, UInt160[]>()
                    {
                        [script.ToArray().ToScriptHash()] = new UInt160[] { }
                    }
                }
            };
            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            Assert.IsTrue(result.GetBoolean());
        }

        /// <summary>
        /// Entry(CheckWitness)
        /// AllowedCallingContracts: Empty
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingContracts_Entry_Fail()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, null, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_CheckWitness, sender);
            engine.LoadScript(script.ToArray());

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingContracts,
                    AllowedCallingContracts = new Dictionary<UInt160, UInt160[]>()
                }
            };
            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            Assert.IsFalse(result.GetBoolean());
        }

        /// <summary>
        /// Entry=>VerifyContract(CheckWitness)
        /// AllowedCallingContracts: [VerifyContractHash]=[Any]
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingContracts_Entry_Call_Success()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();
            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            engine.LoadScript(BuildEntryCallScript(sender, verifyContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingContracts,
                    AllowedCallingContracts = new Dictionary<UInt160, UInt160[]>()
                    {
                        [verifyContract.Hash] = new UInt160[] { }
                    }
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsTrue(result.GetBoolean());
        }

        /// <summary>
        /// Entry=>VerifyContract(CheckWitness)
        /// AllowedCallingContracts: Empty
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingContracts_Entry_Call_Fail()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();
            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            engine.LoadScript(BuildEntryCallScript(sender, verifyContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingContracts,
                    AllowedCallingContracts = new Dictionary<UInt160, UInt160[]>()
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsFalse(result.GetBoolean());
        }

        /// <summary>
        /// Entry=>Bridge=>Verify(CheckWitness)
        /// AllowedCallingContracts: [VerifyContractHash]=[BridgeContractHash,...]
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingContracts_Bridge_Success()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();

            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            engine.LoadScript(BuildBridgeCallScript(sender, verifyContract, bridgeContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingContracts,
                    AllowedCallingContracts = new Dictionary<UInt160, UInt160[]>()
                    {
                        [verifyContract.Hash] = new UInt160[] { bridgeContract.Hash }
                    }
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsTrue(result.GetBoolean());
        }

        /// <summary>
        /// Entry=>Bridge=>Verify(CheckWitness)
        /// AllowedCallingContracts: [VerifyContractHash]=[...(Except Bridge)]
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingContracts_Bridge_Fail()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();

            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            engine.LoadScript(BuildBridgeCallScript(sender, verifyContract, bridgeContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingContracts,
                    AllowedCallingContracts = new Dictionary<UInt160, UInt160[]>()
                    {
                        [verifyContract.Hash] = new UInt160[] { verifyContract.Hash}
                    }
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsFalse(result.GetBoolean());
        }

        /// <summary>
        /// Entry=>Bridge=>Verify(CheckWitness)
        /// AllowedCallingContracts: empty
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingContracts_Bridge_Fail2()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();

            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            engine.LoadScript(BuildBridgeCallScript(sender, verifyContract, bridgeContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingContracts,
                    AllowedCallingContracts = new Dictionary<UInt160, UInt160[]>()
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsFalse(result.GetBoolean());
        }

        /// <summary>
        /// Entry(CheckWitness)
        /// AllowedCallingGroup: [EntryHash]=[...]
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingGroup_Entry_Success()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, null, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_CheckWitness, sender);
            engine.LoadScript(script.ToArray());

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingGroups,
                    AllowedCallingGroup = new Dictionary<UInt160, ECPoint[]>()
                    {
                        [script.ToArray().ToScriptHash()] = new ECPoint[] { }
                    }
                }
            };
            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            Assert.IsTrue(result.GetBoolean());
        }

        /// <summary>
        /// Entry(CheckWitness)
        /// AllowedCallingGroup: Empty
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingGroup_Entry_Fail()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, null, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_CheckWitness, sender);
            engine.LoadScript(script.ToArray());

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingGroups,
                    AllowedCallingGroup = new Dictionary<UInt160, ECPoint[]>()
                }
            };
            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            Assert.IsFalse(result.GetBoolean());
        }

        /// <summary>
        /// Entry=>VerifyContract(CheckWitness)
        /// AllowedCallingGroup: [VerifyContractHash]=[Any]
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingGroup_Entry_Call_Success()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();
            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            engine.LoadScript(BuildEntryCallScript(sender, verifyContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingGroups,
                    AllowedCallingGroup = new Dictionary<UInt160, ECPoint[]>()
                    {
                        [verifyContract.Hash] = new ECPoint[] { }
                    }
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsTrue(result.GetBoolean());
        }

        /// <summary>
        /// Entry=>VerifyContract(CheckWitness)
        /// AllowedCallingGroup: Empty
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingGroup_Entry_Call_Fail()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();
            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            engine.LoadScript(BuildEntryCallScript(sender, verifyContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingGroups,
                    AllowedCallingGroup = new Dictionary<UInt160, ECPoint[]>()
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsFalse(result.GetBoolean());
        }

        private ECPoint _point = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);
        private ECPoint _point2 = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a700", ECCurve.Secp256r1);

        /// <summary>
        /// Entry=>Bridge=>Verify(CheckWitness)
        /// AllowedCallingGroup: [VerifyContractHash]=[BridgeGroup,...]
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingGroup_Bridge_Success()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();

            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            engine.LoadScript(BuildBridgeCallScript(sender, verifyContract, bridgeContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingGroups,
                    AllowedCallingGroup = new Dictionary<UInt160, ECPoint[]>()
                    {
                        [verifyContract.Hash] = new ECPoint[] { _point }
                    }
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsTrue(result.GetBoolean());
        }

        /// <summary>
        /// Entry=>Bridge=>Verify(CheckWitness)
        /// AllowedCallingGroup: [VerifyContractHash]=[...(Except Bridge)]
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingGroup_Bridge_Fail()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();

            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            engine.LoadScript(BuildBridgeCallScript(sender, verifyContract, bridgeContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingGroups,
                    AllowedCallingGroup = new Dictionary<UInt160, ECPoint[]>()
                    {
                        [verifyContract.Hash] = new ECPoint[] { _point2 }
                    }
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsFalse(result.GetBoolean());
        }

        /// <summary>
        /// Entry=>Bridge=>Verify(CheckWitness)
        /// AllowedCallingGroup: empty
        /// </summary>
        [TestMethod]
        public void TestCheckWitness_CustomCallingGroup_Bridge_Fail2()
        {
            var sender = GerRandomAddress();
            var tx = InitTx();

            var verifyContract = GetVerifyContract();
            var bridgeContract = GetBridgeContract();
            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.AddContract(verifyContract.Hash, verifyContract);
            snapshot.AddContract(bridgeContract.Hash, bridgeContract);
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            engine.LoadScript(BuildBridgeCallScript(sender, verifyContract, bridgeContract));

            tx.Signers = new Signer[]
            {
                new Signer()
                {
                    Account = sender,
                    Scopes = WitnessScope.CustomCallingGroups,
                    AllowedCallingGroup = new Dictionary<UInt160, ECPoint[]>()
                }
            };
            var state = engine.Execute();
            Assert.AreEqual(VMState.HALT, state);

            var result = engine.ResultStack.Pop();
            Assert.IsFalse(result.GetBoolean());
        }

        private UInt160 GerRandomAddress()
        {
            var bytes = new byte[20];
            TestUtils.TestRandom.NextBytes(bytes);
            return new UInt160(bytes);
        }

        private ContractState GetVerifyContract()
        {
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_CheckWitness);
            var contract = new ContractState()
            {
                Manifest = new ContractManifest()
                {
                    Abi = new ContractAbi()
                    {
                        Methods = new[]
                        {
                            new ContractMethodDescriptor()
                            {
                                Name = "verify",
                                Parameters = new ContractParameterDefinition[]
                                {
                                    new() { Name = "signer", Type = ContractParameterType.Hash160 }
                                },
                            }
                        }
                    }
                },
                Nef = new NefFile { Script = script.ToArray() },
                Hash = script.ToArray().ToScriptHash(),
            };
            return contract;
        }

        private ContractState GetBridgeContract()
        {
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);
            var contract = new ContractState()
            {
                Manifest = new ContractManifest()
                {
                    Groups = new ContractGroup[] { new() { PubKey = _point } },
                    Permissions = new[] { ContractPermission.DefaultPermission },
                    Abi = new ContractAbi()
                    {
                        Methods = new[]
                        {
                            new ContractMethodDescriptor()
                            {
                                Name = "call",
                                Parameters = new ContractParameterDefinition[]
                                {
                                    new() { Name = "contract", Type = ContractParameterType.Hash160 },
                                    new() { Name = "method", Type = ContractParameterType.String },
                                    new() { Name = "flag", Type = ContractParameterType.Integer },
                                    new() { Name = "paras", Type = ContractParameterType.Array },
                                },
                            }
                        }
                    }
                },
                Nef = new NefFile { Script = script.ToArray() },
                Hash = script.ToArray().ToScriptHash(),
            };
            return contract;
        }

        private byte[] BuildBridgeCallScript(UInt160 sender, ContractState verifyContract, ContractState bridgeContract)
        {
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_Call, bridgeContract.Hash, "call", CallFlags.All,
                new ContractParameter
                {
                    Type = ContractParameterType.Array,
                    Value = new ContractParameter[]
                    {
                        new ContractParameter() { Type = ContractParameterType.Hash160, Value = verifyContract.Hash },
                        new ContractParameter() { Type = ContractParameterType.String, Value = "verify" },
                        new ContractParameter() { Type = ContractParameterType.Integer, Value = (BigInteger)(byte)CallFlags.All },
                        new ContractParameter
                        {
                            Type = ContractParameterType.Array,
                            Value = new ContractParameter[]
                                { new() { Type = ContractParameterType.Hash160, Value = sender } }
                        }
                    }
                });
            return script.ToArray();
        }

        private byte[] BuildEntryCallScript(UInt160 sender, ContractState verifyContract)
        {
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_Call, verifyContract.Hash, "verify", CallFlags.All,
                new ContractParameter
                {
                    Type = ContractParameterType.Array,
                    Value = new ContractParameter[]
                    {
                        new ContractParameter() { Type = ContractParameterType.Hash160, Value = sender },
                    }
                });
            return script.ToArray();
        }

        private Transaction InitTx()
        {
            return new Transaction
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
        }
    }
}
