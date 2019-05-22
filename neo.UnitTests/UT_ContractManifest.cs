using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ContractManifest
    {
        [TestMethod]
        public void ParseFromJson()
        {
            var json = @"{""hash"":""0x0000000000000000000000000000000000000000"",""groups"":null,""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""returnType"":""Array"",""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}]},""methods"":[],""events"":[]},""permissions"":[],""trusts"":[],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson(), json);

            Assert.IsNull(manifest.Groups);
            Assert.AreEqual(manifest.Permissions.Count, 0);
            Assert.AreEqual(manifest.SafeMethods.Count, 0);
            Assert.AreEqual(manifest.Trusts.Count, 0);
            Assert.AreEqual(manifest.Features, Ledger.ContractPropertyState.NoProperty);
            Assert.IsTrue(manifest.Abi.Equals(ContractManifest.CreateDefault(UInt160.Zero).Abi));
        }
    }
}