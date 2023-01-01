// Copyright (C) 2015-2023 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Indicates that the transaction is of high priority.
    /// </summary>
    public class HighPriorityAttribute : TransactionAttribute
    {
        public override bool AllowMultiple => false;
        public override TransactionAttributeType Type => TransactionAttributeType.HighPriority;

        protected override void DeserializeWithoutType(ref MemoryReader reader)
        {
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
        }

        public override bool Verify(DataCache snapshot, Transaction tx)
        {
            UInt160 committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            return tx.Signers.Any(p => p.Account.Equals(committee));
        }
    }
}
