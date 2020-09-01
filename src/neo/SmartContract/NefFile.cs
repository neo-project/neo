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
            sizeof(uint) +      // Magic
            32 +                // Compiler
            (sizeof(int) * 4) + // Version
            UInt160.Length;     // ScriptHash

        public int Size =>
            HeaderSize +        // Header
            sizeof(uint) +      // Checksum
            Script.GetVarSize();// Script

        public void Serialize(BinaryWriter writer)
        {
            SerializeHeader(writer, Compiler, Version, ScriptHash);
            writer.Write(CheckSum);
            writer.WriteVarBytes(Script ?? Array.Empty<byte>());
        }

        private static void SerializeHeader(BinaryWriter writer, string compiler, Version version, UInt160 scriptHash)
        {
            writer.Write(Magic);
            writer.WriteFixedString(compiler, 32);

            // Version
            writer.Write(version.Major);
            writer.Write(version.Minor);
            writer.Write(version.Build);
            writer.Write(version.Revision);

            writer.Write(scriptHash);
        }

        public static (string compiler, Version version, UInt160 scriptHash) DeserializeHeader(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic)
            {
                throw new FormatException("Wrong magic");
            }

            var compiler = reader.ReadFixedString(32);
            var version = new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            var scriptHash = reader.ReadSerializable<UInt160>();
            var checkSum = reader.ReadUInt32();

            if (checkSum != ComputeChecksum(compiler, version, scriptHash))
            {
                throw new FormatException("CRC verification fail");
            }

            return (compiler, version, scriptHash);
        }

        public void Deserialize(BinaryReader reader)
        {
            (Compiler, Version, ScriptHash) = DeserializeHeader(reader);
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
            return ComputeChecksum(file.Compiler, file.Version, file.ScriptHash);
        }

        unsafe private static uint ComputeChecksum(string compiler, Version version, UInt160 scriptHash) 
        {
            Span<byte> header = stackalloc byte[HeaderSize];
            fixed (byte* p = header)
                using (UnmanagedMemoryStream ms = new UnmanagedMemoryStream(p, HeaderSize, HeaderSize, FileAccess.Write))
                using (BinaryWriter wr = new BinaryWriter(ms, Utility.StrictUTF8, false))
                {
                    SerializeHeader(wr, compiler, version, scriptHash);
                    wr.Flush();
                }
            return BitConverter.ToUInt32(Crypto.Hash256(header), 0);
        }
    }
}
