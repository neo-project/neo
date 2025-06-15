// Copyright (C) 2015-2025 The Neo Project.
//
// DebugEventLog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.Core.Logging
{
    internal static class DebugEventLog
    {
        public const int Fault = 100;

        public const int Create = 200;
        public const int Load = 201;
        public const int PrePost = 202;
        public const int Post = 203;
        public const int Break = 204;
        public const int Execute = 205;

        public const int Burn = 300;
        public const int Call = 301;
        public const int Notify = 302;
        public const int Log = 303;

        public const int Persist = 400;
        public const int PostPersist = 401;

        public const int StoragePut = 500;
        public const int StorageGet = 501;
        public const int StorageFind = 502;
        public const int StorageDelete = 503;

        public const int IteratorNext = 600;
        public const int IteratorGet = 601;
    }
}
