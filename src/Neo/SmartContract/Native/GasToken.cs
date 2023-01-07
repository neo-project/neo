// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents the GAS token in the NEO system.
    /// </summary>
    public sealed class GasToken : FungibleToken<AccountState>
    {
        public override string Symbol => "GAS";
        public override byte Decimals => 8;

        internal GasToken()
        {
        }

        internal override ContractTask Initialize(ApplicationEngine engine)
        {
            UInt160 account = Contract.GetBFTAddress(engine.ProtocolSettings.StandbyValidators);
            return Mint(engine, account, engine.ProtocolSettings.InitialGasDistribution, false);
        }

        internal override async ContractTask OnPersist(ApplicationEngine engine)
        {
            long totalNetworkFee = 0;
            foreach (Transaction tx in engine.PersistingBlock.Transactions)
            {
                await Burn(engine, tx.Sender, tx.SystemFee + tx.NetworkFee);
                totalNetworkFee += tx.NetworkFee;
            }
            ECPoint[] validators = NEO.GetNextBlockValidators(engine.Snapshot, engine.ProtocolSettings.ValidatorsCount);
            UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
            await Mint(engine, primary, totalNetworkFee, false);
        }
    }
}
