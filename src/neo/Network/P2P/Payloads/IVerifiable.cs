using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Models;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public static class Verifier
    {
        public static UInt160[] GetScriptHashesForVerifying(this IWitnessed witnessed, StoreView snapshot) 
            => witnessed switch
            {
                BlockBase block => GetScriptHashes(block, snapshot),
                Transaction tx => GetScriptHashes(tx, snapshot),
                ConsensusPayload payload => GetScriptHashes(payload, snapshot),
                _ => throw new Exception("Invalid IWitnessed")
            };

        private static UInt160[] GetScriptHashes(BlockBase block, StoreView snapshot)
        {
            if (block.PrevHash == UInt256.Zero) return new[] { block.Witness.ScriptHash };
            Header prev_header = snapshot.GetHeader(block.PrevHash);
            if (prev_header == null) throw new InvalidOperationException();
            return new[] { prev_header.NextConsensus };
        }

        private static UInt160[] GetScriptHashes(Transaction tx, StoreView snapshot)
        {
            return tx.Signers.Select(p => p.Account).ToArray();
        }

        private static UInt160[] GetScriptHashes(ConsensusPayload payload, StoreView snapshot)
        {
            ECPoint[] validators = NativeContract.NEO.GetNextBlockValidators(snapshot);
            if (validators.Length <= payload.ValidatorIndex)
                throw new InvalidOperationException();
            return new[] { Contract.CreateSignatureRedeemScript(validators[payload.ValidatorIndex]).ToScriptHash() };
        }

    }
}

