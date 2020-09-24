using System;
using System.Linq;
using Neo.IO;
using Neo.Ledger;
using Neo.Models;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.Network.P2P.Payloads
{
    public class VerifiableBlock : Block, IVerifiable, IInteroperable
    {
        public VerifiableBlock() : base(ProtocolSettings.Default.Magic)
        {
        }

        public TrimmedBlock Trim()
        {
            return new TrimmedBlock
            {
                Version = Version,
                PrevHash = PrevHash,
                MerkleRoot = MerkleRoot,
                Timestamp = Timestamp,
                Index = Index,
                NextConsensus = NextConsensus,
                Witness = Witness,
                Hashes = Transactions.Select(p => p.Hash).Prepend(ConsensusData.Hash).ToArray(),
                ConsensusData = ConsensusData
            };
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

        public void FromStackItem(StackItem stackItem)
        {
            throw new NotImplementedException();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new VM.Types.Array(referenceCounter, new StackItem[]
            {
                // Computed properties
                Hash.ToArray(),

                // BlockBase properties
                Version,
                PrevHash.ToArray(),
                MerkleRoot.ToArray(),
                Timestamp,
                Index,
                NextConsensus.ToArray(),

                // Block properties
                Transactions.Length
            });
        }
    }
}
