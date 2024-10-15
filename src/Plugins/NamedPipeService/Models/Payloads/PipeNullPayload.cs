// Copyright (C) 2015-2024 The Neo Project.
//
// PipeNullPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.IO;

namespace Neo.Plugins.Models.Payloads
{
    internal sealed class PipeNullPayload : IPipeMessage
    {
        public int Size => 0;

        public void CopyFrom(Stream stream) { }

        public void FromByteArray(byte[] buffer, int position = 0) { }

        public void CopyTo(Stream stream) { }

        public void CopyTo(byte[] buffer) { }

        public byte[] ToByteArray() => [];
    }
}
