using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using System.Collections.Generic;

namespace Neo.SmartContract.Native
{
    public sealed class GasToken : FungibleToken<AccountState>
    {
        public override string Symbol => "GAS";
        public override byte Decimals => 8;

        internal GasToken()
        {
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            UInt160 account = Contract.GetBFTAddress(engine.ProtocolSettings.StandbyValidators);
            Mint(engine, account, 30_000_000 * Factor, false);
        }

        internal override void OnPersist(ApplicationEngine engine)
        {
            long totalNetworkFee = 0;
            HashSet<UInt160> distributed = new HashSet<UInt160>();
            foreach (Transaction tx in engine.PersistingBlock.Transactions)
            {
                if (distributed.Add(tx.Sender)) NEO.DistributeGas(engine, tx.Sender, false);
                Burn(engine, tx.Sender, tx.SystemFee + tx.NetworkFee);
                totalNetworkFee += tx.NetworkFee;
            }
            ECPoint[] validators = NEO.GetNextBlockValidators(engine.Snapshot, engine.ProtocolSettings.ValidatorsCount);
            UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
            Mint(engine, primary, totalNetworkFee, false);
        }
    }
}
