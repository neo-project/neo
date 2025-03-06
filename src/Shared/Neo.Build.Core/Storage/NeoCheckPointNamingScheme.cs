// Copyright (C) 2015-2025 The Neo Project.
//
// NeoCheckPointNamingScheme.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FASTER.core;
using System;
using System.IO;
using System.Linq;

namespace Neo.Build.Core.Storage
{
    internal class NeoCheckPointNamingScheme(
        string basePath = "") : ICheckpointNamingScheme
    {
        public string BaseName() =>
            basePath;

        public long CommitNumber(FileDescriptor fileDescriptor) =>
            long.Parse(fileDescriptor.fileName.Split('.').Reverse().Take(2).Last());

        public FileDescriptor DeltaLog(Guid token) =>
            new($"{LogCheckpointBasePath()}/{token}", "DELTA");

        public string FasterLogCommitBasePath() =>
            "commits";

        public FileDescriptor FasterLogCommitMetadata(long commitNumber) =>
            new($"{FasterLogCommitBasePath()}", $"COMMIT.{commitNumber}");

        public FileDescriptor HashTable(Guid token) =>
            new($"{IndexCheckpointBasePath()}/{token}", "TABLE");

        public FileDescriptor IndexCheckpointBase(Guid token) =>
            new($"{IndexCheckpointBasePath()}/{token}", null);

        public string IndexCheckpointBasePath() =>
            "index";

        public FileDescriptor IndexCheckpointMetadata(Guid token) =>
            new($"{IndexCheckpointBasePath()}/{token}", "CHKIDX");

        public FileDescriptor LogCheckpointBase(Guid token) =>
            new($"{LogCheckpointBasePath()}/{token}", null);

        public string LogCheckpointBasePath() =>
            "log";

        public FileDescriptor LogCheckpointMetadata(Guid token) =>
            new($"{LogCheckpointBasePath()}/{token}", "CHKMAN");

        public FileDescriptor LogSnapshot(Guid token) =>
            new($"{LogCheckpointBasePath()}/{token}", "SNAP");

        public FileDescriptor ObjectLogSnapshot(Guid token) =>
            new($"{LogCheckpointBasePath()}/{token}", "SLOG");

        public Guid Token(FileDescriptor fileDescriptor) =>
            Guid.Parse(new DirectoryInfo(fileDescriptor.directoryName).Name);
    }
}
