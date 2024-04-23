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
using Neo.Hosting.App.Helpers;
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
            var readBytes = 0;

            if (BitConverter.ToUInt64(span[..pos]) != Magic)
                throw new FormatException();

            if (BitConverter.ToUInt32(span[pos..(pos += sizeof(uint))]) != Crc32.Compute(buffer[(sizeof(ulong) + sizeof(uint))..]))
                throw new InvalidDataException();

            var itmp = BinaryUtility.From7BitEncodedInt(buffer[pos..], out readBytes);
            Version = new Version(Encoding.UTF8.GetString(span[(pos += readBytes)..(pos += itmp)]));

            Plugins = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);

            itmp = BinaryUtility.From7BitEncodedInt(buffer[pos..], out readBytes);
            pos += readBytes;

            for (int i = 0, count = itmp; i < count; i++)
            {
                itmp = BinaryUtility.From7BitEncodedInt(buffer[pos..], out readBytes);
                var key = Encoding.UTF8.GetString(span[(pos += readBytes)..(pos += itmp)]);

                itmp = BinaryUtility.From7BitEncodedInt(buffer[pos..], out readBytes);
                var value = Encoding.UTF8.GetString(span[(pos += readBytes)..(pos += itmp)]);

                _ = Plugins.TryAdd(key, new Version(value));
            }

            Platform = (PlatformID)span[pos++];

            var ltmp = BitConverter.ToInt64([.. buffer[pos..(pos += sizeof(long))].Reverse()]);
            TimeStamp = new DateTime(ltmp);

            itmp = BinaryUtility.From7BitEncodedInt(buffer[pos..], out readBytes);
            MachineName = Encoding.UTF8.GetString(span[(pos += readBytes)..(pos += itmp)]);

            itmp = BinaryUtility.From7BitEncodedInt(buffer[pos..], out readBytes);
            UserName = Encoding.UTF8.GetString(span[(pos += readBytes)..(pos += itmp)]);

            ProcessId = BinaryUtility.From7BitEncodedInt(buffer[pos..], out readBytes);
            pos += readBytes;

            itmp = BinaryUtility.From7BitEncodedInt(buffer[pos..], out readBytes);
            ProcessPath = Encoding.UTF8.GetString(span[(pos += readBytes)..(pos += itmp)]);
        }

        public override byte[] ToArray()
        {
            var strbuf = $"{Version}";
            var tmp = Encoding.UTF8.GetBytes(strbuf);
            var count = Encoding.UTF8.GetByteCount(strbuf);

            byte[] buffer = [.. BinaryUtility.To7BitEncodedInt(count), .. tmp];

            count = Plugins.Count;
            buffer = [.. buffer, .. BinaryUtility.To7BitEncodedInt(count)];
            foreach (var plugin in Plugins)
            {
                strbuf = plugin.Key;
                tmp = Encoding.UTF8.GetBytes(strbuf);
                count = Encoding.UTF8.GetByteCount(strbuf);

                buffer = [.. buffer, .. BinaryUtility.To7BitEncodedInt(count), .. tmp];

                strbuf = $"{plugin.Value}";
                tmp = Encoding.UTF8.GetBytes(strbuf);
                count = Encoding.UTF8.GetByteCount(strbuf);

                buffer = [.. buffer, .. BinaryUtility.To7BitEncodedInt(count), .. tmp];
            }

            buffer = [.. buffer, (byte)Platform];
            buffer = [.. buffer, .. BitConverter.GetBytes(TimeStamp.Ticks).Reverse()];

            tmp = Encoding.UTF8.GetBytes(MachineName);
            count = Encoding.UTF8.GetByteCount(MachineName);

            buffer = [.. buffer, .. BinaryUtility.To7BitEncodedInt(count), .. tmp];

            tmp = Encoding.UTF8.GetBytes(UserName);
            count = Encoding.UTF8.GetByteCount(UserName);

            buffer = [.. buffer, .. BinaryUtility.To7BitEncodedInt(count), .. tmp];
            buffer = [.. buffer, .. BinaryUtility.To7BitEncodedInt(ProcessId)];

            strbuf = ProcessPath ?? string.Empty;
            tmp = Encoding.UTF8.GetBytes(strbuf);
            count = Encoding.UTF8.GetByteCount(strbuf);

            buffer = [.. buffer, .. BinaryUtility.To7BitEncodedInt(count), .. tmp];

            return buffer;
        }
    }
}
