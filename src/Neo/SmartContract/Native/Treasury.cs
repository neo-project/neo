// Copyright (C) 2015-2025 The Neo Project.
//
// Treasury.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable
#pragma warning disable IDE0051

using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// The Treasury native contract used for manage the treasury funds.
    /// </summary>
    public sealed class Treasury : NativeContract
    {
        internal Treasury() : base() { }

        public override Hardfork? ActiveIn => Hardfork.HF_Faun;

        protected override void OnManifestCompose(IsHardforkEnabledDelegate hfChecker, uint blockHeight, ContractManifest manifest)
        {
            manifest.SupportedStandards = ["NEP-26", "NEP-27"];
        }

        /// <summary>
        /// Verify checks whether the transaction is signed by the committee.
        /// </summary>
        /// <param name="engine">ApplicationEngine</param>
        /// <returns>Whether transaction is valid.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private bool Verify(ApplicationEngine engine) => CheckCommittee(engine);

        /// <summary>
        /// OnNEP17Payment callback.
        /// </summary>
        /// <param name="engine">ApplicationEngine</param>
        /// <param name="from">GAS sender</param>
        /// <param name="amount">The amount of GAS sent</param>
        /// <param name="data">Optional data</param>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void OnNEP17Payment(ApplicationEngine engine, UInt160 from, BigInteger amount, StackItem data) { }

        /// <summary>
        /// OnNEP11Payment callback.
        /// </summary>
        /// <param name="engine">ApplicationEngine</param>
        /// <param name="from">GAS sender</param>
        /// <param name="amount">The amount of GAS sent</param>
        /// <param name="tokenId">Nep11 token Id</param>
        /// <param name="data">Optional data</param>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void OnNEP11Payment(ApplicationEngine engine, UInt160 from, BigInteger amount, byte[] tokenId, StackItem data) { }
    }
}

#nullable disable
