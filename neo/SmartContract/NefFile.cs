using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;

namespace Neo.SmartContract
{
    /// <summary>
    /// 
    /// +------------+-----------+------------------------------------------------------------+
    /// |   Field    |  Length   |                          Comment                           |
    /// +------------+-----------+------------------------------------------------------------+
    /// | Magic      | 4 bytes   | Magic header                                               |
    /// | Compiler   | 32 bytes  | Compiler used                                              |
    /// | Version    | 16 bytes  | Compiler version (Mayor, Minor, Build, Version)            |
    /// | ScriptHash | 20 bytes  | ScriptHash for the script                                  |
    /// | Script     | Var bytes | Var bytes for the payload                                  |
    /// | Checksum   | 4 bytes   | Sha256 of the whole file whithout the last for bytes(CRC)  |
    /// +------------+-----------+------------------------------------------------------------+
    /// 
    /// </summary>
    public class NefFile : ISerializable
    {
        public enum NefMagic : int
        {
            /// <summary>
            /// NEO Executable Format 3
            /// </summary>
            NEF3 = 0x4E454633
        }

        /// <summary>
        /// Magic
        /// </summary>
        public NefMagic Magic { get; set; }

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

        /// <summary>
        /// Script Hash
        /// </summary>
        public UInt160 ScriptHash { get; set; }

        public int Size =>
            sizeof(NefMagic) +          // Engine
            Compiler.GetVarSize() +     // Compiler
            (sizeof(int) * 4) +         // Version
            ScriptHash.Size +           // ScriptHash
            Script.GetVarSize() +       // Script
            sizeof(uint);               // Checksum

        /// <summary>
        /// Read Script Header from a binary
        /// </summary>
        /// <param name="script">Script</param>
        /// <returns>Return script header</returns>
        public static NefFile FromByteArray(byte[] script)
        {
            using (var stream = new MemoryStream(script))
            using (var reader = new BinaryReader(stream))
            {
                return reader.ReadSerializable<NefFile>();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((int)Magic);
            writer.WriteFixedString(Compiler, 32);

            // Version
            writer.Write(Version.Major);
            writer.Write(Version.Minor);
            writer.Write(Version.Build);
            writer.Write(Version.Revision);

            writer.Write(ScriptHash);
            writer.WriteVarBytes(Script);
            writer.Write(CheckSum);
        }

        public void Deserialize(BinaryReader reader)
        {
            Magic = (NefMagic)reader.ReadInt32();

            if (Magic != NefMagic.NEF3)
            {
                throw new FormatException("Wrong magic");
            }

            Compiler = reader.ReadFixedString(32);
            Version = new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            ScriptHash = reader.ReadSerializable<UInt160>();

            Script = reader.ReadVarBytes(1024 * 1024);
            if (Script.ToScriptHash() != ScriptHash)
            {
                throw new FormatException("ScriptHash is different");
            }

            CheckSum = reader.ReadUInt32();
            if (CheckSum != ComputeChecksum(this))
            {
                throw new FormatException("CRC verification fail");
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

                var buffer = new byte[ms.Length - sizeof(uint)];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);

                return BitConverter.ToUInt32(buffer.Sha256(), 0);
            }
        }
    }
}