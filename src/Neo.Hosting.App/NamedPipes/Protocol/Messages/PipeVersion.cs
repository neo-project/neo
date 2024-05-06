// Copyright (C) 2015-2024 The Neo Project.
//
// PipeVersion.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Extensions;
using Neo.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes.Protocol.Messages
{
    internal sealed class PipeVersion : IPipeMessage
    {
        public const ulong Magic = 0x4e4f4953524556ul; // VERSION

        // Assembly Information
        public int VersionNumber { get; set; }
        public IDictionary<string, Version> Plugins { get; set; }

        // Computer Information
        public PlatformID Platform { get; set; }
        public DateTime TimeStamp { get; set; }
        public string MachineName { get; set; }
        public string UserName { get; set; }

        // Service Information
        public int ProcessId { get; set; }
        public string ProcessPath { get; set; }

        public PipeVersion()
        {
            VersionNumber = Program.ApplicationVersionNumber;
            Plugins = Plugin.Plugins.ToDictionary(k => k.Name, v => v.Version, StringComparer.InvariantCultureIgnoreCase);
            Platform = Environment.OSVersion.Platform;
            TimeStamp = DateTime.UtcNow;
            MachineName = Environment.MachineName;
            UserName = Environment.UserName;
            ProcessId = Environment.ProcessId;
            ProcessPath = Environment.ProcessPath ?? string.Empty;
        }

        public int Size => 0;

        public Task CopyFromAsync(Stream stream)
        {
            if (stream.CanRead == false)
                throw new IOException();

            var magic = stream.Read<ulong>();
            if (magic != Magic)
                throw new InvalidDataException();

            var version = stream.Read<int>();
            if (version != Program.ApplicationVersionNumber)
                throw new InvalidDataException();

            VersionNumber = version;

            var plugins = new Dictionary<string, Version>(StringComparer.InvariantCultureIgnoreCase);
            var count = stream.Read<int>();

            for (var i = 0; i < count; i++)
            {
                var key = stream.ReadString();
                if (Version.TryParse(stream.ReadString(), out var value))
                    _ = plugins.TryAdd(key, value);
            }

            Platform = stream.Read<PlatformID>();
            TimeStamp = DateTime.FromBinary(stream.Read<long>());
            MachineName = stream.ReadString();
            UserName = stream.ReadString();
            ProcessId = stream.Read<int>();
            ProcessPath = stream.ReadString();

            return Task.CompletedTask;
        }

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanWrite == false)
                throw new IOException();

            CopyToStream(stream);
            return stream.FlushAsync(cancellationToken);
        }

        public byte[] ToArray()
        {
            using var ms = new MemoryStream();
            CopyToStream(ms);
            return ms.ToArray();
        }

        private void CopyToStream(Stream stream)
        {
            if (stream.CanWrite == false)
                throw new IOException();

            stream.Write(Magic);
            stream.Write(Program.ApplicationVersionNumber);

            stream.Write(Plugins.Count);
            foreach (var plugin in Plugins)
            {
                stream.Write(plugin.Key);
                stream.Write($"{plugin.Value}");
            }

            stream.Write(Platform);
            stream.Write(TimeStamp.ToBinary());
            stream.Write(MachineName);
            stream.Write(UserName);
            stream.Write(ProcessId);
            stream.Write(ProcessPath);
        }
    }
}
