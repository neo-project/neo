using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SDK.TX;
using Neo.SmartContract.Native;
using System.Threading;

namespace Neo.UnitTests.SDK.TX
{
    [TestClass]
    public class UT_TxManager
    {
        TxManager txManager;
        Mock<RpcClient> rpcClientMock;
        Mock<UInt160> senderMock;

        [TestInitialize]
        public void TestSetup()
        {
            rpcClientMock = new Mock<RpcClient>(MockBehavior.Strict, "http://seed1.neo.org:10331");
            senderMock = new Mock<UInt160>(MockBehavior.Strict);
            MockHashCode();
            MockInvoke1();
            txManager = new TxManager(rpcClientMock.Object, senderMock.Object);
        }

        private void MockHashCode()
        {
            senderMock
                .Setup(p => p.GetHashCode())
                .Returns(0)
                .Verifiable();
        }

        private void MockHeight()
        {
            rpcClientMock
               .Setup(p => p.GetBlockCount())
               .Returns(100)
               .Verifiable();
        }

        private void MockInvoke1()
        {
            string script = "14000000000000000000000000000000000000000051c10962616c616e63654f66142582d1b275e86c8f0e93a9b2facd5fdb760976a168627d5b52";
            RpcInvokeResult result = new RpcInvokeResult()
            {
                Stack = new RpcStack[1]
                {
                    new RpcStack()
                    {
                        Type = "Integer",
                        Value = "10000000000000000"
                    }
                }
            };

            rpcClientMock
                .Setup(p => p.InvokeScript(script))
                .Returns(result)
                .Verifiable();
        }

        private void MockInvoke2()
        {
            string script = "00";
            RpcInvokeResult result = new RpcInvokeResult()
            {
                GasConsumed = "100"
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
            MockInvoke2();

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

        [TestMethod]
        public void TestMakeScript()
        {
            byte[] testScript = txManager.MakeScript(NativeContract.GAS.Hash, "balanceOf", senderMock.Object);

            Assert.AreEqual("14000000000000000000000000000000000000000051c10962616c616e63654f66142582d1b275e86c8f0e93a9b2facd5fdb760976a168627d5b52",
                            testScript.ToHexString());
        }
    }
}
