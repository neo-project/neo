using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_DeployedContract
    {
        [TestMethod]
        public void TestGetAddress()
        {
            var contract = new DeployedContract(new ContractState()
            {
                Manifest = new Neo.SmartContract.Manifest.ContractManifest(),
                Abi = new Neo.SmartContract.Manifest.ContractAbi()
                {
                    Methods = new Neo.SmartContract.Manifest.ContractMethodDescriptor[]
                         {
                             new Neo.SmartContract.Manifest.ContractMethodDescriptor()
                             {
                                  Name="verify",
                                  Parameters=new Neo.SmartContract.Manifest.ContractParameterDefinition[0]
                             }
                         }
                },
                Script = new byte[] { 1, 2, 3 },
                ScriptHash = new byte[] { 1, 2, 3 }.ToScriptHash()
            });

            Assert.AreEqual("0xb2e3fe334830b4741fa5d762f2ab36b90b86c49b", contract.ScriptHash.ToString());
            Assert.AreEqual("Na7bMBy8KWZKSFBWTxeSKth1Je9AcWTpQM", contract.Address);
        }

        [TestMethod]
        public void TestErrors()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new DeployedContract(null));
            Assert.ThrowsException<NotSupportedException>(() => new DeployedContract(new ContractState()
            {
                Manifest = new Neo.SmartContract.Manifest.ContractManifest(),
                Abi = new Neo.SmartContract.Manifest.ContractAbi()
                {
                    Methods = new Neo.SmartContract.Manifest.ContractMethodDescriptor[]
                         {
                             new Neo.SmartContract.Manifest.ContractMethodDescriptor()
                             {
                                  Name="noverify",
                                  Parameters=new Neo.SmartContract.Manifest.ContractParameterDefinition[0]
                             }
                         }
                },
                Script = new byte[] { 1, 2, 3 }
            }));
        }
    }
}
