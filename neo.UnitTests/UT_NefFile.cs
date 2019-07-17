using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.IO;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NefFile
    {
        [TestMethod]
        public void ParseTest()
        {
            var script = new NefFile()
            {
                Magic = NefFile.NefMagic.NEF3,
                Compiler = "".PadLeft(32, ' '),
                Version = new Version(1, 2, 3, 4),
                Script = new byte[] { 0x01, 0x02, 0x03 }
            };

            script.ScriptHash = script.Script.ToScriptHash();

            var data = script.ToArray();
            script = NefFile.FromByteArray(data);

            Assert.AreEqual(NefFile.NefMagic.NEF3, script.Magic);
            Assert.AreEqual("".PadLeft(32, ' '), script.Compiler);
            Assert.AreEqual(new Version(1, 2, 3, 4), script.Version);
            Assert.AreEqual(script.Script.ToScriptHash(), script.ScriptHash);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, script.Script);
        }

        [TestMethod]
        public void LimitTest()
        {
            var script = new NefFile()
            {
                Magic = NefFile.NefMagic.NEF3,
                Compiler = "".PadLeft(byte.MaxValue, ' '),
                Version = new Version(1, 2, 3, 4),
                Script = new byte[1024 * 1024],
                ScriptHash = new byte[1024 * 1024].ToScriptHash()
            };

            // Wrong compiler

            Assert.ThrowsException<ArgumentException>(() => script.ToArray());

            // Wrong script

            script.Compiler = "";
            script.Script = new byte[(1024 * 1024) + 1];
            script.ScriptHash = script.Script.ToScriptHash();
            var data = script.ToArray();

            Assert.ThrowsException<FormatException>(() => NefFile.FromByteArray(data));

            // Wrong script hash

            script.Script = new byte[1024 * 1024];
            data = script.ToArray();

            Assert.ThrowsException<FormatException>(() => NefFile.FromByteArray(data));
        }
    }
}