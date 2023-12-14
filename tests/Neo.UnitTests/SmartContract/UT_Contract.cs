using System;
using System.Linq;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Contract
    {
        [TestMethod]
        public void TestGetScriptHash()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            Contract contract = Contract.CreateSignatureContract(key.PublicKey);
            byte[] expectedArray = new byte[40];
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
            byte[] script = new byte[32];
            ContractParameterType[] parameterList = new ContractParameterType[] { ContractParameterType.Signature };
            Contract contract = Contract.Create(parameterList, script);
            Assert.AreEqual(contract.Script, script);
            Assert.AreEqual(1, contract.ParameterList.Length);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[0]);
        }

        [TestMethod]
        public void TestCreateMultiSigContract()
        {
            byte[] privateKey1 = new byte[32];
            RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] privateKey2 = new byte[32];
            RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            KeyPair key2 = new KeyPair(privateKey2);
            Neo.Cryptography.ECC.ECPoint[] publicKeys = new Neo.Cryptography.ECC.ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = publicKeys.OrderBy(p => p).ToArray();
            Contract contract = Contract.CreateMultiSigContract(2, publicKeys);
            byte[] expectedArray = new byte[77];
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
            byte[] privateKey1 = new byte[32];
            RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] privateKey2 = new byte[32];
            RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            KeyPair key2 = new KeyPair(privateKey2);
            Neo.Cryptography.ECC.ECPoint[] publicKeys = new Neo.Cryptography.ECC.ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = publicKeys.OrderBy(p => p).ToArray();
            Action action = () => Contract.CreateMultiSigRedeemScript(0, publicKeys);
            action.Should().Throw<ArgumentException>();
            byte[] script = Contract.CreateMultiSigRedeemScript(2, publicKeys);
            byte[] expectedArray = new byte[77];
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
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            Contract contract = Contract.CreateSignatureContract(key.PublicKey);
            byte[] expectedArray = new byte[40];
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
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            byte[] script = Contract.CreateSignatureRedeemScript(key.PublicKey);
            byte[] expectedArray = new byte[40];
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
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            byte[] verification = Contract.CreateSignatureRedeemScript(key.PublicKey);
            byte[] invocation = new ScriptBuilder().EmitPush(UInt160.Zero).ToArray();

            var fee = PolicyContract.DefaultExecFeeFactor * (ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] * 2 + ApplicationEngine.OpCodePrices[OpCode.SYSCALL] + ApplicationEngine.CheckSigPrice);

            using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, new Transaction { Signers = Array.Empty<Signer>(), Attributes = Array.Empty<TransactionAttribute>() }, null, settings: TestBlockchain.TheNeoSystem.Settings))
            {
                engine.LoadScript(invocation.Concat(verification).ToArray(), configureState: p => p.CallFlags = CallFlags.None);
                engine.Execute();
                engine.GasConsumed.Should().Be(fee);
            }
        }

        [TestMethod]
        public void TestCreateMultiSigRedeemScriptFee()
        {
            byte[] privateKey1 = new byte[32];
            RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] privateKey2 = new byte[32];
            RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            KeyPair key2 = new KeyPair(privateKey2);
            Neo.Cryptography.ECC.ECPoint[] publicKeys = new Neo.Cryptography.ECC.ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = publicKeys.OrderBy(p => p).ToArray();
            byte[] verification = Contract.CreateMultiSigRedeemScript(2, publicKeys);
            byte[] invocation = new ScriptBuilder().EmitPush(UInt160.Zero).EmitPush(UInt160.Zero).ToArray();

            long fee = PolicyContract.DefaultExecFeeFactor * (ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] * (2 + 2) + ApplicationEngine.OpCodePrices[OpCode.PUSHINT8] * 2 + ApplicationEngine.OpCodePrices[OpCode.SYSCALL] + ApplicationEngine.CheckSigPrice * 2);

            using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, new Transaction { Signers = Array.Empty<Signer>(), Attributes = Array.Empty<TransactionAttribute>() }, null, settings: TestBlockchain.TheNeoSystem.Settings))
            {
                engine.LoadScript(invocation.Concat(verification).ToArray(), configureState: p => p.CallFlags = CallFlags.None);
                engine.Execute();
                engine.GasConsumed.Should().Be(fee);
            }
        }
    }
}
