using System;
using Neo.IO;
using Neo.Models;
using Neo.Persistence;

namespace Neo.Network.P2P.Payloads
{
    public class VerifiableBlock : Block, IVerifiable
    {
        public VerifiableBlock() : base(ProtocolSettings.Default.Magic)
        {
        }

        public InventoryType InventoryType => InventoryType.Block;

        public UInt160[] GetScriptHashesForVerifying(StoreView snapshot)
        {
            if (PrevHash == UInt256.Zero) return new[] { Witness.ScriptHash };
            Header prev_header = snapshot.GetHeader(PrevHash);
            if (prev_header == null) throw new InvalidOperationException();
            return new[] { prev_header.NextConsensus };
        }

        public bool Verify(StoreView snapshot)
        {
            Header prev_header = snapshot.GetHeader(PrevHash);
            if (prev_header == null) return false;
            if (prev_header.Index + 1 != Index) return false;
            if (prev_header.Timestamp >= Timestamp) return false;
            if (!this.VerifyWitnesses(snapshot, 1_00000000)) return false;
            return true;
        }
    }
}
