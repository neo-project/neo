using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_StorageContext
    {
        [TestMethod]
        public void TestToArray()
        {
            UInt160 script_hash = new byte[] { 0x00 }.ToScriptHash();
            StorageContext context = new StorageContext()
            {
                ScriptHash = script_hash,
                IsReadOnly = true
            };
            context.ScriptHash.ToArray().Should().BeEquivalentTo(new byte[] { 0x00 }.ToScriptHash().ToArray());
        }
    }
}
