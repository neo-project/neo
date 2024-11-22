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
        /// Native Notary contract hash stub used until native Notary contract is properly implemented.
        /// </summary>
        private static readonly UInt160 notaryHash = Neo.SmartContract.Helper.GetContractHash(UInt160.Zero, 0, "Notary");

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
            return tx.Signers.Any(p => p.Account.Equals(notaryHash));
        }

        /// <summary>
        /// Calculates the network fee needed to pay for NotaryAssisted attribute. According to the
        /// https://github.com/neo-project/neo/issues/1573#issuecomment-704874472, network fee consists of
        /// the base Notary service fee per key multiplied by the expected number of transactions that should
        /// be collected by the service to complete Notary request increased by one (for Notary node witness
        /// itself).
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="tx">The transaction to calculate.</param>
        /// <returns>The network fee of the NotaryAssisted attribute.</returns>
        public override long CalculateNetworkFee(DataCache snapshot, Transaction tx)
        {
            return (NKeys + 1) * base.CalculateNetworkFee(snapshot, tx);
        }
    }
}
