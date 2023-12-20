// Copyright (C) 2015-2024 The Neo Project.
//
// TestVerifiable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.IO;

namespace Neo.UnitTests
{
    public class TestVerifiable : IVerifiable
    {
        private readonly string testStr = "testStr";

        public Witness[] Witnesses
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public int Size => throw new NotImplementedException();

        public void Deserialize(ref MemoryReader reader)
        {
            throw new NotImplementedException();
        }

        public void DeserializeUnsigned(ref MemoryReader reader)
        {
            throw new NotImplementedException();
        }

        public UInt160[] GetScriptHashesForVerifying(DataCache snapshot)
        {
            throw new NotImplementedException();
        }

        public void Serialize(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(testStr);
        }
    }
}
