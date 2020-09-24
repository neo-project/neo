using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Models;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using Neo.VM;
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

        public static VerifyResult VerifyStateDependent(this Transaction tx, StoreView snapshot, TransactionVerificationContext context)
        {
            if (tx.ValidUntilBlock <= snapshot.Height || tx.ValidUntilBlock > snapshot.Height + Transaction.MaxValidUntilBlockIncrement)
                return VerifyResult.Expired;
            UInt160[] hashes = GetScriptHashes(tx, snapshot);
            if (NativeContract.Policy.IsAnyAccountBlocked(snapshot, hashes))
                return VerifyResult.PolicyFail;
            if (NativeContract.Policy.GetMaxBlockSystemFee(snapshot) < tx.SystemFee)
                return VerifyResult.PolicyFail;
            if (!(context?.CheckTransaction(tx, snapshot) ?? true)) return VerifyResult.InsufficientFunds;
            foreach (TransactionAttribute attribute in tx.Attributes)
                if (!attribute.Verify(snapshot, tx))
                    return VerifyResult.Invalid;
            long net_fee = tx.NetworkFee - tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
            if (!tx.VerifyWitnesses(snapshot, net_fee, WitnessFlag.StateDependent))
                return VerifyResult.Invalid;
            return VerifyResult.Succeed;
        }

        public static VerifyResult VerifyStateIndependent(this Transaction tx)
        {
            if (tx.Size > Transaction.MaxTransactionSize)
                return VerifyResult.Invalid;
            if (!tx.VerifyWitnesses(null, tx.NetworkFee, WitnessFlag.StateIndependent))
                return VerifyResult.Invalid;
            return VerifyResult.Succeed;
        }

        public static VerifyResult Verify(this Transaction tx, StoreView snapshot, TransactionVerificationContext context)
        {
            VerifyResult result = tx.VerifyStateIndependent();
            if (result != VerifyResult.Succeed) return result;
            result = tx.VerifyStateDependent(snapshot, context);
            return result;
        }

        public static bool Verify(this TransactionAttribute @this, StoreView snapshot, Transaction tx)
            => @this switch
            {
                HighPriorityAttribute highPriority => VerifyAttribute(highPriority, snapshot, tx),
                OracleResponse oracleResponse => VerifyAttribute(oracleResponse, snapshot, tx),
                _ => throw new Exception("Invalid TransactionAttribute")
            };

        private static bool VerifyAttribute(HighPriorityAttribute attrib, StoreView snapshot, Transaction tx)
        {
            UInt160 committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            return tx.Signers.Any(p => p.Account.Equals(committee));
        }

        static readonly Lazy<byte[]> fixedScript = new Lazy<byte[]>(() => {
            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitAppCall(NativeContract.Oracle.Hash, "finish");
            return sb.ToArray();
        });

        private static bool VerifyAttribute(OracleResponse attrib, StoreView snapshot, Transaction tx)
        {
            if (tx.Signers.Any(p => p.Scopes != WitnessScope.None)) return false;
            if (!tx.Script.AsSpan().SequenceEqual(fixedScript.Value)) return false;
            OracleRequest request = NativeContract.Oracle.GetRequest(snapshot, attrib.Id);
            if (request is null) return false;
            if (tx.NetworkFee + tx.SystemFee != request.GasForResponse) return false;
            UInt160 oracleAccount = Blockchain.GetConsensusAddress(NativeContract.Oracle.GetOracleNodes(snapshot));
            return tx.Signers.Any(p => p.Account.Equals(oracleAccount));        
        }    
    }
}

