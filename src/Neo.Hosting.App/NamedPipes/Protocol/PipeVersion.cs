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

using Neo.Cryptography;
using Neo.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.Hosting.App.NamedPipes.Protocol
{
    internal sealed class PipeVersion : PipeMessage
    {
        public override ulong Magic { get; } = 0x56455253494f4eu; // VERSION

        // Assembly Information
        public Version Version { get; set; } = Program.ApplicationVersion;
        public IDictionary<string, Version> Plugins { get; set; } = Plugin.Plugins.ToDictionary(k => k.Name, v => v.Version);

        // Computer Information
        public PlatformID Platform { get; set; } = Environment.OSVersion.Platform;
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string MachineName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;

        // Service Information
        public int ProcessId { get; set; } = Environment.ProcessId;
        public string? ProcessPath { get; set; } = Environment.ProcessPath;

        protected override void Initialize(byte[] buffer)
        {
            var span = buffer.AsSpan();

            var pos = sizeof(ulong);

            if (BitConverter.ToUInt64(span[..pos]) != Magic)
                throw new FormatException();

            if (BitConverter.ToUInt32(span[pos..(pos += sizeof(uint))]) != Crc32.Compute(buffer[(sizeof(ulong) + sizeof(uint))..]))
                throw new InvalidDataException();

            var itmp = (int)BitConverter.ToUInt32([.. buffer[pos..(pos += sizeof(uint))].Reverse()]);
            Version = new Version(Encoding.UTF8.GetString(span[pos..(pos += itmp)]));

            Plugins = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);

            itmp = (int)BitConverter.ToUInt32([.. buffer[pos..(pos += sizeof(uint))].Reverse()]);
            for (int i = 0, count = itmp; i < count; i++)
            {
                itmp = (int)BitConverter.ToUInt32([.. buffer[pos..(pos += sizeof(uint))].Reverse()]);
                var key = Encoding.UTF8.GetString(span[pos..(pos += itmp)]);

                itmp = (int)BitConverter.ToUInt32([.. buffer[pos..(pos += sizeof(uint))].Reverse()]);
                var value = Encoding.UTF8.GetString(span[pos..(pos += itmp)]);

                _ = Plugins.TryAdd(key, new Version(value));
            }

            Platform = (PlatformID)span[pos++];

            var ltmp = BitConverter.ToInt64([.. buffer[pos..(pos += sizeof(long))].Reverse()]);
            TimeStamp = new DateTime(ltmp);

            itmp = (int)BitConverter.ToUInt32([.. buffer[pos..(pos += sizeof(uint))].Reverse()]);
            if (itmp > 0)
                MachineName = Encoding.UTF8.GetString(span[pos..(pos += itmp)]);

            itmp = (int)BitConverter.ToUInt32([.. buffer[pos..(pos += sizeof(uint))].Reverse()]);
            if (itmp > 0)
                UserName = Encoding.UTF8.GetString(span[pos..(pos += itmp)]);

            ProcessId = (int)BitConverter.ToUInt32([.. buffer[pos..(pos += sizeof(uint))].Reverse()]);

            itmp = (int)BitConverter.ToUInt32([.. buffer[pos..(pos += sizeof(uint))].Reverse()]);
            if (itmp > 0)
                ProcessPath = Encoding.UTF8.GetString(span[pos..(pos += itmp)]);
        }

        public override byte[] ToArray()
        {
            var strbuf = $"{Version}";
            var tmp = Encoding.UTF8.GetBytes(strbuf);
            var count = (uint)Encoding.UTF8.GetByteCount(strbuf);

            byte[] buffer = [.. BitConverter.GetBytes(count).Reverse(), .. tmp];

            count = (uint)Plugins.Count;
            buffer = [.. buffer, .. BitConverter.GetBytes(count).Reverse()];
            foreach (var plugin in Plugins)
            {
                strbuf = plugin.Key;
                tmp = Encoding.UTF8.GetBytes(strbuf);
                count = (uint)Encoding.UTF8.GetByteCount(strbuf);

                buffer = [.. buffer, .. BitConverter.GetBytes(count).Reverse(), .. tmp];

                strbuf = $"{plugin.Value}";
                tmp = Encoding.UTF8.GetBytes(strbuf);
                count = (uint)Encoding.UTF8.GetByteCount(strbuf);

                buffer = [.. buffer, .. BitConverter.GetBytes(count).Reverse(), .. tmp];
            }

            buffer = [.. buffer, (byte)Platform];
            buffer = [.. buffer, .. BitConverter.GetBytes(TimeStamp.Ticks).Reverse()];

            tmp = Encoding.UTF8.GetBytes(MachineName);
            count = (uint)Encoding.UTF8.GetByteCount(MachineName);

            buffer = [.. buffer, .. BitConverter.GetBytes(count).Reverse(), .. tmp];

            tmp = Encoding.UTF8.GetBytes(UserName);
            count = (uint)Encoding.UTF8.GetByteCount(UserName);

            buffer = [.. buffer, .. BitConverter.GetBytes(count).Reverse(), .. tmp];
            buffer = [.. buffer, .. BitConverter.GetBytes(ProcessId).Reverse()];

            strbuf = ProcessPath ?? string.Empty;
            tmp = Encoding.UTF8.GetBytes(strbuf);
            count = (uint)Encoding.UTF8.GetByteCount(strbuf);

            buffer = [.. buffer, .. BitConverter.GetBytes(count).Reverse(), .. tmp];

            return buffer;
        }
    }
}
