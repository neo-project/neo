// Copyright (C) 2015-2024 The Neo Project.
//
// NotValidBefore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class NotValidBefore : TransactionAttribute
    {
        /// <summary>
        /// Indicates that the transaction is not valid before this height.
        /// </summary>
        public uint Height;

        public override TransactionAttributeType Type => TransactionAttributeType.NotValidBefore;

        public override bool AllowMultiple => false;

        public override int Size => base.Size +
            sizeof(uint); // Height.

        protected override void DeserializeWithoutType(ref MemoryReader reader)
        {
            Height = reader.ReadUInt32();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Height);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["height"] = Height;
            return json;
        }

        public override bool Verify(DataCache snapshot, Transaction tx)
        {
            var block_height = NativeContract.Ledger.CurrentIndex(snapshot);
            return block_height >= Height;
        }
    }
}
