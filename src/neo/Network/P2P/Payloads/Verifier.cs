using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Models;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Designate;
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
            UInt160 oracleAccount = Blockchain.GetConsensusAddress(NativeContract.Designate.GetDesignatedByRole(snapshot, Role.Oracle));
            return tx.Signers.Any(p => p.Account.Equals(oracleAccount));        
        }

        public static InventoryType GetInventoryType(this IWitnessed witnessed)
            => witnessed switch
            {
                Block _ => InventoryType.Block,
                Transaction _ => InventoryType.TX,
                ConsensusPayload _ => InventoryType.Consensus,
                _ => throw new Exception("Invalid IWitnessed")
            };

        public static bool Verify(this IWitnessed witnessed, StoreView snapshot)
            => witnessed switch
            {
                Block block => Verify(block, snapshot),
                Transaction tx => Verify(tx, snapshot),
                ConsensusPayload payload => Verify(payload, snapshot),
                _ => throw new Exception("Invalid IWitnessed")
            };

        public static bool Verify(this BlockBase block, StoreView snapshot)
        {
            Header prev_header = snapshot.GetHeader(block.PrevHash);
            if (prev_header == null) return false;
            if (prev_header.Index + 1 != block.Index) return false;
            if (prev_header.Timestamp >= block.Timestamp) return false;
            if (!block.VerifyWitnesses(snapshot, 1_00000000)) return false;
            return true;
        }

        public static bool Verify(this ConsensusPayload payload, StoreView snapshot)
        {
            if (payload.BlockIndex <= snapshot.Height)
                return false;
            return payload.VerifyWitnesses(snapshot, 0_02000000);
        }

        public static bool Verify(this Transaction tx, StoreView snapshot)
        {
            return Verify(tx, snapshot, null) == VerifyResult.Succeed;
        }

        private const long MaxVerificationGas = 0_50000000;

        internal static bool VerifyWitnesses(this IWitnessed verifiable, StoreView snapshot, long gas, WitnessFlag filter = WitnessFlag.All)
        {
            if (gas < 0) return false;
            if (gas > MaxVerificationGas) gas = MaxVerificationGas;

            UInt160[] hashes;
            try
            {
                hashes = verifiable.GetScriptHashesForVerifying(snapshot);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (hashes.Length != verifiable.Witnesses.Length) return false;
            for (int i = 0; i < hashes.Length; i++)
            {
                WitnessFlag flag = verifiable.Witnesses[i].StateDependent ? WitnessFlag.StateDependent : WitnessFlag.StateIndependent;
                if (!filter.HasFlag(flag))
                {
                    gas -= verifiable.Witnesses[i].GasConsumed;
                    if (gas < 0) return false;
                    continue;
                }

                int offset;
                ContractMethodDescriptor init = null;
                byte[] verification = verifiable.Witnesses[i].VerificationScript;
                if (verification.Length == 0)
                {
                    ContractState cs = snapshot.Contracts.TryGet(hashes[i]);
                    if (cs is null) return false;
                    ContractMethodDescriptor md = cs.Manifest.Abi.GetMethod("verify");
                    if (md is null) return false;
                    verification = cs.Script;
                    offset = md.Offset;
                    init = cs.Manifest.Abi.GetMethod("_initialize");
                }
                else
                {
                    if (hashes[i] != verifiable.Witnesses[i].ScriptHash) return false;
                    offset = 0;
                }
                using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, verifiable, snapshot?.Clone(), gas))
                {
                    CallFlags callFlags = verifiable.Witnesses[i].StateDependent ? CallFlags.AllowStates : CallFlags.None;
                    ExecutionContext context = engine.LoadScript(verification, callFlags, offset);
                    if (init != null) engine.LoadContext(context.Clone(init.Offset), false);
                    engine.LoadScript(verifiable.Witnesses[i].InvocationScript, CallFlags.None);
                    if (engine.Execute() == VMState.FAULT) return false;
                    if (engine.ResultStack.Count != 1 || !engine.ResultStack.Pop().GetBoolean()) return false;
                    gas -= engine.GasConsumed;
                    verifiable.Witnesses[i].GasConsumed = engine.GasConsumed;
                }
            }
            return true;
        }
    }
}

