using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    public class InvocationTransaction : Transaction
    {
        public byte[] Script;
        public Fixed8 Gas;

        public override int Size => base.Size + Script.GetVarSize();

        public override Fixed8 SystemFee => Gas;

        public InvocationTransaction()
            : base(TransactionType.InvocationTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version > 1) throw new FormatException();
            Script = reader.ReadVarBytes(65536);
            if (Script.Length == 0) throw new FormatException();
            if (Version >= 1)
            {
                Gas = reader.ReadSerializable<Fixed8>();
                if (Gas < Fixed8.Zero) throw new FormatException();
            }
            else
            {
                Gas = Fixed8.Zero;
            }
        }

        public static Fixed8 GetGas(Fixed8 consumed)
        {
            Fixed8 gas = consumed - Fixed8.FromDecimal(10);
            if (gas <= Fixed8.Zero) return Fixed8.Zero;
            return gas.Ceiling();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            if (Version >= 1)
                writer.Write(Gas);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["script"] = Script.ToHexString();
            json["gas"] = Gas.ToString();
            return json;
        }

        public override bool Verify(IEnumerable<Transaction> mempool, InteropService service = null)
        {
            if (Gas.GetData() % 100000000 != 0) return false;
            return base.Verify(mempool, service);
        }

        protected override bool VerifyReceivingScripts(InteropService service = null)
        {
            HashSet<UInt160> contracts = new HashSet<UInt160>();
            foreach (UInt160 hash in Outputs.Select(p => p.ScriptHash).Distinct())
            {
                ContractState contract = Blockchain.Default.GetContract(hash);
                if (contract == null) continue;
                if (!contract.Payable) return false;
                contracts.Add(hash);
            }

            if (contracts.Count > 0) {
                HashSet<UInt160> calledContracts = new HashSet<UInt160>();
                using (StateReader readerService = new StateReader())
                {
                    ApplicationEngine engine = new ApplicationEngine(TriggerType.VerificationR, this, Blockchain.Default, service == null ? readerService : service, Gas);
                    engine.LoadScript(this.Script);
                    if (!engine.Execute()) return false;
                    if (engine.EvaluationStack.Count != 1 || !engine.EvaluationStack.Pop().GetBoolean()) return false;
                }

                calledContracts.IntersectWith(contracts);
                if (calledContracts.Count != contracts.Count) {
                    return false;
                }
            }
            return true;
        }
    }
}
