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
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes.Protocol.Messages
{
    internal sealed class PipeVersion : IPipeMessage
    {
        private readonly byte[] _versionBytes = GC.AllocateUninitializedArray<byte>(sizeof(int));
        private readonly byte[] _platformBytes = GC.AllocateUninitializedArray<byte>(sizeof(byte));
        private readonly byte[] _timeStampBytes = GC.AllocateUninitializedArray<byte>(sizeof(long));
        private byte[] _machineNameBytes = [];
        private byte[] _userNameBytes = [];
        private readonly byte[] _processIdBytes = GC.AllocateUninitializedArray<byte>(sizeof(int));
        private byte[] _processPathBytes = [];

        // Assembly Information
        public int VersionNumber
        {
            get => _versionBytes.TryCatch(t => t.AsSpan().Read<int>(), 0);
            set
            {
                var span = _versionBytes.AsSpan();
                span.Write(value);
            }
        }

        // Computer Information
        public PlatformID Platform
        {
            get => _platformBytes.TryCatch(t => (PlatformID)t.AsSpan().Read<byte>(), PlatformID.Other);
            set
            {
                var span = _platformBytes.AsSpan();
                span.Write((byte)value);
            }
        }

        public DateTime TimeStamp
        {
            get => _timeStampBytes.TryCatch(t => DateTime.FromBinary(t.AsSpan().Read<long>()), DateTime.UtcNow);
            set
            {
                var span = _timeStampBytes.AsSpan();
                span.Write(value);
            }
        }

        public string MachineName
        {
            get => _machineNameBytes.TryCatch(t => t.AsSpan().ReadString(), string.Empty);
            set
            {
                Array.Resize(ref _machineNameBytes, value.GetStructSize());
                var span = _machineNameBytes.AsSpan();
                span.Write(value);
            }
        }

        public string UserName
        {
            get => _userNameBytes.TryCatch(t => t.AsSpan().ReadString(), string.Empty);
            set
            {
                Array.Resize(ref _userNameBytes, value.GetStructSize());
                var span = _userNameBytes.AsSpan();
                span.Write(value);
            }
        }

        // Service Information
        public int ProcessId
        {
            get => _processIdBytes.TryCatch(t => t.AsSpan().Read<int>(), 0);
            set
            {
                var span = _processIdBytes.AsSpan();
                span.Write(value);
            }
        }

        public string ProcessPath
        {
            get => _processPathBytes.TryCatch(t => t.AsSpan().ReadString(), string.Empty);
            set
            {
                Array.Resize(ref _processPathBytes, value.GetStructSize());
                var span = _processPathBytes.AsSpan();
                span.Write(value);
            }
        }

        public PipeVersion()
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
            sizeof(byte) +
            sizeof(long) +
            MachineName.GetStructSize() +
            UserName.GetStructSize() +
            sizeof(int) +
            ProcessPath.GetStructSize();

        public Task CopyFromAsync(Stream stream)
        {
            if (stream.CanRead == false)
                throw new IOException();

            stream.ReadExactly(_versionBytes);
            stream.ReadExactly(_platformBytes);
            stream.ReadExactly(_timeStampBytes);

            MachineName = stream.ReadString();
            UserName = stream.ReadString();

            stream.ReadExactly(_processIdBytes);

            ProcessPath = stream.ReadString();

            return Task.CompletedTask;
        }

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanWrite == false)
                throw new IOException();

            var bytes = ToArray();
            stream.Write(bytes);

            return stream.FlushAsync(cancellationToken);
        }

        public byte[] ToArray() =>
        [
            .. _versionBytes,
            .. _platformBytes,
            .. _timeStampBytes,
            .. _machineNameBytes,
            .. _userNameBytes,
            .. _processIdBytes,
            .. _processPathBytes
        ];
    }
}
