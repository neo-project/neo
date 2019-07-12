using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.IO;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ScriptFile
    {
        [TestMethod]
        public void ParseTest()
        {
            var script = new ScriptFile()
            {
                Engine = ScriptFile.ScriptEngine.NeoVM,
                Compiler = "".PadLeft(byte.MaxValue, ' '),
                Version = new Version(1, 2, 3, 4),
                Script = new byte[] { 0x01, 0x02, 0x03 }
            };

            script.ScriptHash = script.Script.ToScriptHash();

            var data = script.ToArray();
            script = ScriptFile.FromByteArray(data);

            Assert.AreEqual(ScriptFile.ScriptEngine.NeoVM, script.Engine);
            Assert.AreEqual("".PadLeft(byte.MaxValue, ' '), script.Compiler);
            Assert.AreEqual(new Version(1, 2, 3, 4), script.Version);
            Assert.AreEqual(script.Script.ToScriptHash(), script.ScriptHash);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, script.Script);
        }

        [TestMethod]
        public void LimitTest()
        {
            var script = new ScriptFile()
            {
                Engine = ScriptFile.ScriptEngine.NeoVM,
                Compiler = "".PadLeft(byte.MaxValue + 1, ' '),
                Version = new Version(1, 2, 3, 4),
                Script = new byte[1024 * 1024],
                ScriptHash = new byte[1024 * 1024].ToScriptHash()
            };

            // Wrong compiler

            var data = script.ToArray();
            Assert.ThrowsException<FormatException>(() => ScriptFile.FromByteArray(data));

            // Wrong script

            script.Script = new byte[(1024 * 1024) + 1];
            script.ScriptHash = script.Script.ToScriptHash();
            data = script.ToArray();

            Assert.ThrowsException<FormatException>(() => ScriptFile.FromByteArray(data));

            // Wrong script hash

            script.Script = new byte[1024 * 1024];
            data = script.ToArray();

            Assert.ThrowsException<FormatException>(() => ScriptFile.FromByteArray(data));
        }
    }
}