// Copyright (C) 2015-2025 The Neo Project.
//
// FasterDbScanIterator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FASTER.core;
using System;

namespace Neo.Build.Core.Storage
{
    internal class FasterDbScanIterator : IFasterScanIterator<SpanByteAndMemory, SpanByteAndMemory>
    {
        public FasterDbScanIterator()
        {

        }

        public long CurrentAddress => throw new NotImplementedException();

        public long NextAddress => throw new NotImplementedException();

        public long BeginAddress => throw new NotImplementedException();

        public long EndAddress => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ref SpanByteAndMemory GetKey()
        {
            throw new NotImplementedException();
        }

        public bool GetNext(out RecordInfo recordInfo)
        {
            throw new NotImplementedException();
        }

        public bool GetNext(out RecordInfo recordInfo, out SpanByteAndMemory key, out SpanByteAndMemory value)
        {
            throw new NotImplementedException();
        }

        public ref SpanByteAndMemory GetValue()
        {
            throw new NotImplementedException();
        }
    }
}
