using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract.Manifest;
using System;
using System.IO;

namespace Neo.SmartContract
{
    /// <summary>
    /// +------------+-----------+------------------------------------------------------------+
    /// |   Field    |  Length   |                          Comment                           |
    /// +------------+-----------+------------------------------------------------------------+
    /// | Magic      | 4 bytes   | Magic header                                               |
    /// | Compiler   | 32 bytes  | Compiler used                                              |
    /// | Version    | 16 bytes  | Compiler version (Mayor, Minor, Build, Version)            |
    /// | Abi        | Varbytes  | Abi file                                                   |
    /// +------------+-----------+------------------------------------------------------------+
    /// | Checksum   | 4 bytes   | First four bytes of double SHA256 hash                     |
    /// +------------+-----------+------------------------------------------------------------+
    /// | Script     | Var bytes | Var bytes for the payload                                  |
    /// +------------+-----------+------------------------------------------------------------+
    /// </summary>
    public class NefFile : ISerializable
    {
        /// <summary>
        /// NEO Executable Format 3 (NEF3)
        /// </summary>
        private const uint Magic = 0x3346454E;

        /// <summary>
        /// Compiler
        /// </summary>
        public string Compiler { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Abi
        /// </summary>
        public ContractAbi Abi { get; set; }

        /// <summary>
        /// Checksum
        /// </summary>
        public uint CheckSum { get; set; }

        /// <summary>
        /// Script
        /// </summary>
        public byte[] Script { get; set; }

        /// <summary>
        /// Script Hash
        /// </summary>
        public UInt160 ScriptHash => Abi.Hash;

        private const int StaticSize =
            sizeof(uint) +      // Magic
            32 +                // Compiler
            (sizeof(int) * 4);  // Version

        public int Size =>
            StaticSize +
            Abi.ToJson().ToString().GetVarSize() +  // Abi
            sizeof(uint) +                          // Checksum
            Script.GetVarSize();                    // Script

        public void Serialize(BinaryWriter writer)
        {
            SerializeHeader(writer);
            writer.Write(CheckSum);
            writer.WriteVarBytes(Script ?? Array.Empty<byte>());
        }

        private void SerializeHeader(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.WriteFixedString(Compiler, 32);

            // Version
            writer.Write(Version.Major);
            writer.Write(Version.Minor);
            writer.Write(Version.Build);
            writer.Write(Version.Revision);

            writer.WriteVarString(Abi.ToJson().ToString());
        }

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic)
            {
                throw new FormatException("Wrong magic");
            }

            Compiler = reader.ReadFixedString(32);
            Version = new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            Abi = ContractAbi.FromJson(reader.ReadVarString());
            CheckSum = reader.ReadUInt32();

            if (CheckSum != ComputeChecksum(this))
            {
                throw new FormatException("CRC verification fail");
            }

            Script = reader.ReadVarBytes(1024 * 1024);

            if (Script.ToScriptHash() != Abi.Hash)
            {
                throw new FormatException("Abi hash doesn't match with the script");
            }
        }

        /// <summary>
        /// Compute checksum for a file
        /// </summary>
        /// <param name="file">File</param>
        /// <returns>Return checksum</returns>
        unsafe public static uint ComputeChecksum(NefFile file)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter wr = new BinaryWriter(ms, Utility.StrictUTF8, false);
            file.SerializeHeader(wr);
            wr.Flush();

            ms.Seek(0, SeekOrigin.Begin);
            return BitConverter.ToUInt32(Crypto.Hash256(ms.ToArray()), 0);
        }
    }
}
