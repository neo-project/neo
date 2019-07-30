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
            var file = new NefFile()
            {
                Compiler = "".PadLeft(32, ' '),
                Version = new Version(1, 2, 3, 4),
                Script = new byte[] { 0x01, 0x02, 0x03 }
            };

            file.ScriptHash = file.Script.ToScriptHash();
            file.CheckSum = NefFile.ComputeChecksum(file);

            var data = file.ToArray();
            file = data.AsSerializable<NefFile>();

            Assert.AreEqual("".PadLeft(32, ' '), file.Compiler);
            Assert.AreEqual(new Version(1, 2, 3, 4), file.Version);
            Assert.AreEqual(file.Script.ToScriptHash(), file.ScriptHash);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, file.Script);
        }

        [TestMethod]
        public void LimitTest()
        {
            var file = new NefFile()
            {
                Compiler = "".PadLeft(byte.MaxValue, ' '),
                Version = new Version(1, 2, 3, 4),
                Script = new byte[1024 * 1024],
                ScriptHash = new byte[1024 * 1024].ToScriptHash(),
                CheckSum = 0
            };

            // Wrong compiler

            Assert.ThrowsException<ArgumentException>(() => file.ToArray());

            // Wrong script

            file.Compiler = "";
            file.Script = new byte[(1024 * 1024) + 1];
            file.ScriptHash = file.Script.ToScriptHash();
            var data = file.ToArray();

            Assert.ThrowsException<FormatException>(() => data.AsSerializable<NefFile>());

            // Wrong script hash

            file.Script = new byte[1024 * 1024];
            data = file.ToArray();

            Assert.ThrowsException<FormatException>(() => data.AsSerializable<NefFile>());

            // Wrong checksum

            file.Script = new byte[1024];
            data = file.ToArray();
            file.CheckSum = NefFile.ComputeChecksum(file) + 1;

            Assert.ThrowsException<FormatException>(() => data.AsSerializable<NefFile>());
        }
    }
}