// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Contract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Contract
    {
        [TestMethod]
        public void TestGetScriptHash()
        {
            var privateKey = new byte[32];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            var key = new KeyPair(privateKey);
            var contract = Contract.CreateSignatureContract(key.PublicKey);
            var expectedArray = new byte[40];
            expectedArray[0] = (byte)OpCode.PUSHDATA1;
            expectedArray[1] = 0x21;
            Array.Copy(key.PublicKey.EncodePoint(true), 0, expectedArray, 2, 33);
            expectedArray[35] = (byte)OpCode.SYSCALL;
            Array.Copy(BitConverter.GetBytes(ApplicationEngine.System_Crypto_CheckSig), 0, expectedArray, 36, 4);
            Assert.AreEqual(expectedArray.ToScriptHash(), contract.ScriptHash);
        }

        [TestMethod]
        public void TestCreate()
        {
            var script = new byte[32];
            var parameterList = new ContractParameterType[] { ContractParameterType.Signature };
            var contract = Contract.Create(parameterList, script);
            Assert.AreEqual(contract.Script, script);
            Assert.AreEqual(1, contract.ParameterList.Length);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[0]);
        }

        [TestMethod]
        public void TestCreateMultiSigContract()
        {
            var privateKey1 = new byte[32];
            var rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            var key1 = new KeyPair(privateKey1);
            var privateKey2 = new byte[32];
            var rng2 = RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            var key2 = new KeyPair(privateKey2);
            var publicKeys = new Neo.Cryptography.ECC.ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = [.. publicKeys.OrderBy(p => p)];
            var contract = Contract.CreateMultiSigContract(2, publicKeys);
            var expectedArray = new byte[77];
            expectedArray[0] = (byte)OpCode.PUSH2;
            expectedArray[1] = (byte)OpCode.PUSHDATA1;
            expectedArray[2] = 0x21;
            Array.Copy(publicKeys[0].EncodePoint(true), 0, expectedArray, 3, 33);
            expectedArray[36] = (byte)OpCode.PUSHDATA1;
            expectedArray[37] = 0x21;
            Array.Copy(publicKeys[1].EncodePoint(true), 0, expectedArray, 38, 33);
            expectedArray[71] = (byte)OpCode.PUSH2;
            expectedArray[72] = (byte)OpCode.SYSCALL;
            Array.Copy(BitConverter.GetBytes(ApplicationEngine.System_Crypto_CheckMultisig), 0, expectedArray, 73, 4);
            CollectionAssert.AreEqual(expectedArray, contract.Script);
            Assert.AreEqual(2, contract.ParameterList.Length);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[0]);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[1]);
        }

        [TestMethod]
        public void TestCreateMultiSigRedeemScript()
        {
            var privateKey1 = new byte[32];
            var rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            var key1 = new KeyPair(privateKey1);
            var privateKey2 = new byte[32];
            var rng2 = RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            var key2 = new KeyPair(privateKey2);
            var publicKeys = new Neo.Cryptography.ECC.ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = [.. publicKeys.OrderBy(p => p)];
            Action action = () => Contract.CreateMultiSigRedeemScript(0, publicKeys);
            action.Should().Throw<ArgumentException>();
            var script = Contract.CreateMultiSigRedeemScript(2, publicKeys);
            var expectedArray = new byte[77];
            expectedArray[0] = (byte)OpCode.PUSH2;
            expectedArray[1] = (byte)OpCode.PUSHDATA1;
            expectedArray[2] = 0x21;
            Array.Copy(publicKeys[0].EncodePoint(true), 0, expectedArray, 3, 33);
            expectedArray[36] = (byte)OpCode.PUSHDATA1;
            expectedArray[37] = 0x21;
            Array.Copy(publicKeys[1].EncodePoint(true), 0, expectedArray, 38, 33);
            expectedArray[71] = (byte)OpCode.PUSH2;
            expectedArray[72] = (byte)OpCode.SYSCALL;
            Array.Copy(BitConverter.GetBytes(ApplicationEngine.System_Crypto_CheckMultisig), 0, expectedArray, 73, 4);
            CollectionAssert.AreEqual(expectedArray, script);
        }

        [TestMethod]
        public void TestCreateSignatureContract()
        {
            var privateKey = new byte[32];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            var key = new KeyPair(privateKey);
            var contract = Contract.CreateSignatureContract(key.PublicKey);
            var expectedArray = new byte[40];
            expectedArray[0] = (byte)OpCode.PUSHDATA1;
            expectedArray[1] = 0x21;
            Array.Copy(key.PublicKey.EncodePoint(true), 0, expectedArray, 2, 33);
            expectedArray[35] = (byte)OpCode.SYSCALL;
            Array.Copy(BitConverter.GetBytes(ApplicationEngine.System_Crypto_CheckSig), 0, expectedArray, 36, 4);
            CollectionAssert.AreEqual(expectedArray, contract.Script);
            Assert.AreEqual(1, contract.ParameterList.Length);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[0]);
        }

        [TestMethod]
        public void TestCreateSignatureRedeemScript()
        {
            var privateKey = new byte[32];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            var key = new KeyPair(privateKey);
            var script = Contract.CreateSignatureRedeemScript(key.PublicKey);
            var expectedArray = new byte[40];
            expectedArray[0] = (byte)OpCode.PUSHDATA1;
            expectedArray[1] = 0x21;
            Array.Copy(key.PublicKey.EncodePoint(true), 0, expectedArray, 2, 33);
            expectedArray[35] = (byte)OpCode.SYSCALL;
            Array.Copy(BitConverter.GetBytes(ApplicationEngine.System_Crypto_CheckSig), 0, expectedArray, 36, 4);
            CollectionAssert.AreEqual(expectedArray, script);
        }

        [TestMethod]
        public void TestSignatureRedeemScriptFee()
        {
            var privateKey = new byte[32];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            var key = new KeyPair(privateKey);
            var verification = Contract.CreateSignatureRedeemScript(key.PublicKey);
            var invocation = new ScriptBuilder().EmitPush(UInt160.Zero).ToArray();

            var fee = PolicyContract.DefaultExecFeeFactor * (ApplicationEngine.OpCodePriceTable[(byte)OpCode.PUSHDATA1] * 2 + ApplicationEngine.OpCodePriceTable[(byte)OpCode.SYSCALL] + ApplicationEngine.CheckSigPrice);

            using (var engine = ApplicationEngine.Create(TriggerType.Verification, new Transaction { Signers = [], Attributes = [] }, null, settings: TestBlockchain.TheNeoSystem.Settings))
            {
                engine.LoadScript(invocation.Concat(verification).ToArray(), configureState: p => p.CallFlags = CallFlags.None);
                engine.Execute();
                engine.GasConsumed.Should().Be(fee);
            }
        }

        [TestMethod]
        public void TestCreateMultiSigRedeemScriptFee()
        {
            var privateKey1 = new byte[32];
            var rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            var key1 = new KeyPair(privateKey1);
            var privateKey2 = new byte[32];
            var rng2 = RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            var key2 = new KeyPair(privateKey2);
            var publicKeys = new Neo.Cryptography.ECC.ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = [.. publicKeys.OrderBy(p => p)];
            var verification = Contract.CreateMultiSigRedeemScript(2, publicKeys);
            var invocation = new ScriptBuilder().EmitPush(UInt160.Zero).EmitPush(UInt160.Zero).ToArray();

            var fee = PolicyContract.DefaultExecFeeFactor * (ApplicationEngine.OpCodePriceTable[(byte)OpCode.PUSHDATA1] * (2 + 2) + ApplicationEngine.OpCodePriceTable[(byte)OpCode.PUSHINT8] * 2 + ApplicationEngine.OpCodePriceTable[(byte)OpCode.SYSCALL] + ApplicationEngine.CheckSigPrice * 2);

            using (var engine = ApplicationEngine.Create(TriggerType.Verification, new Transaction { Signers = [], Attributes = [] }, null, settings: TestBlockchain.TheNeoSystem.Settings))
            {
                engine.LoadScript(invocation.Concat(verification).ToArray(), configureState: p => p.CallFlags = CallFlags.None);
                engine.Execute();
                engine.GasConsumed.Should().Be(fee);
            }
        }
    }
}
