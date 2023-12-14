// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.ComponentModel;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;

namespace Neo.GUI.Wrappers
{
    internal class SignerWrapper
    {
        [TypeConverter(typeof(UIntBaseConverter))]
        public UInt160 Account { get; set; }
        public WitnessScope Scopes { get; set; }
        public List<UInt160> AllowedContracts { get; set; } = new List<UInt160>();
        public List<ECPoint> AllowedGroups { get; set; } = new List<ECPoint>();

        public Signer Unwrap()
        {
            return new Signer
            {
                Account = Account,
                Scopes = Scopes,
                AllowedContracts = AllowedContracts.ToArray(),
                AllowedGroups = AllowedGroups.ToArray()
            };
        }
    }
}
