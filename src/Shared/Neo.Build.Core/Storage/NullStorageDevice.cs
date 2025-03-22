// Copyright (C) 2015-2025 The Neo Project.
//
// NullStorageDevice.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FASTER.core;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Build.Core.Storage
{
    internal class NullStorageDevice
    {
        public static FasterKV<byte[], byte[]> Create(string basePath, [NotNull] out LogSettings logSettings, [NotNull] out CheckpointSettings checkpointSettings) =>
            new(
                1L << 20,
                logSettings = new()
                {
                    LogDevice = new NullDevice(),
                    ObjectLogDevice = new NullDevice(),
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

        public static FasterKV<byte[], byte[]> Create(CheckpointSettings checkpointSettings, [NotNull] out LogSettings logSettings) =>
            new(
                1L << 20,
                logSettings = new()
                {
                    LogDevice = new NullDevice(),
                    ObjectLogDevice = new NullDevice(),
                    PageSizeBits = 9,
                    MemorySizeBits = 21,
                    SegmentSizeBits = 21,
                    MutableFraction = 0.3,
                },
                checkpointSettings,
                new SerializerSettings<byte[], byte[]>()
                {
                    keySerializer = () => new ByteArraySerializer(),
                    valueSerializer = () => new ByteArraySerializer(),
                },
                new ByteArrayFasterEqualityComparer()
            );

        public static FasterKV<byte[], byte[]> Create(ICheckpointManager checkpointManager, [NotNull] out LogSettings logSettings, [NotNull] out CheckpointSettings checkpointSettings) =>
            new(
                1L << 20,
                logSettings = new()
                {
                    LogDevice = new NullDevice(),
                    ObjectLogDevice = new NullDevice(),
                    PageSizeBits = 9,
                    MemorySizeBits = 21,
                    SegmentSizeBits = 21,
                    MutableFraction = 0.3,
                },
                checkpointSettings = new CheckpointSettings()
                {
                    CheckpointManager = checkpointManager,
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
