using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// ┌───────────────────────────────────────────────────────────────────────┐
    /// │                    NEO Executable Format 3 (NEF3)                     │
    /// ├──────────┬───────────────┬────────────────────────────────────────────┤
    /// │  Field   │     Type      │                  Comment                   │
    /// ├──────────┼───────────────┼────────────────────────────────────────────┤
    /// │ Magic    │ uint32        │ Magic header                               │
    /// │ Compiler │ byte[64]      │ Compiler name and version                  │
    /// ├──────────┼───────────────┼────────────────────────────────────────────┤
    /// │ Reserve  │ byte[2]       │ Reserved for future extensions. Must be 0. │
    /// │ Tokens   │ MethodToken[] │ Method tokens.                             │
    /// │ Reserve  │ byte[2]       │ Reserved for future extensions. Must be 0. │
    /// │ Script   │ byte[]        │ Var bytes for the payload                  │
    /// ├──────────┼───────────────┼────────────────────────────────────────────┤
    /// │ Checksum │ uint32        │ First four bytes of double SHA256 hash     │
    /// └──────────┴───────────────┴────────────────────────────────────────────┘
    /// </summary>
    public class NefFile : ISerializable
    {
        /// <summary>
        /// NEO Executable Format 3 (NEF3)
        /// </summary>
        private const uint Magic = 0x3346454E;

        /// <summary>
        /// Compiler name and version
        /// </summary>
        public string Compiler { get; set; }

        /// <summary>
        /// Method tokens
        /// </summary>
        public MethodToken[] Tokens { get; set; }

        /// <summary>
        /// Script
        /// </summary>
        public byte[] Script { get; set; }

        /// <summary>
        /// Checksum
        /// </summary>
        public uint CheckSum { get; set; }

        public const int MaxScriptLength = 512 * 1024;

        private const int HeaderSize =
            sizeof(uint) +  // Magic
            64;             // Compiler

        public int Size =>
            HeaderSize +            // Header
            2 +                     // Reserve
            Tokens.GetVarSize() +   // Tokens
            2 +                     // Reserve
            Script.GetVarSize() +   // Script
            sizeof(uint);           // Checksum

        public void Serialize(BinaryWriter writer)
        {
            SerializeHeader(writer);
            writer.Write((short)0);
            writer.Write(Tokens);
            writer.Write((short)0);
            writer.WriteVarBytes(Script ?? Array.Empty<byte>());
            writer.Write(CheckSum);
        }

        private void SerializeHeader(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.WriteFixedString(Compiler, 64);
        }

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic) throw new FormatException("Wrong magic");

            Compiler = reader.ReadFixedString(64);

            if (reader.ReadUInt16() != 0) throw new FormatException("Reserved bytes must be 0");

            Tokens = reader.ReadSerializableArray<MethodToken>(128);

            if (reader.ReadUInt16() != 0) throw new FormatException("Reserved bytes must be 0");

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
            return BinaryPrimitives.ReadUInt32LittleEndian(Crypto.Hash256(file.ToArray().AsSpan(..^sizeof(uint))));
        }

        public JObject ToJson()
        {
            return new JObject
            {
                ["magic"] = Magic,
                ["compiler"] = Compiler,
                ["tokens"] = new JArray(Tokens.Select(p => p.ToJson())),
                ["script"] = Convert.ToBase64String(Script),
                ["checksum"] = CheckSum
            };
        }
    }
}
