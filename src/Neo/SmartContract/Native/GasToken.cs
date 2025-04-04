// Copyright (C) 2015-2025 The Neo Project.
//
// GasToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Serilog;
using System.Diagnostics;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents the GAS token in the NEO system.
    /// </summary>
    public sealed class GasToken : FungibleToken<AccountState>
    {
        // Serilog logger instance
        private static readonly ILogger _log = Log.ForContext<GasToken>();

        public override string Symbol => "GAS";
        public override byte Decimals => 8;

        internal GasToken()
        {
        }

        internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
        {
            if (hardfork == ActiveIn)
            {
                _log.Information("Initializing GasToken native contract state...");
                var sw = Stopwatch.StartNew();
                UInt160 account = Contract.GetBFTAddress(engine.ProtocolSettings.StandbyValidators);
                BigInteger amount = engine.ProtocolSettings.InitialGasDistribution;
                _log.Information("Minting initial GAS distribution ({Amount}) to BFT address {BFTAddress}", amount, account);
                var mintResult = Mint(engine, account, amount, false);
                sw.Stop();
                _log.Information("GasToken initialization finished in {DurationMs} ms", sw.ElapsedMilliseconds);
                return mintResult;
            }
            return ContractTask.CompletedTask;
        }

        internal override async ContractTask OnPersistAsync(ApplicationEngine engine)
        {
            _log.Debug("GasToken OnPersist for block {BlockIndex}...", engine.PersistingBlock.Index);
            var sw = Stopwatch.StartNew();
            long totalNetworkFee = 0;
            foreach (Transaction tx in engine.PersistingBlock.Transactions)
            {
                long totalFeeToBurn = tx.SystemFee + tx.NetworkFee;
                _log.Verbose("Burning {Amount} GAS for Tx {TxHash} (Sender: {Sender}, SysFee: {SysFee}, NetFee: {NetFee})",
                    totalFeeToBurn, tx.Hash, tx.Sender, tx.SystemFee, tx.NetworkFee);
                await Burn(engine, tx.Sender, totalFeeToBurn);
                totalNetworkFee += tx.NetworkFee;

                // Adjust for NotaryAssisted attribute fee (minted elsewhere)
                var notaryAssisted = tx.GetAttribute<NotaryAssisted>();
                if (notaryAssisted is not null)
                {
                    long notaryFee = (notaryAssisted.NKeys + 1) * Policy.GetAttributeFeeV1(engine.SnapshotCache, (byte)notaryAssisted.Type);
                    _log.Verbose("Adjusting total network fee for Tx {TxHash} due to NotaryAssisted attribute (Fee: {NotaryFee})", tx.Hash, notaryFee);
                    totalNetworkFee -= notaryFee;
                }
            }

            if (totalNetworkFee > 0)
            {
                ECPoint[] validators = NEO.GetNextBlockValidators(engine.SnapshotCache, engine.ProtocolSettings.ValidatorsCount);
                UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
                _log.Information("Minting total network fee ({Amount}) to primary validator {PrimaryValidator}", totalNetworkFee, primary);
                await Mint(engine, primary, totalNetworkFee, false);
            }
            else
            {
                _log.Debug("No network fees to mint for block {BlockIndex} (TotalNetFee <= 0)", engine.PersistingBlock.Index);
            }

            sw.Stop();
            _log.Debug("GasToken OnPersist finished for block {BlockIndex} in {DurationMs} ms", engine.PersistingBlock.Index, sw.ElapsedMilliseconds);
        }
    }
}

