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
    /// +------------+-----------+------------------------------------------------------------+
    /// | Abi        | Var bytes | Abi file                                                   |
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
        /// Script
        /// </summary>
        public byte[] Script { get; set; }

        internal const int StaticSize =
            sizeof(uint) +      // Magic
            32 +                // Compiler
            (sizeof(int) * 4);  // Version

        public int Size =>
            StaticSize +
            Abi.ToJson().ToString().GetVarSize() +  // Abi
            Script.GetVarSize();                    // Script

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.WriteFixedString(Compiler, 32);

            // Version
            writer.Write(Version.Major);
            writer.Write(Version.Minor);
            writer.Write(Version.Build);
            writer.Write(Version.Revision);

            writer.WriteVarString(Abi.ToJson().ToString());
            writer.WriteVarBytes(Script ?? Array.Empty<byte>());
        }

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic)
            {
                throw new FormatException("Wrong magic");
            }

            Compiler = reader.ReadFixedString(32);
            Version = new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            Abi = ContractAbi.FromJson(JObject.Parse(reader.ReadVarString()));
            Script = reader.ReadVarBytes(1024 * 1024);
        }
    }
}
