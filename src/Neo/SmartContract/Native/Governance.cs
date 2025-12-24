// Copyright (C) 2015-2025 The Neo Project.
//
// Governance.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using System.Numerics;

namespace Neo.SmartContract.Native;

public sealed class Governance : NativeContract
{
    public const string GasTokenName = "GasToken";
    public const string GasTokenSymbol = "GAS";
    public const byte GasTokenDecimals = 8;
    public static readonly BigInteger GasTokenFactor = BigInteger.Pow(10, GasTokenDecimals);

    public UInt160 GasTokenId => field ??= TokenManagement.GetAssetId(Hash, GasTokenName);

    internal Governance() : base(-13) { }

    internal override async ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardFork)
    {
        if (hardFork == ActiveIn)
        {
            UInt160 tokenid = TokenManagement.CreateInternal(engine, Hash, GasTokenName, GasTokenSymbol, GasTokenDecimals, BigInteger.MinusOne);
            UInt160 account = Contract.GetBFTAddress(engine.ProtocolSettings.StandbyValidators);
            await TokenManagement.MintInternal(engine, tokenid, account, engine.ProtocolSettings.InitialGasDistribution, assertOwner: false, callOnPayment: false);
        }
    }

    internal override async ContractTask OnPersistAsync(ApplicationEngine engine)
    {
        long totalNetworkFee = 0;
        foreach (Transaction tx in engine.PersistingBlock!.Transactions)
        {
            await TokenManagement.BurnInternal(engine, GasTokenId, tx.Sender, tx.SystemFee + tx.NetworkFee, assertOwner: false);
            totalNetworkFee += tx.NetworkFee;

            // Reward for NotaryAssisted attribute will be minted to designated notary nodes
            // by Notary contract.
            var notaryAssisted = tx.GetAttribute<NotaryAssisted>();
            if (notaryAssisted is not null)
            {
                totalNetworkFee -= (notaryAssisted.NKeys + 1) * Policy.GetAttributeFee(engine.SnapshotCache, (byte)notaryAssisted.Type);
            }
        }
        ECPoint[] validators = NEO.GetNextBlockValidators(engine.SnapshotCache, engine.ProtocolSettings.ValidatorsCount);
        UInt160 primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
        await TokenManagement.MintInternal(engine, GasTokenId, primary, totalNetworkFee, assertOwner: false, callOnPayment: false);
    }

    // A placeholder method to prevent empty contract script.
    // Should be removed when new methods are added.
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.None)]
    internal static void Placeholder() { }
}
