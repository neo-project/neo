// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.ComponentModel;
using Neo.IO;
using Neo.Network.P2P.Payloads;

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
