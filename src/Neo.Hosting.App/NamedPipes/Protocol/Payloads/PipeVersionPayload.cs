// Copyright (C) 2015-2024 The Neo Project.
//
// PipeVersionPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Buffers;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using System;

namespace Neo.Hosting.App.NamedPipes.Protocol.Payloads
{
    internal sealed class PipeVersionPayload : IPipeMessage
    {
        public int VersionNumber { get; set; }

        public PlatformID Platform { get; set; }

        public DateTime TimeStamp { get; set; }

        public string MachineName { get; set; }

        public string UserName { get; set; }

        public int ProcessId { get; set; }

        public string ProcessPath { get; set; }

        public PipeVersionPayload()
        {
            VersionNumber = Program.ApplicationVersionNumber;
            Platform = Environment.OSVersion.Platform;
            TimeStamp = DateTime.UtcNow;
            MachineName = Environment.MachineName;
            UserName = Environment.UserName;
            ProcessId = Environment.ProcessId;
            ProcessPath = Environment.ProcessPath ?? string.Empty;
        }

        public int Size =>
            sizeof(int) +
            sizeof(PlatformID) +
            sizeof(long) +
            Struffer.SizeOf(MachineName) +
            Struffer.SizeOf(UserName) +
            sizeof(int) +
            Struffer.SizeOf(ProcessPath);

        public void FromArray(byte[] buffer)
        {
            var wrapper = new Struffer(buffer);

            VersionNumber = wrapper.Read<int>();
            Platform = wrapper.Read<PlatformID>();
            TimeStamp = DateTime.FromBinary(wrapper.Read<long>());
            MachineName = wrapper.ReadString();
            UserName = wrapper.ReadString();
            ProcessId = wrapper.Read<int>();
            ProcessPath = wrapper.ReadString();
        }

        public byte[] ToArray()
        {
            var wrapper = new Struffer(Size);

            wrapper.Write(VersionNumber);
            wrapper.Write(Platform);
            wrapper.Write(TimeStamp.ToBinary());
            wrapper.Write(MachineName);
            wrapper.Write(UserName);
            wrapper.Write(ProcessId);
            wrapper.Write(ProcessPath);

            return [.. wrapper];
        }
    }
}
