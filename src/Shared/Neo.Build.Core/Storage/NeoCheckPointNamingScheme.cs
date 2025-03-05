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

        public FileDescriptor DeltaLog(Guid token)
        {
            throw new NotImplementedException();
        }

        public string FasterLogCommitBasePath()
        {
            throw new NotImplementedException();
        }

        public FileDescriptor FasterLogCommitMetadata(long commitNumber)
        {
            throw new NotImplementedException();
        }

        public FileDescriptor HashTable(Guid token)
        {
            throw new NotImplementedException();
        }

        public FileDescriptor IndexCheckpointBase(Guid token)
        {
            throw new NotImplementedException();
        }

        public string IndexCheckpointBasePath()
        {
            throw new NotImplementedException();
        }

        public FileDescriptor IndexCheckpointMetadata(Guid token)
        {
            throw new NotImplementedException();
        }

        public FileDescriptor LogCheckpointBase(Guid token)
        {
            throw new NotImplementedException();
        }

        public string LogCheckpointBasePath()
        {
            throw new NotImplementedException();
        }

        public FileDescriptor LogCheckpointMetadata(Guid token)
        {
            throw new NotImplementedException();
        }

        public FileDescriptor LogSnapshot(Guid token)
        {
            throw new NotImplementedException();
        }

        public FileDescriptor ObjectLogSnapshot(Guid token)
        {
            throw new NotImplementedException();
        }

        public Guid Token(FileDescriptor fileDescriptor)
        {
            throw new NotImplementedException();
        }
    }
}
