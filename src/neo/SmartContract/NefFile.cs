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
    /// | ScriptHash | 20 bytes  | ScriptHash for the script                                  |
    /// +------------+-----------+------------------------------------------------------------+
    /// | Checksum   | 4 bytes   | Sha256 of the header (CRC)                                 |
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
        /// Script Hash
        /// </summary>
        public UInt160 ScriptHash { get; set; }

        /// <summary>
        /// Checksum
        /// </summary>
        public uint CheckSum { get; set; }

        /// <summary>
        /// Script
        /// </summary>
        public byte[] Script { get; set; }

        private const int HeaderSize =
            sizeof(uint) +              // Magic
            32 +                        // Compiler
            (sizeof(int) * 4) +         // Version
            UInt160.Length +            // ScriptHash
            sizeof(uint);               // Checksum

        public int Size =>
            HeaderSize +              // Header
            Script.GetVarSize();      // Script

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.WriteFixedString(Compiler, 32);

            // Version
            writer.Write(Version.Major);
            writer.Write(Version.Minor);
            writer.Write(Version.Build);
            writer.Write(Version.Revision);

            writer.Write(ScriptHash);
            writer.Write(CheckSum);
            writer.WriteVarBytes(Script ?? new byte[0]);
        }

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic)
            {
                throw new FormatException("Wrong magic");
            }

            Compiler = reader.ReadFixedString(32);
            Version = new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            ScriptHash = reader.ReadSerializable<UInt160>();
            CheckSum = reader.ReadUInt32();

            if (CheckSum != ComputeChecksum(this))
            {
                throw new FormatException("CRC verification fail");
            }

            Script = reader.ReadVarBytes(1024 * 1024);

            if (Script.ToScriptHash() != ScriptHash)
            {
                throw new FormatException("ScriptHash is different");
            }
        }

        /// <summary>
        /// Compute checksum for a file
        /// </summary>
        /// <param name="file">File</param>
        /// <returns>Return checksum</returns>
        public static uint ComputeChecksum(NefFile file)
        {
            using (var ms = new MemoryStream())
            using (var wr = new BinaryWriter(ms))
            {
                file.Serialize(wr);
                wr.Flush();

                // Read header without CRC

                var buffer = new byte[HeaderSize - sizeof(uint)];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);

                return BitConverter.ToUInt32(buffer.Sha256(), 0);
            }
        }
    }
}
