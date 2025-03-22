// Copyright (C) 2015-2025 The Neo Project.
//
// LocalStorageDevice.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FASTER.core;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Neo.Build.Core.Storage
{
    internal class LocalStorageDevice
    {
        public static FasterKV<byte[], byte[]> Create(string basePath, [NotNull] out LogSettings logSettings, [NotNull] out CheckpointSettings checkpointSettings) =>
            new(
                1L << 20,
                logSettings = new LogSettings()
                {
                    LogDevice = new ManagedLocalStorageDevice(Path.Combine(basePath, "LOG"), recoverDevice: true, osReadBuffering: true),
                    ObjectLogDevice = new ManagedLocalStorageDevice(Path.Combine(basePath, "DATA"), recoverDevice: true, osReadBuffering: true),
                    PageSizeBits = 9,
                    MemorySizeBits = 21,
                    SegmentSizeBits = 21,
                    MutableFraction = 0.3,
                },
                checkpointSettings = new CheckpointSettings()
                {
                    CheckpointManager = new DeviceLogCommitCheckpointManager(
                        new LocalStorageNamedDeviceFactory(),
                        new NeoCheckPointNamingScheme(basePath),
                        removeOutdated: false),
                },
                new SerializerSettings<byte[], byte[]>()
                {
                    keySerializer = () => new ByteArraySerializer(),
                    valueSerializer = () => new ByteArraySerializer(),
                },
                new ByteArrayFasterEqualityComparer()
            );
    }
}
