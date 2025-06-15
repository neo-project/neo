// Copyright (C) 2015-2025 The Neo Project.
//
// ByteArrayFunctions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FASTER.core;

namespace Neo.Build.Core.Storage
{
    internal class ByteArrayFunctions : SimpleFunctions<byte[], byte[], Empty>
    {
        public override bool SingleWriter(ref byte[] key, ref byte[] input, ref byte[] src, ref byte[] dst, ref byte[] output, ref UpsertInfo upsertInfo, WriteReason reason) =>
            ConcurrentWriter(ref key, ref input, ref src, ref dst, ref output, ref upsertInfo);

        public override bool ConcurrentWriter(ref byte[] key, ref byte[] input, ref byte[] src, ref byte[] dst, ref byte[] output, ref UpsertInfo upsertInfo)
        {
            output = dst = src;
            return true;
        }

        public override bool InitialUpdater(ref byte[] key, ref byte[] input, ref byte[] value, ref byte[] output, ref RMWInfo rmwInfo)
        {
            output = value = input;
            return true;
        }

        public override bool CopyUpdater(ref byte[] key, ref byte[] input, ref byte[] oldValue, ref byte[] newValue, ref byte[] output, ref RMWInfo rmwInfo)
        {
            output = newValue = input;
            return true;
        }

        public override bool InPlaceUpdater(ref byte[] key, ref byte[] input, ref byte[] value, ref byte[] output, ref RMWInfo rmwInfo)
        {
            output = value = input;
            return true;
        }
    }
}
