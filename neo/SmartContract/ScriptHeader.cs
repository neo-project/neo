using Neo.IO;
using System;
using System.IO;

namespace Neo.SmartContract
{
    public class ScriptHeader : ISerializable
    {
        public enum ScriptEngine : byte
        {
            NeoVM = 0x01
        }

        /// <summary>
        /// Engine
        /// </summary>
        public ScriptEngine Engine { get; set; }

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
        /// Script Hash
        /// </summary>
        public UInt160 ScriptHash { get; set; }

        public int Size =>
            sizeof(ScriptEngine) +              // Engine
            Compiler.GetVarSize() +             // Compiler
            Version.ToString().GetVarSize() +   // Version
            Script.GetVarSize() +               // Script
            ScriptHash.Size;                    // ScriptHash (CRC)

        /// <summary>
        /// Read Script Header from a binary
        /// </summary>
        /// <param name="script">Script</param>
        /// <returns>Return script header</returns>
        public static ScriptHeader FromByteArray(byte[] script)
        {
            using (var stream = new MemoryStream(script))
            using (var reader = new BinaryReader(stream))
            {
                return reader.ReadSerializable<ScriptHeader>();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Engine);
            writer.WriteVarString(Compiler);
            writer.WriteVarString(Version.ToString());
            writer.WriteVarBytes(Script);
            writer.Write(ScriptHash);
        }

        public void Deserialize(BinaryReader reader)
        {
            Engine = (ScriptEngine)reader.ReadByte();
            Compiler = reader.ReadVarString(byte.MaxValue);
            if (!Version.TryParse(reader.ReadVarString(byte.MaxValue), out var v))
            {
                throw new FormatException("Wrong version format");
            }
            Version = v;
            Script = reader.ReadVarBytes(1024 * 1024);
            ScriptHash = reader.ReadSerializable<UInt160>();

            if (Script.ToScriptHash() != ScriptHash)
            {
                throw new FormatException("CRC verification fail");
            }
        }
    }
}
