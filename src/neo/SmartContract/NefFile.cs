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
    /// | Version    | 32 bytes  | Compiler version                                           |
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
        public string Version { get; set; }

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
            (32 * 2);           // Compiler+Version

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
            writer.WriteFixedString(Version, 32);
        }

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic) throw new FormatException("Wrong magic");

            Compiler = reader.ReadFixedString(32);
            Version = reader.ReadFixedString(32);
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
        public static uint ComputeChecksum(NefFile file)
        {
            return BitConverter.ToUInt32(Crypto.Hash256(file.ToArray().AsSpan(..^sizeof(int))));
        }
    }
}
