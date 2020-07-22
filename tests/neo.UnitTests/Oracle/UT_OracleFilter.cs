using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Oracle;
using System.Text;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OracleFilter
    {
        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestSerialization()
        {
            OracleFilter filter = new OracleFilter()
            {
                ContractHash = UInt160.Parse("0x7ab841144dcdbf228ff57f7068f795e2afd1a3c1"),
                FilterMethod = "MyMethod",
                FilterArgs = "MyArgs"
            };

            var data = filter.ToArray();
            var copy = data.AsSerializable<OracleFilter>();

            Assert.AreEqual(filter.Size, data.Length);

            Assert.AreEqual(filter.ContractHash, copy.ContractHash);
            Assert.AreEqual(filter.FilterMethod, copy.FilterMethod);
            Assert.AreEqual(filter.FilterArgs, copy.FilterArgs);
        }

        [TestMethod]
        public void TestFilter()
        {
            OracleFilter filter = new OracleFilter()
            {
                ContractHash = UInt160.Parse("0x7ab841144dcdbf228ff57f7068f795e2afd1a3c1"),
                FilterMethod = "MyMethod",
                FilterArgs = "MyArgs"
            };

            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();

            // Without contract

            Assert.IsFalse(OracleFilter.Filter(snapshot, filter, new byte[] { }, out var result, out var gas));
            Assert.IsNull(result);
            Assert.AreEqual(1007780, gas);

            // With contract

            snapshot.Contracts.Add(filter.ContractHash, new ContractState()
            {
                Script = new byte[] { (byte)VM.OpCode.DROP, (byte)VM.OpCode.RET },
                Manifest = new Neo.SmartContract.Manifest.ContractManifest()
                {
                    Abi = new Neo.SmartContract.Manifest.ContractAbi()
                    {
                        Hash = filter.ContractHash,
                        Methods = new Neo.SmartContract.Manifest.ContractMethodDescriptor[]
                            {
                                new Neo.SmartContract.Manifest.ContractMethodDescriptor()
                                {
                                     Name = filter.FilterMethod,
                                     Parameters = new Neo.SmartContract.Manifest.ContractParameterDefinition[]
                                     {
                                         new Neo.SmartContract.Manifest.ContractParameterDefinition(){ Name= "method", Type = Neo.SmartContract.ContractParameterType.String},
                                         new Neo.SmartContract.Manifest.ContractParameterDefinition(){ Name= "value", Type = Neo.SmartContract.ContractParameterType.String},
                                     },
                                     ReturnType = Neo.SmartContract.ContractParameterType.ByteArray,
                                     Offset = 0
                                }
                            }
                    }
                }
            });

            Assert.IsTrue(OracleFilter.Filter(snapshot, filter, new byte[] { }, out result, out gas));

            Assert.AreEqual(filter.FilterArgs, Encoding.UTF8.GetString(result));
            Assert.AreEqual(1007840, gas);
        }
    }
}
