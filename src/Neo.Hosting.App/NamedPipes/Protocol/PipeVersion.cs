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

using Neo.Hosting.App.Helpers;
using Neo.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.Hosting.App.NamedPipes.Protocol
{
    internal sealed class PipeVersion : PipeMessage
    {
        public override ulong Magic { get; } = 0x56455253494f4eul; // VERSION

        // Assembly Information
        public Version Version { get; set; } = Program.ApplicationVersion;
        public IDictionary<string, Version> Plugins { get; set; } = Plugin.Plugins.ToDictionary(k => k.Name, v => v.Version, StringComparer.OrdinalIgnoreCase);

        // Computer Information
        public PlatformID Platform { get; set; } = Environment.OSVersion.Platform;
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string? MachineName { get; set; } = Environment.MachineName;
        public string? UserName { get; set; } = Environment.UserName;

        // Service Information
        public int ProcessId { get; set; } = Environment.ProcessId;
        public string? ProcessPath { get; set; } = Environment.ProcessPath;

        protected override int Initialize(byte[] buffer)
        {
            const string VERSION_NULL_STRING = "0.0.0.0";

            var span = buffer.AsSpan();
            var pos = 0;

            var versionString = BinaryUtility.ReadUtf8String(span, out var count, pos);
            pos += count;

            Version = new(versionString ?? VERSION_NULL_STRING);
            Plugins = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);

            var arrayCount = BinaryUtility.ReadEncodedInteger(span, int.MaxValue, pos);
            pos += BinaryUtility.GetByteCount(arrayCount);

            for (var x = 0; x < arrayCount; x++)
            {
                var k = BinaryUtility.ReadUtf8String(span, out count, pos);
                pos += count;

                var v = BinaryUtility.ReadUtf8String(span, out count, pos);
                pos += count;

                if (string.IsNullOrEmpty(k) == false || string.IsNullOrEmpty(v) == false)
                    _ = Plugins.TryAdd(k!, new Version(v!));
            }

            Platform = (PlatformID)span[pos++];

            var timeValue = BinaryUtility.ReadEncodedInteger(span, long.MaxValue, pos);
            TimeStamp = DateTime.FromBinary(timeValue);
            pos += sizeof(long) + 1;

            MachineName = BinaryUtility.ReadUtf8String(span, out count, pos);
            pos += count;

            UserName = BinaryUtility.ReadUtf8String(span, out count, pos);
            pos += count;

            ProcessId = BinaryUtility.ReadEncodedInteger(span, int.MaxValue, pos);
            pos += sizeof(int) + 1;

            ProcessPath = BinaryUtility.ReadUtf8String(span, out count, pos);
            pos += count;

            return pos;
        }

        public override byte[] ToArray()
        {
            var str = $"{Version}";
            var count = Encoding.UTF8.GetByteCount(str);

            var size = BinaryUtility.GetByteCount(str);
            var dst = new byte[size];
            var pos = dst.Length;
            _ = BinaryUtility.WriteUtf8String(str, 0, dst, 0, count);

            size = BinaryUtility.GetByteCount(Plugins.Count);
            Array.Resize(ref dst, dst.Length + size);
            BinaryUtility.WriteEncodedInteger(Plugins.Count, dst, pos);
            pos = dst.Length;

            foreach (var (key, value) in Plugins)
            {
                str = key;
                count = Encoding.UTF8.GetByteCount(str);
                size = BinaryUtility.GetByteCount(str);
                Array.Resize(ref dst, dst.Length + size);
                _ = BinaryUtility.WriteUtf8String(str, 0, dst, pos, count);
                pos = dst.Length;

                str = $"{value}";
                count = Encoding.UTF8.GetByteCount(str);
                size = BinaryUtility.GetByteCount(str);
                Array.Resize(ref dst, dst.Length + size);
                _ = BinaryUtility.WriteUtf8String(str, 0, dst, pos, count);
                pos = dst.Length;
            }

            dst = [.. dst, (byte)Platform];
            pos = dst.Length;

            var timeValue = TimeStamp.ToBinary();
            Array.Resize(ref dst, dst.Length + sizeof(long) + 1);
            BinaryUtility.WriteEncodedInteger(timeValue, dst, pos);
            pos = dst.Length;

            count = MachineName != null
                ? Encoding.UTF8.GetByteCount(MachineName)
                : 0;
            size = BinaryUtility.GetByteCount(MachineName);
            Array.Resize(ref dst, dst.Length + size);
            _ = BinaryUtility.WriteUtf8String(MachineName, 0, dst, pos, count);
            pos = dst.Length;

            count = UserName != null
                ? Encoding.UTF8.GetByteCount(UserName)
                : 0;
            size = BinaryUtility.GetByteCount(UserName);
            Array.Resize(ref dst, dst.Length + size);
            _ = BinaryUtility.WriteUtf8String(UserName, 0, dst, pos, count);
            pos = dst.Length;

            Array.Resize(ref dst, dst.Length + sizeof(int) + 1);
            BinaryUtility.WriteEncodedInteger(ProcessId, dst, pos);
            pos = dst.Length;

            count = ProcessPath != null
                ? Encoding.UTF8.GetByteCount(ProcessPath)
                : 0;
            size = BinaryUtility.GetByteCount(ProcessPath);
            Array.Resize(ref dst, dst.Length + size);
            _ = BinaryUtility.WriteUtf8String(ProcessPath, 0, dst, pos, count);
            //pos = dst.Length;

            return dst;
        }
    }
}
