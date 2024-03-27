// Copyright (C) 2015-2024 The Neo Project.
//
// PipeMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Service.IO.Pipes.Payloads;
using System;
using System.IO;
using System.Text;

namespace Neo.Service.IO.Pipes
{
    internal sealed class PipeMessage : ISerializable
    {
        public static int HeaderSize =>
            sizeof(PipeCommand) +
            sizeof(int);

        public PipeCommand Command { get; private set; }
        public ISerializable? Payload { get; private set; }

        public int Size =>
            sizeof(PipeCommand) +
            (Payload is null ? 0 : Payload.Size);

        public static PipeMessage Create(PipeCommand command, ISerializable message) =>
            new()
            {
                Command = command,
                Payload = message,
            };

        public static PipeMessage? ReadFromStream(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            var message = new PipeMessage()
            {
                Command = (PipeCommand)reader.ReadByte(),
            };

            var size = reader.ReadInt32();

            if (size <= 0)
                return default;

            var data = reader.ReadBytes(size);

            switch (message.Command)
            {
                case PipeCommand.Version:
                    message.Payload = data.AsSerializable<PipeVersionPayload>();
                    break;
                default:
                    break;
            }

            return message;
        }

        public void Deserialize(ref MemoryReader reader)
        {
            throw new NotImplementedException();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Command);
            if (Payload is null)
                writer.Write(0);
            else
            {
                writer.Write(Payload.Size);
                writer.Write(Payload);
            }
        }
    }
}
