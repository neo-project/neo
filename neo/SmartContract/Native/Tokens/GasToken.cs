using System;
using System.Linq;
using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public sealed class GasToken : Nep5Token<Nep5AccountState>
    {
        public override string ServiceName => "Neo.Native.Tokens.GAS";
        public override string Name => "GAS";
        public override string Symbol => "gas";
        public override int Decimals => 8;

        internal GasToken()
        {
        }

        protected override StackItem Main(ApplicationEngine engine, string operation, VMArray args)
        {
            switch (operation)
            {
                case "distributeFees":
                    return DistributeFees(engine);
                default:
                    return base.Main(engine, operation, args);
            }
        }

        private bool DistributeFees(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.System) throw new InvalidOperationException();
            foreach (Transaction tx in engine.Snapshot.PersistingBlock.Transactions)
                Burn(engine, tx.Sender, tx.Gas + tx.NetworkFee);
            ECPoint[] validators;
            if (engine.Snapshot.PersistingBlock.Index > 1)
                validators = engine.Snapshot.NextValidators.Get().Validators;
            else
                validators = Blockchain.StandbyValidators;
            UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.Snapshot.PersistingBlock.ConsensusData.PrimaryIndex]).ToScriptHash();
            Mint(engine, primary, engine.Snapshot.PersistingBlock.Transactions.Sum(p => p.NetworkFee));
            return true;
        }
    }
}
