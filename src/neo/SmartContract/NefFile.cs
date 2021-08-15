// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;

namespace Neo.SmartContract
{
    /*
    ┌───────────────────────────────────────────────────────────────────────┐
    │                    NEO Executable Format 3 (NEF3)                     │
    ├──────────┬───────────────┬────────────────────────────────────────────┤
    │  Field   │     Type      │                  Comment                   │
    ├──────────┼───────────────┼────────────────────────────────────────────┤
    │ Magic    │ uint32        │ Magic header                               │
    │ Compiler │ byte[64]      │ Compiler name and version                  │
    ├──────────┼───────────────┼────────────────────────────────────────────┤
    │ Reserve  │ byte[2]       │ Reserved for future extensions. Must be 0. │
    │ Tokens   │ MethodToken[] │ Method tokens.                             │
    │ Reserve  │ byte[2]       │ Reserved for future extensions. Must be 0. │
    │ Script   │ byte[]        │ Var bytes for the payload                  │
    ├──────────┼───────────────┼────────────────────────────────────────────┤
    │ Checksum │ uint32        │ First four bytes of double SHA256 hash     │
    └──────────┴───────────────┴────────────────────────────────────────────┘
    */
    /// <summary>
    /// Represents the structure of NEO Executable Format.
    /// </summary>
    public class NefFile : ISerializable
    {
        /// <summary>
        /// NEO Executable Format 3 (NEF3)
        /// </summary>
        private const uint Magic = 0x3346454E;

        /// <summary>
        /// The name and version of the compiler that generated this nef file.
        /// </summary>
        public string Compiler { get; set; }

        /// <summary>
        /// The methods that to be called statically.
        /// </summary>
        public MethodToken[] Tokens { get; set; }

        /// <summary>
        /// The script of the contract.
        /// </summary>
        public byte[] Script { get; set; }

        /// <summary>
        /// The checksum of the nef file.
        /// </summary>
        public uint CheckSum { get; set; }

        /// <summary>
        /// The maximum length of the script.
        /// </summary>
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
        /// Computes the checksum for the specified nef file.
        /// </summary>
        /// <param name="file">The specified nef file.</param>
        /// <returns>The checksum of the nef file.</returns>
        public static uint ComputeChecksum(NefFile file)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(Crypto.Hash256(file.ToArray().AsSpan(..^sizeof(uint))));
        }

        /// <summary>
        /// Converts the nef file to a JSON object.
        /// </summary>
        /// <returns>The nef file represented by a JSON object.</returns>
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
