using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;

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
            _ = Mint(engine, account, 30_000_000 * Factor, false);
        }

        internal override void OnPersist(ApplicationEngine engine)
        {
            long totalNetworkFee = 0;
            foreach (Transaction tx in engine.PersistingBlock.Transactions)
            {
                Burn(engine, tx.Sender, tx.SystemFee + tx.NetworkFee);
                totalNetworkFee += tx.NetworkFee;
            }
            ECPoint[] validators = NEO.GetNextBlockValidators(engine.Snapshot, engine.ProtocolSettings.ValidatorsCount);
            UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
            _ = Mint(engine, primary, totalNetworkFee, false);
        }
    }
}
