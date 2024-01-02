// Copyright (C) 2015-2024 The Neo Project.
//
// TransactionAttributeWrapper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.ComponentModel;

namespace Neo.GUI.Wrappers
{
    internal class TransactionAttributeWrapper
    {
        public TransactionAttributeType Usage { get; set; }
        [TypeConverter(typeof(HexConverter))]
        public byte[] Data { get; set; }

        public TransactionAttribute Unwrap()
        {
            MemoryReader reader = new(Data);
            return TransactionAttribute.DeserializeFrom(ref reader);
        }
    }
}
