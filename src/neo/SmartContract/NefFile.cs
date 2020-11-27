using Neo.Cryptography;
using Neo.IO;
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
    /// +------------+-----------+------------------------------------------------------------+
    /// | Script     | Var bytes | Var bytes for the payload                                  |
    /// +------------+-----------+------------------------------------------------------------+
    /// | Checksum   | 4 bytes   | First four bytes of double SHA256 hash                     |
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
        /// Script
        /// </summary>
        public byte[] Script { get; set; }

        /// <summary>
        /// Checksum
        /// </summary>
        public uint CheckSum { get; set; }

        public const int MaxScriptLength = 1024 * 1024;

        private const int HeaderSize =
            sizeof(uint) +      // Magic
            32 +                // Compiler
            (sizeof(int) * 4);  // Version

        public int Size =>
            HeaderSize +            // Header
            Script.GetVarSize() +   // Script
            sizeof(uint);           // Checksum

        public void Serialize(BinaryWriter writer)
        {
            SerializeHeader(writer);
            writer.WriteVarBytes(Script ?? Array.Empty<byte>());
            writer.Write(CheckSum);
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
        }

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic) throw new FormatException("Wrong magic");

            Compiler = reader.ReadFixedString(32);
            Version = new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            Script = reader.ReadVarBytes(MaxScriptLength);
            if (Script.Length == 0) throw new ArgumentException($"Script can't be empty");

            CheckSum = reader.ReadUInt32();
            if (CheckSum != ComputeChecksum(this)) throw new FormatException("CRC verification fail");
        }

        /// <summary>
        /// Compute checksum for a file
        /// </summary>
        /// <param name="file">File</param>
        /// <returns>Return checksum</returns>
        unsafe public static uint ComputeChecksum(NefFile file)
        {
            var data = file.ToArray();
            return BitConverter.ToUInt32(Crypto.Hash256(data.AsSpan().Slice(0, data.Length - sizeof(int))));
        }
    }
}
