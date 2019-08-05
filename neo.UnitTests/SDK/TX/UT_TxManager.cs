using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SDK.TX;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System.Numerics;

namespace Neo.UnitTests.SDK.TX
{
    [TestClass]
    public class UT_TxManager
    {
        TxManager txManager;
        Mock<RpcClient> rpcClientMock;
        readonly UInt160 sender = UInt160.Zero;

        [TestInitialize]
        public void TestSetup()
        {
            rpcClientMock = new Mock<RpcClient>(MockBehavior.Strict, "http://seed1.neo.org:10331");
            txManager = new TxManager(rpcClientMock.Object, sender);
        }

        private void MockHeight()
        {
            rpcClientMock
               .Setup(p => p.GetBlockCount())
               .Returns(100)
               .Verifiable();
        }

        private void MockGasBalance()
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(NativeContract.GAS.Hash, "balanceOf", sender);
                script = sb.ToArray();
            }

            RpcInvokeResult result = new RpcInvokeResult()
            {
                Stack = new[]
                {
                    new ContractParameter()
                    {
                        Type = ContractParameterType.Integer,
                        Value = BigInteger.Parse("10000000000000000")
                    }
                }
            };

            rpcClientMock
                .Setup(p => p.InvokeScript(script))
                .Returns(result)
                .Verifiable();
        }

        private void MockEmptyScript()
        {
            byte[] script = new byte[1];

            RpcInvokeResult result = new RpcInvokeResult()
            {
                GasConsumed = "100"
            };

            rpcClientMock
                .Setup(p => p.InvokeScript(script))
                .Returns(result)
                .Verifiable();
        }

        private void MockFeePerByte()
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(NativeContract.Policy.Hash, "getFeePerByte");
                script = sb.ToArray();
            }

            RpcInvokeResult result = new RpcInvokeResult()
            {
                Stack = new[] {new ContractParameter() {
                      Type = ContractParameterType.Integer,
                      Value = (BigInteger)1000
                 }}
            };

            rpcClientMock
                .Setup(p => p.InvokeScript(script))
                .Returns(result)
                .Verifiable();
        }

        [TestMethod]
        public void TestMakeTransaction()
        {
            MockHeight();
            MockEmptyScript();
            MockFeePerByte();
            MockGasBalance();

            TransactionAttribute[] attributes = new TransactionAttribute[1]
            {
                new TransactionAttribute
                {
                    Usage = TransactionAttributeUsage.Url,
                    Data = "53616d706c6555726c".HexToBytes() // "SampleUrl"
                }
            };

            byte[] script = new byte[1];
            long size = Transaction.HeaderSize + attributes.GetVarSize() + script.GetVarSize() + Neo.IO.Helper.GetVarSize(1);

            Transaction tx = txManager.MakeTransaction(attributes, script);

            Assert.AreEqual("53616d706c6555726c", tx.Attributes[0].Data.ToHexString());
            Assert.AreEqual(0, tx.SystemFee % (long)NativeContract.GAS.Factor);
            Assert.AreEqual(size * 1000, tx.NetworkFee);
        }


    }
}
