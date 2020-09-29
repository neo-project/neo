using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using System;
using System.IO;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_NefFile
    {
        public NefFile file = new NefFile()
        {
            Compiler = "".PadLeft(32, ' '),
            Version = new Version(1, 2, 3, 4),
            Script = new byte[] { 0x01, 0x02, 0x03 }
        };

        [TestInitialize]
        public void TestSetup()
        {
            file.Abi = new ContractAbi()
            {
                Events = Array.Empty<ContractEventDescriptor>(),
                Methods = Array.Empty<ContractMethodDescriptor>(),
                Hash = file.Script.ToScriptHash()
            };
        }

        [TestMethod]
        public void TestDeserialize()
        {
            byte[] wrongMagic = { 0x00, 0x00, 0x00, 0x00 };
            using (MemoryStream ms = new MemoryStream(1024))
            using (BinaryWriter writer = new BinaryWriter(ms))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ((ISerializable)file).Serialize(writer);
                ms.Seek(0, SeekOrigin.Begin);
                ms.Write(wrongMagic, 0, 4);
                ms.Seek(0, SeekOrigin.Begin);
                ISerializable newFile = new NefFile();
                Action action = () => newFile.Deserialize(reader);
                action.Should().Throw<FormatException>();
            }

            file.Abi.Hash = new byte[] { 0x01 }.ToScriptHash();
            using (MemoryStream ms = new MemoryStream(1024))
            using (BinaryWriter writer = new BinaryWriter(ms))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ((ISerializable)file).Serialize(writer);
                ms.Seek(0, SeekOrigin.Begin);
                ISerializable newFile = new NefFile();
                Action action = () => newFile.Deserialize(reader);
                action.Should().Throw<FormatException>();
            }

            file.Abi.Hash = file.Script.ToScriptHash();
            var data = file.ToArray();
            var newFile1 = data.AsSerializable<NefFile>();
            newFile1.Version.Should().Be(file.Version);
            newFile1.Compiler.Should().Be(file.Compiler);
            newFile1.ScriptHash.Should().Be(file.ScriptHash);
            newFile1.Script.Should().BeEquivalentTo(file.Script);
        }

        [TestMethod]
        public void TestGetSize()
        {
            file.Size.Should().Be(4 + 32 + 16 + file.Abi.ToJson().ToString().GetVarSize() + 4);
        }

        [TestMethod]
        public void ParseTest()
        {
            var file = new NefFile()
            {
                Compiler = "".PadLeft(32, ' '),
                Version = new Version(1, 2, 3, 4),
                Script = new byte[] { 0x01, 0x02, 0x03 },
                Abi = new ContractAbi()
                {
                    Events = Array.Empty<ContractEventDescriptor>(),
                    Methods = Array.Empty<ContractMethodDescriptor>(),
                    Hash = new byte[] { 0x01, 0x02, 0x03 }.ToScriptHash()
                }
            };

            file.Abi.Hash = file.Script.ToScriptHash();

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
                Abi = new ContractAbi()
                {
                    Events = Array.Empty<ContractEventDescriptor>(),
                    Methods = Array.Empty<ContractMethodDescriptor>(),
                    Hash = new byte[1024 * 1024].ToScriptHash()
                }
            };

            // Wrong compiler

            Assert.ThrowsException<ArgumentException>(() => file.ToArray());

            // Wrong script

            file.Compiler = "";
            file.Script = new byte[(1024 * 1024) + 1];
            file.Abi.Hash = file.Script.ToScriptHash();
            var data = file.ToArray();

            Assert.ThrowsException<FormatException>(() => data.AsSerializable<NefFile>());

            // Wrong script hash

            file.Script = new byte[1024 * 1024];
            data = file.ToArray();

            Assert.ThrowsException<FormatException>(() => data.AsSerializable<NefFile>());
        }
    }
}
