// Copyright (C) 2015-2024 The Neo Project.
//
// NotaryAssisted.cs file belongs to the neo project and is free
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
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class NotaryAssisted : TransactionAttribute
    {
        /// <summary>
        /// Indicates the number of keys participating in the transaction (main or fallback) signing process.
        /// </summary>
        public byte NKeys;

        public override TransactionAttributeType Type => TransactionAttributeType.NotaryAssisted;

        public override bool AllowMultiple => false;

        public override int Size => base.Size + sizeof(byte);

        protected override void DeserializeWithoutType(ref MemoryReader reader)
        {
            NKeys = reader.ReadByte();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(NKeys);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["nkeys"] = NKeys;
            return json;
        }

        public override bool Verify(DataCache snapshot, Transaction tx)
        {
            // Stub native Notary contract related check until the contract is implemented.
            UInt160 notaryH = new UInt160();
            return tx.Signers.Any(p => p.Account.Equals(notaryH));
        }

        public override long CalculateNetworkFee(DataCache snapshot, Transaction tx)
        {
            return (NKeys + 1) * base.CalculateNetworkFee(snapshot, tx);
        }
    }
}
