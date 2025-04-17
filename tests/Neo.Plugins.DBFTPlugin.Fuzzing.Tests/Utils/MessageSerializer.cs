// Copyright (C) 2015-2025 The Neo Project.
//
// MessageSerializer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using System;
using System.IO;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Utils
{
    /// <summary>
    /// Helper methods for serializing messages
    /// </summary>
    public static class MessageSerializer
    {
        /// <summary>
        /// Write a consensus message to a file
        /// </summary>
        /// <param name="outputDirectory">Directory to write the file</param>
        /// <param name="filename">Filename</param>
        /// <param name="message">Consensus message to write</param>
        /// <param name="category">Category for the extensible payload</param>
        public static void WriteMessageToFile(string outputDirectory, string filename, ConsensusMessage message, string category)
        {
            // Create an extensible payload to wrap the consensus message
            var payload = new ExtensiblePayload
            {
                Category = category,
                Sender = UInt160.Zero,
                Witness = new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
            };

            // Serialize the consensus message
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                message.Serialize(writer);
                payload.Data = ms.ToArray();
            }

            // Serialize the extensible payload
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                ((ISerializable)payload).Serialize(writer);
                File.WriteAllBytes(Path.Combine(outputDirectory, filename), ms.ToArray());
            }
        }

        /// <summary>
        /// Write raw bytes to a file as an extensible payload
        /// </summary>
        /// <param name="outputDirectory">Directory to write the file</param>
        /// <param name="filename">Filename</param>
        /// <param name="data">Raw data to write</param>
        /// <param name="category">Category for the extensible payload</param>
        public static void WriteRawBytesToFile(string outputDirectory, string filename, byte[] data, string category)
        {
            // Create an extensible payload with the raw data
            var payload = new ExtensiblePayload
            {
                Category = category,
                Sender = UInt160.Zero,
                Data = data,
                Witness = new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
            };

            // Serialize the extensible payload
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                ((ISerializable)payload).Serialize(writer);
                File.WriteAllBytes(Path.Combine(outputDirectory, filename), ms.ToArray());
            }
        }
    }
}
