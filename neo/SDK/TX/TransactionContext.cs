using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System.Collections.Generic;

namespace Neo.SDK.TX
{
    public class TransactionContext : ContractParametersContext
    {
        public TransactionContext(Transaction tx) : base(tx) { }

        public override IReadOnlyList<UInt160> ScriptHashes
        {
            get
            {
                return Verifiable.GetScriptHashesForVerifying(null);
            }
        }
    }
}
